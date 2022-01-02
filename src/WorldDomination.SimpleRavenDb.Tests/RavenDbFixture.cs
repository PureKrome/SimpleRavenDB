namespace WorldDomination.SimpleRavenDb.Tests
{
    public class RavenDbFixture : RavenTestDriver
    {
        public RavenDbFixture()
        {
            var serverTestOptions = new TestServerOptions
            {
                FrameworkVersion = "6.0.1"
            };

            ConfigureServer(serverTestOptions);
        }
    }
}
