using EntitySecurity.Domain.Enums;

namespace EntitySecurity.Contract.Security
{
    public interface IIdentityInfo
    {
        int GetIdentityId();

        bool HasRole(EntitySecurityRoleEnum role);

        bool HasValue(string name);

        string GetValue(string name);
    }
}
