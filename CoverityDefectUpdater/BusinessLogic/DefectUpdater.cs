namespace BusinessLogic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CoverityService;
    using Serilog;

    /// <summary>
    /// A class for updating defects/issues in Coverity Connect based on information from Tfs.
    /// </summary>
    public class DefectUpdater
    {
        /// <summary>
        /// Name of the Owner attribute in Coverity
        /// </summary>
        private const string OwnerAttributeName = "Owner";

        /// <summary>
        /// Value of the Owner attribute in Coverity when it is unassigned/empty
        /// </summary>
        private const string OwnerAttributeUnassignedValue = "Unassigned";

        /// <summary>
        /// Name of the Comment attribute in Coverity
        /// </summary>
        private const string CommentAttributeName = "Comment";

        /// <summary>
        /// Name of the Legacy attribute in Coverity. Used to find legacy defects.
        /// </summary>
        private const string LegacyAttributeName = "Legacy";

        /// <summary>
        /// The value that the legacy attribute should have for the defect to be considered a legacy defect.
        /// </summary>
        private const string LegacyAttributeValue = "True";

        /// <summary>
        /// Client for connecting to the coverity configuration service.
        /// </summary>
        private readonly CoverityConfigurationClient coverityConfiguration;

        /// <summary>
        /// Client for connecting to the coverity defect services
        /// </summary>
        private readonly CoverityDefectsClient coverityDefects;

        /// <summary>
        /// Client for getting owner/last changed by from TFS.
        /// </summary>
        private readonly TfsHistoryClient historyClient;

        /// <summary>
        /// Indicates whether to simulate defect updates. If set to true not updates is done, they are just simulated.
        /// </summary>
        private readonly bool simulateChanges;

        /// <summary>
        /// Determine if legacy defects should be assigned an owner. 
        /// This rarely makes sense as multiple people might have edited the file since the defect was introduced.
        /// </summary>
        private readonly bool assignLegacyDefects;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefectUpdater" /> class.
        /// </summary>
        /// <param name="configurationClient">Client for connecting to the coverity configuration service.</param>
        /// <param name="defectsClient">Client for connecting to the coverity defect services</param>
        /// <param name="historyClient">Client for getting owner/last changed by from TFS.</param>
        /// <param name="simulate">Indicates whether to simulate defect updates. If set to true not updates is done, they are just simulated.</param>
        /// <param name="assignLegacy">Determine if legacy defects should be assigned an owner. This rarely makes sense as multiple people might have edited the file since the defect was introduced.</param>
        public DefectUpdater(CoverityConfigurationClient configurationClient, CoverityDefectsClient defectsClient, TfsHistoryClient historyClient, bool simulate = false, bool assignLegacy = false)
        {
            this.coverityConfiguration = configurationClient;
            this.coverityDefects = defectsClient;
            this.historyClient = historyClient;
            this.simulateChanges = simulate;
            this.assignLegacyDefects = assignLegacy;
        }

        /// <summary>
        /// Assigns all unassigned defects in coverity connect
        /// </summary>
        /// <param name="targetStream">The target stream.</param>
        /// <param name="projectName">Name of the TFS project.</param>
        /// <param name="branchRootPath">The TFS branch root path.</param>
        /// <returns>A task representing the ongoing process of assigning defects to their "rightful" owners.</returns>
        public async Task AssignUnassignedDefectsAsync(string targetStream, string projectName, string branchRootPath)
        {
            var users = await this.coverityConfiguration.GetUsersAsync();
            var assignableUsers = users.Where(user => !user.disabled).ToDictionary(BuildUserDictionaryKey);
            var defects = await this.GetMergedDefectFromStream(targetStream);

            if (!this.assignLegacyDefects)
            { 
                defects = defects.Where(defect => !defect.defectStateAttributeValues.Any(attribute => attribute.attributeDefinitionId.name == LegacyAttributeName && attribute.attributeValueId.name == LegacyAttributeValue));
            }

            defects = defects.Where(defect => defect.defectStateAttributeValues.Any(attribute => attribute.attributeDefinitionId.name == OwnerAttributeName && attribute.attributeValueId.name == OwnerAttributeUnassignedValue));

            foreach (var defect in defects)
            {
                string lastEditedBy = this.historyClient.LookupOwner(projectName, branchRootPath, defect.filePathname);
                if (string.IsNullOrWhiteSpace(lastEditedBy))
                {
                    Log.Information("Unable to determine owner for {CID}", defect.cid);
                    continue;
                }

                if (!assignableUsers.ContainsKey(lastEditedBy))
                {
                    Log.Information("Unable to assign owner for CID {CID}. TFS Contributer {LastEditedBy} is not assignable in Coverity. Did you forget to add all users to Coverity?", defect.cid, lastEditedBy);
                    continue;
                }

                var previousOwner = this.GetCurrentOwner(defect);
                var newOwner = assignableUsers[lastEditedBy];

                defectStateAttributeValueDataObj ownerAttribute = new defectStateAttributeValueDataObj()
                { 
                    attributeDefinitionId = new attributeDefinitionIdDataObj() { name = OwnerAttributeName },
                    attributeValueId = new attributeValueIdDataObj() { name = newOwner.username },
                };

                defectStateAttributeValueDataObj commentAttribute = new defectStateAttributeValueDataObj()
                {
                    attributeDefinitionId = new attributeDefinitionIdDataObj() { name = CommentAttributeName },
                    attributeValueId = new attributeValueIdDataObj() { name = "Assigning owner to defect based on TFS History" },
                };

                if (!this.simulateChanges)
                {
                    await this.coverityDefects.UpdateDefectState(defect, new defectStateAttributeValueDataObj[] { ownerAttribute, commentAttribute });
                }

                Log.Information("Owner updated on CID {CID} from {PreviousOwner} to {LastEditedBy}", defect.cid, previousOwner, lastEditedBy);
            }
        }

        /// <summary>
        /// Builds the user dictionary key, with the Windows domain name if required.
        /// </summary>
        /// <param name="coverityUser">The coverity user.</param>
        /// <returns>A string to be used as a key in the user dictionary.</returns>
        private static string BuildUserDictionaryKey(userDataObj coverityUser)
        {
            if (coverityUser.domain == null || string.IsNullOrWhiteSpace(coverityUser.domain.name))
            {
                return coverityUser.username;
            }
            else
            {
                return coverityUser.domain.name.ToUpper() + "\\" + coverityUser.username;
            }
        }

        /// <summary>
        /// Gets the merged defect from stream.
        /// </summary>
        /// <param name="targetStream">The target stream.</param>
        /// <returns>A list of defects from the given stream</returns>
        private async Task<IEnumerable<mergedDefectDataObj>> GetMergedDefectFromStream(string targetStream)
        {
            var allStreams = await this.coverityConfiguration.GetStreamsAsync();
            var streams = allStreams.Where(it => it.id.name == targetStream);
            if (!streams.Any())
            {
                // Log Error and quit
                Log.Error("No streams found with the given name, aborting");
                throw new InvalidOperationException("No streams found with the given name, aborting");
            }

            return await this.coverityDefects.GetMergedDefectForStreamsAsync(streams.Select(it => it.id));
        }

        /// <summary>
        /// Gets the current owner.
        /// </summary>
        /// <param name="defect">The defect.</param>
        /// <returns>The current owner of the given defect</returns>
        private string GetCurrentOwner(mergedDefectDataObj defect)
        {
            return defect.defectStateAttributeValues
                .Where(it => it.attributeDefinitionId.name == OwnerAttributeName)
                .Select(it => it.attributeValueId.name).SingleOrDefault();
        }
    }
}