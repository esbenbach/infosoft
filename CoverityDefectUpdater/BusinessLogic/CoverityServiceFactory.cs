namespace BusinessLogic
{
    using CoverityService;

    /// <summary>
    /// Factory methods for creating service clients that connect to the coverity server.
    /// </summary>
    public class CoverityServiceFactory
    {
        /// <summary>
        /// The Coverity password
        /// </summary>
        private readonly string password;

        /// <summary>
        /// The Coverity username
        /// </summary>
        private readonly string username;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverityServiceFactory"/> class.
        /// </summary>
        /// <param name="username">The username for authenticating with Coverity Connect</param>
        /// <param name="password">The password for authenticating with Coverity Connect</param>
        public CoverityServiceFactory(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        /// <summary>
        /// Creates a configuration service client.
        /// </summary>
        /// <returns>A Configuration Service client</returns>
        public ConfigurationServiceClient CreateConfigurationServiceClient()
        {
            var service = new ConfigurationServiceClient();
            service.Endpoint.Behaviors.Add(new WssSecurityBehavior(this.username, this.password));
            return service;
        }

        /// <summary>
        /// Creates a defect service client.
        /// </summary>
        /// <returns>A Defect Service client</returns>
        public DefectServiceClient CreateDefectServiceClient()
        {
            var service = new DefectServiceClient();
            service.Endpoint.Behaviors.Add(new WssSecurityBehavior(this.username, this.password));
            return service;
        }
    }
}