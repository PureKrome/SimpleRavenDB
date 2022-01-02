namespace WorldDomination.SimpleRavenDb
{
    public static class AggregateRootExtensions
    {
        /// <summary>
        /// Returns the Id + Node section of a document Id.
        /// </summary>
        /// <param name="value">AggreagrateRoot.</param>
        /// <returns>string: the Id + Node. e.g. 1-A.</returns>
        public static string? IdAsANumberAndNode(this AggregateRoot value)
        {
            // Format is: <documentName>/<id>-NODE
            // e.g. accounts/1-A
            //      productNotifications/1-A

            if (string.IsNullOrWhiteSpace(value.Id))
            {
                return null;
            }

            // We have some text value, but it is legit?
            var lastIndex = value.Id.LastIndexOf("/", StringComparison.Ordinal);
            if (lastIndex <= 0)
            {
                // Nope - it's not in a valid format.
                return null;
            }

            // Grab the <number>-NODE.
            return value.Id[(lastIndex + 1)..];
        }
    }
}
