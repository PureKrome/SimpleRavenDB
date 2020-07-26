using System;

namespace WorldDomination.SimpleRavenDb
{
    public static class AggregateRootExtensions
    {
        /// <summary>
        /// Returns the Id + Node section of a document Id.
        /// </summary>
        /// <param name="value">AggreagrateRoot.</param>
        /// <returns>string: the Id + Node. e.g. 1-A.</returns>
        public static string IdAsANumberAndNode(this AggregateRoot value)
        {
            if (string.IsNullOrWhiteSpace(value.Id))
            {
                return null;
            }

            // Format is: <documentName>/<id>-NODE
            // e.g. accounts/1-A
            //      productNotifications/1-A
            var lastIndex = value.Id.LastIndexOf("/", StringComparison.Ordinal);
            if (lastIndex <= 0)
            {
                return null;
            }

            // Grab the <number>-NODE.
            return value.Id.Substring(lastIndex + 1);
        }
    }
}
