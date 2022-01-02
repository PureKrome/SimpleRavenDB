namespace WorldDomination.SimpleRavenDb
{
    internal static class StringExtensions
    {
        internal static string ToFirstNewLineOrDefault(this string value)
        {
            return value.Contains('\n')
                ? value[..value.IndexOf('\n')]
                : value;
        }
    }
}
