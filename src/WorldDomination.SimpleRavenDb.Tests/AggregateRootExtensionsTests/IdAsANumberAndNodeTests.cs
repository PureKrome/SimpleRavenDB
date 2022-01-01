

namespace WorldDomination.SimpleRavenDb.Tests.AggregateRootExtensionsTests
{
    public class IdAsANumberAndNodeTests
    {
        public class Foo : AggregateRoot
        {
        }

        [Theory]
        [InlineData("accounts/1-A", "1-A")]
        [InlineData("a/b/c/accounts/1-A", "1-A")]
        [InlineData("accounts/1-A-b", "1-A-b")]
        [InlineData("1-A", null)]
        [InlineData(null, null)]
        public void GivenAnAggregateRootWithAnIdSet_IdAsANumberAndNode_ReturnsTheExpectedString(string id, string expectedResult)
        {
            // Arrange.
            var foo = new Foo
            {
                Id = id
            };

            // Act.
            var result = foo.IdAsANumberAndNode();

            // Assert.
            result.ShouldBe(expectedResult);
        }
    }
}
