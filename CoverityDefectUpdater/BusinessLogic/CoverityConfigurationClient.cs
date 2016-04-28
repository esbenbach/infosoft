namespace BusinessLogic
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CoverityService;

    /// <summary>
    /// A class for getting global configuration values from the Coverity Connect server
    /// </summary>
    public sealed class CoverityConfigurationClient
    {
        /// <summary>
        /// The default page size used when getting data from the server
        /// </summary>
        private const int DefaultPageSize = 100;

        /// <summary>
        /// The service factory used to create service connections
        /// </summary>
        private CoverityServiceFactory serviceFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverityConfigurationClient"/> class.
        /// </summary>
        /// <param name="serviceFactory">The service factory.</param>
        public CoverityConfigurationClient(CoverityServiceFactory serviceFactory)
        {
            this.serviceFactory = serviceFactory;
        }

        /// <summary>
        /// Gets all users known by Coverity Connect
        /// </summary>
        /// <returns>An enumeration of users</returns>
        public async Task<IEnumerable<userDataObj>> GetUsersAsync()
        {
            var filterSpecification = new userFilterSpecDataObj();

            var pageSpecification = new pageSpecDataObj()
            {
                pageSize = DefaultPageSize,
                sortAscending = true,
                startIndex = 0,
            };

            var request = new getUsersRequest(filterSpecification, pageSpecification);

            int totalNumberOfRecords = 0;
            int recordsFetched = 0;

            var users = new List<userDataObj>();

            using (var service = this.serviceFactory.CreateConfigurationServiceClient())
            { 
                // Need to request one page at a time since coverity has no way to allow us to get everything
                do
                {
                    var response = await service.getUsersAsync(request);
                    totalNumberOfRecords = response.@return.totalNumberOfRecords;
                    pageSpecification.startIndex += DefaultPageSize; // Get the next page!
                    recordsFetched += DefaultPageSize; // Use the page size to avoid enumerating users collection
                    users.AddRange(response.@return.users);
                }
                while (recordsFetched < totalNumberOfRecords);
            }

            return users;
        }

        /// <summary>
        /// Gets all projects known to Coverity Connect
        /// </summary>
        /// <param name="namePattern">The name pattern used to search/filter projects</param>
        /// <returns>An enumeration of all projects matching the given pattern</returns>
        public async Task<IEnumerable<projectDataObj>> GetProjectsAsync(string namePattern)
        {
            using (var service = this.serviceFactory.CreateConfigurationServiceClient())
            {
                var filterSpecification = new projectFilterSpecDataObj() { namePattern = namePattern };
                var request = new getProjectsRequest(filterSpecification);
                var response = await service.getProjectsAsync(request);
                return response.@return;
            }
        }

        /// <summary>
        /// Gets all streams
        /// </summary>
        /// <returns>An enumeration of streams known by Coverity</returns>
        public async Task<IEnumerable<streamDataObj>> GetStreamsAsync()
        {
            using (var service = this.serviceFactory.CreateConfigurationServiceClient())
            {
                var filterSpecification = new streamFilterSpecDataObj() { };
                var request = new getStreamsRequest(filterSpecification);
                var response = await service.getStreamsAsync(request);
                return response.@return;
            }
        }

        /// <summary>
        /// Executes view notifcation
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        /// <returns>A task representing the being done.</returns>
        public async Task ExecuteViewNotifcationAsync(string viewName)
        {
            using (var service = this.serviceFactory.CreateConfigurationServiceClient())
            {
                var request = new executeNotificationRequest(viewName);
                await service.executeNotificationAsync(request);
            }
        }
    }
}