using System;
using System.Collections;
using System.Collections.Generic;
using Polly;

namespace WorldDomination.SimpleRavenDb
{
    public class RavenDbSetupOptions
    {
        /// <summary>
        /// Optional: A collection of Document-Collections, where each collection are any documents to be stored.
        /// </summary>
        public IEnumerable<IList> DocumentCollections { get; set; }

        /// <summary>
        /// Optional: Assembly which contains any indexes that need to be created/updated.
        /// </summary>
        public Type IndexAssembly { get; set; }

        /// <summary>
        /// Optional: Custom Polly policy to check if the Databsse is up and running. 
        /// </summary>
        public AsyncPolicy Policy { get; set; }
}
}
