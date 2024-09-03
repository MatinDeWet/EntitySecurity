using EntitySecurity.Contract.Repository;
using EntitySecurity.Contract.Security;
using EntitySecurity.Domain.Enums;
using EntitySecurity.Logic.Lock;
using EntitySecurity.Logic.Repository.Enums;
using Microsoft.EntityFrameworkCore;

namespace EntitySecurity.Logic.Repository
{
    public class JudgedRepository<TCtx> : IRepository where TCtx : DbContext
    {
        protected readonly TCtx _context;
        protected readonly IIdentityInfo _info;

        private readonly IEnumerable<IProtected> _protection;

        public JudgedRepository(TCtx context, IIdentityInfo info, IEnumerable<IProtected> protection)
        {
            _context = context;
            _info = info;
            _protection = protection;
        }

        public IQueryable<T> Secure<T>() where T : class
        {
            if (!_info.HasRole(EntitySecurityRoleEnum.Developer))
            {
                if (_protection.FirstOrDefault(x => x.IsMatch(typeof(T))) is IProtected<T> entityLock)
                    return entityLock.Secured(_info.GetIdentityId());
            }

            return _context.Set<T>();
        }

        public virtual async Task InsertAsync<T>(T obj, CancellationToken cancellationToken) where T : class
        {
            var hasAccess = await HasAccess(obj, RepositoryOperationEnum.Insert, cancellationToken);

            if (!hasAccess)
                throw new UnauthorizedAccessException();

            _context.Add(obj);
        }

        public virtual async Task InsertAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class
        {
            foreach (var item in obj)
                await InsertAsync(item, cancellationToken);
        }

        public virtual async Task UpdateAsync<T>(T obj, CancellationToken cancellationToken) where T : class
        {
            var hasAccess = await HasAccess(obj, RepositoryOperationEnum.Update, cancellationToken);

            if (!hasAccess)
                throw new UnauthorizedAccessException();

            _context.Update(obj);
        }

        public virtual async Task UpdateAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class
        {
            foreach (var item in obj)
                await UpdateAsync(item, cancellationToken);
        }

        public virtual async Task DeleteAsync<T>(T obj, CancellationToken cancellationToken) where T : class
        {
            var hasAccess = await HasAccess(obj, RepositoryOperationEnum.Delete, cancellationToken);

            if (!hasAccess)
                throw new UnauthorizedAccessException();

            _context.Remove(obj);
        }

        public virtual async Task DeleteAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class
        {
            foreach (var item in obj)
                await DeleteAsync(item, cancellationToken);
        }

        private async Task<bool> HasAccess<T>(T obj, RepositoryOperationEnum operation, CancellationToken cancellationToken) where T : class
        {
            var result = true;

            if (!_info.HasRole(EntitySecurityRoleEnum.Developer))
            {
                if (_protection.FirstOrDefault(x => x.IsMatch(typeof(T))) is IProtected<T> entityLock)
                {
                    result = await entityLock.HasAccess(obj, operation, _info.GetIdentityId(), cancellationToken);
                }
            }

            return result;
        }
    }
}
