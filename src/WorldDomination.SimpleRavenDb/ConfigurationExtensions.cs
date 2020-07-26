using Microsoft.Extensions.Configuration;

namespace WorldDomination.SimpleRavenDb
{
    public static class ConfigurationExtensions
    {
        public static RavenDbOptions AddRavenDbConfiguration(this IConfiguration configuration)
        {
            return configuration
                .GetSection(RavenDbOptions.SectionKey)
                .Get<RavenDbOptions>();
        }
    }
}
