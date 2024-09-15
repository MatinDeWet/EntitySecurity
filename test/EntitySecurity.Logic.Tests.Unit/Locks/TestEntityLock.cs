using EntitySecurity.Logic.Lock;
using EntitySecurity.Logic.Repository.Enums;
using EntitySecurity.Logic.Tests.Unit.Models;

namespace EntitySecurity.Logic.Tests.Unit.Locks
{
    public class TestEntityLock : Lock<TestEntity>
    {
        public override IQueryable<TestEntity> Secured(int identityId)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> HasAccess(TestEntity obj, RepositoryOperationEnum operation, int identityId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
