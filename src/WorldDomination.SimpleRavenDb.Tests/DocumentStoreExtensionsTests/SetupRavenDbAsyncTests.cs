using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using Microsoft.Extensions.Logging;
using Moq;
using Raven.Client.Documents.Operations;
using Raven.TestDriver;
using Shouldly;
using WorldDomination.SimpleRavenDb.Tests.DocumentStoreExtensionsTests;
using Xunit;

namespace WorldDomination.SimpleRavenDb.Tests
{
    public class SetupRavenDbAsyncTests : RavenTestDriver
    {
        public static TheoryData<IEnumerable<IList>, Type> Data
        {
            get
            {
                var fakeUsers = Builder<FakeUser>
                    .CreateListOfSize(20)
                    .All()
                    .With(fakeUser => fakeUser.Id, null) // Let the DB autogenerate the Id.
                    .Build()
                    .ToList();

                IEnumerable<IList> fakeData = new List<IList>
                {
                    fakeUsers
                };

                var heapsAndHeapsOfUsers = Builder<FakeUser>
                    .CreateListOfSize(5000)
                    .All()
                    .With(fakeUser => fakeUser.Id, null) // Let the DB autogenerate the Id.
                    .Build()
                    .ToList();

                var heapsAndHeapsOfFakeData = new List<IList>
                {
                    heapsAndHeapsOfUsers
                };

                var data = new TheoryData<IEnumerable<IList>, Type>
                {
                    // Fake data, total number of documents, Type which contains the index to execute.
                    { null,  null }, // No fake data to seed, no Indexes.
                    { fakeData, null }, // Some fake data to seed, no Indexes.
                    { null, typeof(FakeUsers_ByName) }, // No fake data but an Index.
                    { fakeData, typeof(FakeUsers_ByName) }, // Some fake data to seed & Indexes.
                    { heapsAndHeapsOfFakeData, typeof(FakeUsers_ByName) } // Heaps and heaps of some fake data to seed & Indexes.
                };

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Data))]
        public async Task GivenTheBareMinimum_SetupRavenDb_SetsUpADatabase(IEnumerable<IList> fakeData, Type indexAssembly)
        {
            // Arrange.
            var logger = new Mock<ILogger>();

            var documentStore = GetDocumentStore();

            // Act.
            var setupOptions = new RavenDbSetupOptions
            {
                DocumentCollections = fakeData,
                IndexAssembly = indexAssembly
            };
            await documentStore.SetupRavenDbAsync(setupOptions,logger.Object, default);

            // Assert.
            WaitForIndexing(documentStore);

            var statistics = documentStore.Maintenance.Send(new GetStatisticsOperation());

            statistics.Indexes.Length.ShouldBe(indexAssembly is null ? 0 : 1);

            if (fakeData?.Any() == true)
            {
                statistics.CountOfDocuments.ShouldBeGreaterThan(0);

                using (var asyncSession = documentStore.OpenAsyncSession())
                {
                    var fakeUser = await asyncSession.LoadAsync<FakeUser>("fakeUsers/1-a");
                    fakeUser.ShouldNotBeNull();
                }
            }

            documentStore.Dispose();
        }
    }
}
