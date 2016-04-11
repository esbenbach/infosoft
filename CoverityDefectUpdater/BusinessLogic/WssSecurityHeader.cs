namespace BusinessLogic
{
    using System.ServiceModel.Channels;
    using System.Xml;

    /// <summary>
    /// A message header implementation that can be used to set WSS security header values into a message
    /// </summary>
    internal class WssSecurityHeader : MessageHeader
    {
        /// <summary>
        /// The OASIS WSS security namespace
        /// </summary>
        private const string HeaderNamespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        /// <summary>
        /// The OASIS WSS security header name.
        /// </summary>
        private const string HeaderName = "Security";

        /// <summary>
        /// The username to be used when authenticating with the service
        /// </summary>
        private readonly string username;

        /// <summary>
        /// The password to be used when authenticating with the service
        /// </summary>
        private readonly string password;

        /// <summary>
        /// Initializes a new instance of the <see cref="WssSecurityHeader"/> class.
        /// </summary>
        /// <param name="username">Username for the service</param>
        /// <param name="password">Password for the service</param>
        public WssSecurityHeader(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        /// <summary>
        /// Gets the name of the message header.
        /// </summary>
        public override string Name
        {
            get { return "Security"; }
        }

        /// <summary>
        /// Gets the namespace of the message header.
        /// </summary>
        public override string Namespace
        {
            get { return HeaderNamespace; }
        }

        /// <summary>
        /// Called when the header is to be serialized as xml. This writes the header into the actual message
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="messageVersion">The message version.</param>
        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteStartElement("UsernameToken", this.Namespace);
            writer.WriteElementString("Username", this.Namespace, this.username);
            writer.WriteStartElement("Password", this.Namespace);
            writer.WriteAttributeString("Type", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText");
            writer.WriteValue(this.password);
            writer.WriteEndElement(); // Password
            writer.WriteEndElement(); // UsernameToken
        }
    }
}