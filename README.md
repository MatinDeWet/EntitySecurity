# EntitySecurity

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/MatinDeWet/DatabaseSecurity/dotnet.yml)](https://github.com/MatinDeWet/DatabaseSecurity)

## Packedges
### EntitySecurity.Domain
[![NuGet Version](https://img.shields.io/nuget/v/MatinDeWet.EntitySecurity.Domain)](https://www.nuget.org/packages/MatinDeWet.EntitySecurity.Domain) 

### EntitySecurity.Contract
[![NuGet Version](https://img.shields.io/nuget/v/MatinDeWet.EntitySecurity.Contract)](https://www.nuget.org/packages/MatinDeWet.EntitySecurity.Contract)

### EntitySecurity.Logic
[![NuGet Version](https://img.shields.io/nuget/v/MatinDeWet.EntitySecurity.Logic)](https://www.nuget.org/packages/MatinDeWet.EntitySecurity.Logic) 

Entity Security is a library that allows you to secure your entities in your application.

## Setup
EntitySecurity.Logic contains a register method that you can use to register your entities.

```csharp
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddEntitySecurity();
	}
```

### Creating a custom Repository
To be able to effectively make use of this library you will need to create a custom repository with a interface that will inherit from the IRepository interface.

```C#
	public interface IExampleRepository : IRepository
	{
	}
```
```C#
	public class ExampleRepository : JudgedRepository<TestContext>, IExampleRepository
	{
		public ExampleRepository(TestContext ctx) : base(ctx)
		{
		}
	}
```

Within the repository you will need to inherit from the JudgedRepository class where it takes in your context as a generic type.
You will need to register this repository in the main project program file.

```C#
    services.AddScoped<IExampleRepository, ExampleRepository>();
```

### Securing your data
To secure your data you will need to create locks on your data.
Currently the library makes use of a locking mechanism.
There are two methods that need to be implemented in the lock classes: (Secured and HasAccess)

- Secured is used when reading data
- HasAccess is used when saving data

They can be implemented as follows:

```C#
    public class ClientLock : Lock<Client>
    {
        private readonly TestContext _context;

        public ClientLock(TestContext context)
        {
            _context = context;
        }

        public override IQueryable<Client> Secured(int identityId)
        {
            var qry = from c in _context.Clients
                      join ut in _context.UserTeams on c.TeamId equals ut.TeamId
                      where ut.UserId == identityId
                      select c;

            return qry;
        }

        public override async Task<bool> HasAccess(Client obj, RepositoryOperationEnum operation, int identityId, CancellationToken cancellationToken)
        {
            var teamId = obj.TeamId;

            if (teamId == 0)
            {
                return false;
            }

            var query = from ut in _context.UserTeams
                        where ut.UserId == identityId
                        && ut.TeamId == teamId
                        select ut.TeamId;

            return await query.AnyAsync(cancellationToken);
        }
    }
```

You will also need to register the locks.

```C#
    services.AddScoped<IProtected, ClientLock>();
```

### The Middleware
For the Library to work you will need to supply the library with the current identity. Specifically with a subjectid(EntitySecurityClaimTypes.sub) claim

The IdentityConstants Id will be used in the lock.
```C#
    new Claim(EntitySecurityClaimTypes.sub, "1")
```

You will need to pass the user claims through to the IInfoSetter interface and call the SetUser to pass the user claims through, this data is required data to the library.

