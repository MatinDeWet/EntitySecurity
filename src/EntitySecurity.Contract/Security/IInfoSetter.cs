using System.Security.Claims;

namespace EntitySecurity.Contract.Security
{
    public interface IInfoSetter : IList<Claim>
    {
        void SetUser(IEnumerable<Claim> claims);
    }
}
