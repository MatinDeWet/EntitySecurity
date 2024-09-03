using EntitySecurity.Contract.Security;
using System.Security.Claims;

namespace EntitySecurity.Logic.Security
{
    public class InfoSetter : List<Claim>, IInfoSetter
    {
        public void SetUser(IEnumerable<Claim> claims)
        {
            Clear();

            AddRange(claims);
        }
    }
}
