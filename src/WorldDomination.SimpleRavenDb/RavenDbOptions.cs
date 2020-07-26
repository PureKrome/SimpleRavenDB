using System.Collections.Generic;

namespace WorldDomination.SimpleRavenDb
{
    /// <summary>
    /// Configuration section information for easily and quickly loading up RavenDb database settings.
    /// <code>
    ///     "RavenDb": {
    ///         "ServerUrls": [ "http://localhost:5200" ],
    ///         "DatabaseName": "Sabre",
    ///         "X509CertificateBase64": ""
    ///     },
    /// </code>
    /// </summary>
    public class RavenDbOptions
    {
        /// <summary>
        /// Main name for the configuration section which will contain all the RavenDb settings.
        /// </summary>
        public const string SectionKey = "RavenDb";

        /// <summary>
        /// List of Url's to connect to the RavenDb server.
        /// </summary>
        public IList<string> ServerUrls { get; set; }

        /// <summary>
        /// Database "Tenant" in the RavenDb server.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Base64 encoded representation of the X509 certificate.
        /// </summary>
        /// <remarks>
        /// How do I create one of these? Read this: https://ayende.com/blog/186881-A/x509-certificates-vs-api-keys-in-ravendb
        /// </remarks>
        public string X509CertificateBase64 { get; set; }
    }
}
