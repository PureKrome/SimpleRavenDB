namespace WorldDomination.SimpleRavenDb.Tests.DocumentStoreExtensionsTests
{
    public class FakeUsers_ByName : AbstractIndexCreationTask<FakeUser>
    {
        public FakeUsers_ByName()
        {
            Map = fakeUsers => from fakeUser in fakeUsers
                               select new
                               {
                                   fakeUser.Name
                               };
        }
    }
}
