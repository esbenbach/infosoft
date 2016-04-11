namespace BusinessLogic
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;

    /// <summary>
    /// An endpoint behaviour that changes client messages to include a username token header as described in http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0.pdf
    /// It should be doable using default WCF configuration behaviours, but I can't be bothered to figure out how when Coverity has solved the issue similarly to this.
    /// </summary>
    /// <seealso cref="System.ServiceModel.Dispatcher.IClientMessageInspector" />
    /// <seealso cref="System.ServiceModel.Description.IEndpointBehavior" />
    internal class WssSecurityBehavior : IClientMessageInspector, IEndpointBehavior
    {
        /// <summary>
        /// The username to be used when authenticating with the service
        /// </summary>
        private readonly string username;

        /// <summary>
        /// The password to be used when authenticating with the service
        /// </summary>
        private readonly string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="WssSecurityBehavior"/> class.
        /// </summary>
        /// <param name="username">Username for the service</param>
        /// <param name="password">Password for the service</param>
        public WssSecurityBehavior(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        /// <summary>
        /// Defines what to do with the reply from the service. Not relevant here.
        /// </summary>
        /// <param name="reply">The reply.</param>
        /// <param name="correlationState">State of the correlation.</param>
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
        }

        /// <summary>
        /// Defines what to do with the message before sending it to the server.
        /// This is the injection point where we want to add header information
        /// </summary>
        /// <param name="request">The request to inject headers into.</param>
        /// <param name="channel">The channel where the message is sent on.</param>
        /// <returns>Null since this should represent a correlation state</returns>
        public object BeforeSendRequest(ref System.ServiceModel.Channels.Message request, IClientChannel channel)
        {
            var header = new WssSecurityHeader(this.username, this.password);
            request.Headers.Add(header);
            return null;
        }

        /// <summary>
        /// Applies the client behavior, in this case by adding ourselves as message inspector to the endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="clientRuntime">The client runtime.</param>
        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add((IClientMessageInspector)this);
        }

        /// <summary>
        /// Applies the dispatch behavior. Not needed here
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        /// <summary>
        /// Validates the specified endpoint. Not needed here
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void Validate(ServiceEndpoint endpoint)
        {
        }

        /// <summary>
        /// Adds the binding parameters. Not needed here
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="bindingParameters">The binding parameters.</param>
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }
    }
}