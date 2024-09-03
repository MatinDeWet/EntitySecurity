using EntitySecurity.Contract.Security;
using EntitySecurity.Domain.Constants;
using EntitySecurity.Domain.Enums;
using System.Security.Claims;

namespace EntitySecurity.Logic.Security
{
    public class IdentityInfo : IIdentityInfo
    {
        private readonly IInfoSetter _infoSetter;

        public IdentityInfo(IInfoSetter infoSetter)
        {
            _infoSetter = infoSetter;
        }

        public int GetIdentityId()
        {
            var uid = GetValue(EntitySecurityClaimTypes.sub);

            if (!int.TryParse(uid, out int result))
            {
                return 0;
            }

            return result;
        }

        public bool HasRole(EntitySecurityRoleEnum role)
        {
            var roles = _infoSetter
                .Where(x => x.Type == ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList();

            EntitySecurityRoleEnum combinedRoles = EntitySecurityRoleEnum.None;

            foreach (var roleString in roles)
                if (Enum.TryParse(roleString, true, out EntitySecurityRoleEnum parsedRole))
                    combinedRoles |= parsedRole;

            return combinedRoles.HasFlag(role);
        }

        public string GetValue(string name)
        {
            var claim = _infoSetter.FirstOrDefault(x => x.Type == name);

            if (claim is null)
                return null!;
            else
                return claim.Value;
        }

        public bool HasValue(string name)
        {
            return _infoSetter.Any(x => x.Type == name);
        }
    }
}
