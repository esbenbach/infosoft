namespace BusinessLogic
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using CoverityService;

    /// <summary>
    /// A client for getting and updating defects/issues in Coverity Connect
    /// </summary>
    public class CoverityDefectsClient
    {
        /// <summary>
        /// The default page size used when requesting data from coverity
        /// </summary>
        private const int DefaultPageSize = 1000;

        /// <summary>
        /// Factory for creating connections to the Coverity Defect service
        /// </summary>
        private CoverityServiceFactory serviceFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverityDefectsClient"/> class.
        /// </summary>
        /// <param name="serviceFactory">The service factory.</param>
        public CoverityDefectsClient(CoverityServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Gets a list of merged defect for the given streams asynchronous.
        /// </summary>
        /// <param name="streams">The streams to get defects for</param>
        /// <returns>An enumeration of defects for the given streams.</returns>
        public async Task<IEnumerable<mergedDefectDataObj>> GetMergedDefectForStreamsAsync(IEnumerable<streamIdDataObj> streams)
        {
            using (var service = this.serviceFactory.CreateDefectServiceClient())
            {
                int totalNumberOfRecords = 0;
                int recordsFetched = 0;

                var pageSpecification = new pageSpecDataObj()
                {
                    pageSize = DefaultPageSize,
                    sortAscending = true,
                    startIndex = 0,
                };

                // TODO: We could probably filter on the owner here to reduce load on the Coverity Server.
                // TODO: We could definetly filter out based on various attributes here to avoid getting defects we know we are not interested in.
                var request = new getMergedDefectsForStreamsRequest()
                {
                    pageSpec = pageSpecification,
                    filterSpec = new mergedDefectFilterSpecDataObj(),
                    snapshotScope = new snapshotScopeSpecDataObj(),
                    streamIds = streams.ToArray()
                };

                // Need to request one page at a time since coverity has no way to allow us to get everything in one go
                List<mergedDefectDataObj> results = new List<mergedDefectDataObj>();
                do
                {
                    var response = await service.getMergedDefectsForStreamsAsync(request);
                    totalNumberOfRecords = response.@return.totalNumberOfRecords;
                    pageSpecification.startIndex += DefaultPageSize; // Get the next page!
                    recordsFetched += DefaultPageSize; // Use the page size to avoid enumerating users collection
                    results.AddRange(response.@return.mergedDefects);
                }
                while (recordsFetched < totalNumberOfRecords);

                return results;
            }
        }

        /// <summary>
        /// Updates the state of the defect
        /// </summary>
        /// <param name="defect">The defect to update</param>
        /// <param name="attributes">The attributes to set on the defects</param>
        /// <returns>A task representing the update work</returns>
        internal async Task UpdateDefectState(mergedDefectDataObj defect, IEnumerable<defectStateAttributeValueDataObj> attributes)
        {
            using (var service = this.serviceFactory.CreateDefectServiceClient())
            {
                var defectId = new mergedDefectIdDataObj() { cid = defect.cid, cidSpecified = true };
                var request = new getStreamDefectsRequest(new[] { defectId }, new streamDefectFilterSpecDataObj());
                var response = await service.getStreamDefectsAsync(request);

                var updateRequest = new updateStreamDefectsRequest(response.@return.Select(it => it.id).ToArray(), attributes.ToArray());
                await service.updateStreamDefectsAsync(updateRequest);
            }
        }
    }
}