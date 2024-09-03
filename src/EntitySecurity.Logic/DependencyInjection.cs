using EntitySecurity.Contract.Security;
using EntitySecurity.Logic.Security;
using Microsoft.Extensions.DependencyInjection;

namespace EntitySecurity.Logic
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddEntitySecurity(this IServiceCollection services)
        {
            services.AddScoped<IIdentityInfo, IdentityInfo>();
            services.AddScoped<IInfoSetter, InfoSetter>();

            return services;
        }
    }
}
