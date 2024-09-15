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
                var applicableLocks = _protection.Where(x => x.IsMatch(typeof(T))).ToList();

                if (applicableLocks.Any())
                {
                    IQueryable<T> query = null!;

                    foreach (var entityLock in applicableLocks)
                    {
                        var lockType = entityLock.GetType()
                            .GetInterfaces()
                            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProtected<>))
                            .GetGenericArguments()[0];

                        // Check if T is assignable to the lock's type
                        if (lockType.IsAssignableFrom(typeof(T)))
                        {
                            var securedQuery = InvokeSecuredMethod<T>(entityLock, _info.GetIdentityId());

                            query = query == null ? securedQuery : query.Intersect(securedQuery);
                        }
                    }

                    return query ?? _context.Set<T>();
                }
            }

            return _context.Set<T>();
        }

        private IQueryable<T> InvokeSecuredMethod<T>(IProtected entityLock, int identityId) where T : class
        {
            var securedMethod = entityLock.GetType().GetMethod("Secured");

            if (securedMethod == null)
                throw new InvalidOperationException("Secured method not found on lock.");

            var securedQuery = securedMethod.Invoke(entityLock, new object[] { identityId }) as IQueryable;

            if (securedQuery == null)
                throw new InvalidOperationException("Secured method did not return a queryable.");

            return securedQuery.Cast<T>();
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
            if (!_info.HasRole(EntitySecurityRoleEnum.Developer))
            {
                var applicableLocks = _protection.Where(x => x.IsMatch(typeof(T))).ToList();

                foreach (var entityLock in applicableLocks)
                {
                    var lockInterface = entityLock.GetType()
                        .GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProtected<>));
                    var lockType = lockInterface.GetGenericArguments()[0];

                    // Check if the lock's type is assignable from the object's runtime type
                    if (lockType.IsAssignableFrom(obj.GetType()))
                    {
                        var hasAccessMethod = entityLock.GetType().GetMethod("HasAccess");

                        if (hasAccessMethod != null)
                        {
                            var hasAccessTask = (Task<bool>)hasAccessMethod.Invoke(entityLock, new object[] { obj, operation, _info.GetIdentityId(), cancellationToken })!;
                            var hasAccess = await hasAccessTask;

                            if (!hasAccess)
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
