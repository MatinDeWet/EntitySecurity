namespace EntitySecurity.Contract.Repository
{
    public interface IRepository
    {
        IQueryable<T> Secure<T>() where T : class;

        Task InsertAsync<T>(T obj, CancellationToken cancellationToken) where T : class;
        Task InsertAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class;

        Task UpdateAsync<T>(T obj, CancellationToken cancellationToken) where T : class;
        Task UpdateAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class;

        Task DeleteAsync<T>(T obj, CancellationToken cancellationToken) where T : class;
        Task DeleteAsync<T>(List<T> obj, CancellationToken cancellationToken) where T : class;
    }
}
