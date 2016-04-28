namespace BusinessLogic
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using CoverityService;
    using Serilog;

    /// <summary>
    /// Used to do view notifications in coverity connect whenever an owner has been updated.
    /// </summary>
    public class ViewNotifier
    {
        /// <summary>
        /// The configuration client
        /// </summary>
        private readonly CoverityConfigurationClient configurationClient;

        /// <summary>
        /// Determines if we are simulating results or not.
        /// </summary>
        private readonly bool simulation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewNotifier"/> class.
        /// </summary>
        /// <param name="configurationClient">The configuration client.</param>
        /// <param name="simulation">if set to <c>true</c> [simulation].</param>
        public ViewNotifier(CoverityConfigurationClient configurationClient, bool simulation)
        {
            this.configurationClient = configurationClient;
            this.simulation = simulation;
        }

        /// <summary>
        /// Executes the notification.
        /// </summary>
        /// <param name="views">The views.</param>
        public async Task ExecuteNotificationsAsync(IEnumerable<string> views)
        {
            foreach (string view in views)
            {
                Log.Information($"Executing notification for {view}");
                if (!simulation)
                {
                    await this.ExecuteNotification(view);
                }
            }
        }

        /// <summary>
        /// Executes the notification.
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        public async Task ExecuteNotification(string viewName)
        {
            try
            {
                await this.configurationClient.ExecuteViewNotifcationAsync(viewName);
            }
            catch (FaultException<CovRemoteServiceException> exception)
            {
                Log.Error(exception, "Error while trying to execute view notification {ErrorCode} - {Message}", exception.Detail.errorCode, exception.Detail.message);
            }
        }
    }
}