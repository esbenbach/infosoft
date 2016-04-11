namespace BusinessLogic
{
    using System;
    using System.Linq;
    using System.Net;
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;

    /// <summary>
    /// A client for simply getting file history across merges from TFS.
    /// </summary>
    public class TfsHistoryClient
    {
        /// <summary>
        /// The URL for the TFS project that should be connected to
        /// </summary>
        private readonly string projectUrl;

        /// <summary>
        /// The credentials used to connect to TFS
        /// </summary>
        private readonly TfsClientCredentials credentials;

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsHistoryClient" /> class.
        /// </summary>
        /// <param name="projectUrl">The TFS project URL to connect to</param>
        /// <param name="username">Username for connecting to TFS</param>
        /// <param name="password">Password for connecting to TFS</param>
        /// <param name="domainName">Optional domain name for the username/password pair</param>
        public TfsHistoryClient(string projectUrl, string username, string password, string domainName)
        {
            this.projectUrl = projectUrl;
            this.credentials = new TfsClientCredentials(new BasicAuthCredential(new NetworkCredential(username, password, domainName)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsHistoryClient"/> class.
        /// </summary>
        /// <param name="projectUrl">The TFS project URL to connect to</param>
        public TfsHistoryClient(string projectUrl)
        {
            this.projectUrl = projectUrl;
            this.credentials = new TfsClientCredentials(true);
        }

        /// <summary>
        /// Lookups the owner, for the given file.
        /// </summary>
        /// <example>
        /// An example complete server path might be: ToplevelProject/Development/trunk/OmgAwesomeClass.cs
        /// Which would correspond to "projectName/branchPath/filePath" in terms of parameters.
        /// </example>
        /// <param name="projectName">Name of the project, the main TFS project in the project collection. This is the first part of the full path to the file.</param>
        /// <param name="branchPath">The relative server branch path, such as "Development/trunk"</param>
        /// <param name="filePath">The file path, relative to the branch root such as "/OmgAwesomeClass.cs"</param>
        /// <returns>The username for the person who last changed the file given in the filePath</returns>
        public string LookupOwner(string projectName, string branchPath, string filePath)
        {
            using (var conncetion = this.CreateCollection())
            { 
                conncetion.Connect(Microsoft.TeamFoundation.Framework.Common.ConnectOptions.IncludeServices);
                var service = conncetion.GetService<VersionControlServer>();
                var project = service.TryGetTeamProject(projectName);

                var serverPath = $"{project.ServerItem}{branchPath}{filePath}";
                return this.GetLastChangedBy(service, serverPath, VersionSpec.Latest);
            }
        }

        /// <summary>
        /// Creates a project collection reference (i.e. a connection to TFS)
        /// </summary>
        /// <returns>A Tfs Project collection</returns>
        private TfsTeamProjectCollection CreateCollection()
        {
            return new TfsTeamProjectCollection(new Uri(this.projectUrl), this.credentials);
        }

        /// <summary>
        /// Gets the last changed by for the given server item.
        /// This method follows all merge type changes and ignores the fact that a merge might sometimes also be an Edit or multiple other things. 
        /// So the result is the person that originally changed the file in the source branch even though that file might have been changed during a merge.
        /// </summary>
        /// <remarks>
        /// The method will likely produce weird results on "branch" type changes.
        /// </remarks>
        /// <param name="service">The service that has VC information</param>
        /// <param name="serverItem">The server item to get the owner for</param>
        /// <param name="latestVersion">The latest version to retreive ownership for (typically this is the current/latest/greates version)</param>
        /// <returns>The username of the person who last edited the file.</returns>
        private string GetLastChangedBy(VersionControlServer service, string serverItem, VersionSpec latestVersion)
        {
            // Ideally querying the history should be done on a more narrow version set, but this works so, it has to do.
            var queryParams = new QueryHistoryParameters(serverItem, RecursionType.Full)
            {
                ItemVersion = latestVersion,
                DeletionId = 0,
                VersionStart = null,
                VersionEnd = latestVersion,
                IncludeChanges = true,
                SlotMode = false,
                IncludeDownloadInfo = false,
            };

            var history = service.QueryHistory(queryParams);

            // We want the first one if one exists since it is the latest change.
            var changeset = history.FirstOrDefault();

            if (changeset != null)
            {
                foreach (var change in changeset.Changes)
                {
                    // Follow the history to the source branch if the change was a merge. 
                    // For all other changes just accept the changeset owner as the owner (this is essentially wrong for "Branch" change types, but irrelevant for the current need).
                    if (change.ChangeType.HasFlag(ChangeType.Merge))
                    {
                        var mergesWithDetails = service.QueryMergesWithDetails(null, null, 0, serverItem, new ChangesetVersionSpec(changeset.ChangesetId), 0, null, latestVersion, RecursionType.Full);
                        var latestWithDetails = mergesWithDetails.MergedItems.OrderByDescending(it => it.SourceVersionFrom).FirstOrDefault();
                        return this.GetLastChangedBy(service, latestWithDetails.SourceServerItem, new ChangesetVersionSpec(latestWithDetails.SourceVersionFrom));
                    }

                    return changeset.Owner;
                }
            }

            return string.Empty;
        }
    }
}