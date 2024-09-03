using EntitySecurity.Logic.Repository.Enums;

namespace EntitySecurity.Logic.Lock
{
    public interface IProtected
    {
        bool IsMatch(Type t);
    }

    public interface IProtected<T> : IProtected where T : class
    {
        //Used for Writes
        Task<bool> HasAccess(T obj, RepositoryOperationEnum operation, int identityId, CancellationToken cancellationToken);

        //Used for Reads
        IQueryable<T> Secured(int identityId);
    }
}
