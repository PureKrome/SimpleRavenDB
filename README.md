<h1 align="center">Simple: RavenDb</h1>

<div align="center">
  Making it simple and easy to use RavenDb in your .NET Core application(s).
</div>

<br />

<div align="center">
    <!-- License -->
    <a href="https://choosealicense.com/licenses/mit/">
    <img src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" alt="License - MIT" />
    </a>
    <!-- NuGet -->
    <a href="https://www.nuget.org/packages/WorldDomination.SimpleRavenDb/">
    <img src="https://buildstats.info/nuget/WorldDomination.SimpleRavenDb" alt="NuGet" />
    </a>
    <!-- Github Actions -->
    <a href="https://ci.appveyor.com/api/projects/status/simpleravendb/branch/master?svg=true">
    <img src="https://ci.appveyor.com/api/projects/status/wao450s3jsu1d81s?svg=true" alt="AppVeyor-CI" />
    </a>
</div>

## Key Points

This library makes it <b>SIMPLE</b> (by abstracting away most of the boring ceremony) to use/code against a RavenDb server in your .NET application.

- ✅ Easy to read in configuration settings.
- ✅ Easy to seed fake data.
- ✅ Easy to do 'database migrations'. (think => RavenDb indexes or even schema changes)
- ✅ Easy to make sure any seeding/migrations are done _before_ the web host starts.

Currently targetting: `.NET60`.

---
## Installation

Package is available via NuGet.

```sh
dotnet add package WorldDomination.SimpleRavenDb
```


---

## Quickstarts

###

REF: [Sample Web Application](https://github.com/PureKrome/SimpleRavenDB/blob/2b3b5b171713df80e7f906537ff29643e98774b2/src/WorldDomination.SimpleRavenDb.SampleWebApplication/Startup.cs) which uses RavenDb

```json
// appSettings.json

{
    "RavenDb": {
        "ServerUrls": [ "http://localhost:5200" ],
        "DatabaseName": "Testing-SimpleRavenDb",
        "X509CertificateBase64": ""
    },

    "Logging": {
        ...
    },
    "AllowedHosts": "*"
}
```

```csharp
// startup.cs

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // Read in the RavenDb settings from appSettings.json
        var ravenDbOptions = Configuration.AddRavenDbConfiguration();
        var ravenDbSetupOptions = new RavenDbSetupOptions
        {
            DocumentCollections = FakeData()
        };

        // 1. Initializes an `IDocumentStore` and registers it with DI/IoC.
        // 2. Creates a DB migrations host, which will auto start
        // 3. Adds 2x fake document collections to the db, via the DB migrations host.
        services.AddSimpleRavenDb(ravenDbOptions, ravenDbSetupOptions);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    { ... }

    // A list of 2x document-collections: Users and Orders.
    private static List<IList> FakeData()
    {
        var fakeUsers = new List<User>
        {
            new User {  Name = "Princess Leia" },
            new User {  Name = "Han Solo" }
        };

        var fakeOrders = new List<Order>
        {
            new Order { Price = 1.1m },
            new Order { Price = 2.2m }
        };

        return new List<IList>
        {
            fakeUsers,
            fakeOrders
        };
    }
}

```

## Breakdown of Quickstart sections

### Reading RavenDb settings from an `appSettings.json` file

Given the following simple schema:

![image](https://user-images.githubusercontent.com/899878/147865491-8cdd08fe-86aa-44d4-81a8-fcf1e43722dd.png)


```csharp

// `Configuration` is some IConfiguration which is usually setup elsewhere.
var ravenDbOptions = Configuration.AddRavenDbConfiguration();
```

### Registering RavenDb with your applications DI/IoC

```csharp

// `services` is some IServiceCollection which is usually setup elsewhere.
services.AddSimpleRavenDb(ravenDbOptions);
```

This will initialise a new `IDocumentStore` and add this to your DI/IoC.
The `ravenDbOptions` settings are usually via configuration settings (e.g. environmental variables or appSettings.json, etc.)


### Seeding fake data / migrating indexes.

Seeding fake data is a great scenario during your development. You wouldn't do this for prodution.
This will insert some data into the database. For example: the same 10 users every time you start your application.

When doing database migations or data seeding, this should all be done -before- the website/application is ready to accept connections/requests from users.
This means we will need to have a _separate host_ which runs _before_ the main application's host.

This is _automatically setup_ when you call `services.AddSimpleRavenDb(ravenDbOptions);`. So besides just adding an `IDocumentStore` to your DI/IoC,
it also secretly adds it's own 'IHostedService` which will run before your main application's hosted service, runs.


---

## Contribute
Yep - contributions are always welcome. Please read the contribution guidelines first.

## Code of Conduct

If you wish to participate in this repository then you need to abide by the code of conduct.

## Feedback

Yes! Please use the Issues section to provide feedback - either good or needs improvement :cool:

---
