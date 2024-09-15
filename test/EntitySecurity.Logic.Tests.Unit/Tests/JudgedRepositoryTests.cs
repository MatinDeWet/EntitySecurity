using EntitySecurity.Contract.Security;
using EntitySecurity.Domain.Enums;
using EntitySecurity.Logic.Lock;
using EntitySecurity.Logic.Repository;
using EntitySecurity.Logic.Repository.Enums;
using EntitySecurity.Logic.Tests.Unit.Locks;
using EntitySecurity.Logic.Tests.Unit.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EntitySecurity.Logic.Tests.Unit.Tests
{
    public class JudgedRepositoryTests
    {
        [Fact]
        public void Secure_ReturnsSecuredQuery_WhenUserIsNotDeveloperAndLocksApply()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var testData = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Entity1" },
                new TestEntity { Id = 2, Name = "Entity2" },
                new TestEntity { Id = 3, Name = "Entity3" }
            }.AsQueryable();

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(testData.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(testData.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(testData.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(testData.GetEnumerator());

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            var protectedMock = new Mock<IProtected<TestEntity>>();
            protectedMock.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock.Setup(x => x.Secured(identityId)).Returns(testData.Where(e => e.Id != 2));

            var protections = new List<IProtected> { protectedMock.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            // Act
            var result = repository.Secure<TestEntity>().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, e => e.Id == 2);
        }

        [Fact]
        public void Secure_ReturnsFullQuery_WhenUserIsDeveloper()
        {
            // Arrange
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(true);

            var testData = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Entity1" },
                new TestEntity { Id = 2, Name = "Entity2" },
                new TestEntity { Id = 3, Name = "Entity3" }
            }.AsQueryable();

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(testData.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(testData.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(testData.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(testData.GetEnumerator());

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            var protections = new List<IProtected>(); // No protections applied

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            // Act
            var result = repository.Secure<TestEntity>().ToList();

            // Assert
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void Secure_ReturnsIntersectionOfSecuredQueries_WhenMultipleLocksApply()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var testData = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Entity1" },
                new TestEntity { Id = 2, Name = "Entity2" },
                new TestEntity { Id = 3, Name = "Entity3" }
            }.AsQueryable();

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(testData.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(testData.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(testData.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(testData.GetEnumerator());

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            var protectedMock1 = new Mock<IProtected<TestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.Secured(identityId)).Returns(testData.Where(e => e.Id != 2));

            var protectedMock2 = new Mock<IProtected<ITestInterface>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns((Type t) => typeof(ITestInterface).IsAssignableFrom(t));
            protectedMock2.Setup(x => x.Secured(identityId)).Returns(testData.Where(e => e.Id != 1));

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            // Act
            var result = repository.Secure<TestEntity>().ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
        }

        [Fact]
        public async Task InsertAsync_ThrowsUnauthorizedAccessException_WhenHasAccessReturnsFalse()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var dbContextMock = new Mock<DbContext>();

            var protectedMock = new Mock<IProtected<TestEntity>>();
            protectedMock.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Insert, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var protections = new List<IProtected> { protectedMock.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.InsertAsync(testEntity, CancellationToken.None));
        }

        [Fact]
        public async Task InsertAsync_AddsEntity_WhenHasAccessReturnsTrue()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            var protectedMock = new Mock<IProtected<TestEntity>>();
            protectedMock.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Insert, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protections = new List<IProtected> { protectedMock.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            await repository.InsertAsync(testEntity, CancellationToken.None);

            // Assert
            dbContextMock.Verify(x => x.Add(testEntity), Times.Once);
        }

        [Fact]
        public async Task HasAccess_ReturnsFalse_WhenAnyLockDeniesAccess()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock1 = new Mock<IProtected<TestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protectedMock2 = new Mock<IProtected<ITestInterface>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns((Type t) => typeof(ITestInterface).IsAssignableFrom(t));
            protectedMock2.Setup(x => x.HasAccess(It.IsAny<ITestInterface>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(TestEntity));
            var result = await (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasAccess_ReturnsTrue_WhenAllLocksAllowAccess()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock1 = new Mock<IProtected<TestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protectedMock2 = new Mock<IProtected<ITestInterface>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns((Type t) => typeof(ITestInterface).IsAssignableFrom(t));
            protectedMock2.Setup(x => x.HasAccess(It.IsAny<ITestInterface>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(TestEntity));
            var result = await (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatch_ReturnsTrue_WhenTypeIsAssignableFromT()
        {
            // Arrange
            var lockInstance = new TestEntityLock();

            // Act
            var result = lockInstance.IsMatch(typeof(ITestInterface));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasAccess_DoesNotInvokeLocks_WithIncompatibleTypes()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock1 = new Mock<IProtected<TestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protectedMock2 = new Mock<IProtected<SecondTestEntity>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock2.Setup(x => x.HasAccess(It.IsAny<SecondTestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).Throws(new Exception("This lock should not be invoked"));

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(TestEntity));
            var result = await (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!;

            // Assert
            Assert.True(result);
            protectedMock1.Verify(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>()), Times.Once);
            protectedMock2.Verify(x => x.HasAccess(It.IsAny<SecondTestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Secure_DoesNotApplyLocks_WithIncompatibleTypes()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var testData = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Entity1" },
                new TestEntity { Id = 2, Name = "Entity2" }
            }.AsQueryable();

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(testData.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(testData.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(testData.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(testData.GetEnumerator());

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            var protectedMock1 = new Mock<IProtected<SecondTestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.Secured(identityId)).Throws(new Exception("This lock should not be applied"));

            var protections = new List<IProtected> { protectedMock1.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            // Act
            var result = repository.Secure<TestEntity>().ToList();

            // Assert
            Assert.Equal(2, result.Count);
            protectedMock1.Verify(x => x.Secured(identityId), Times.Never);
        }

        [Fact]
        public async Task HasAccess_InvokesLocks_WithCompatibleInterfaceTypes()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock = new Mock<IProtected<ITestInterface>>();
            protectedMock.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns((Type t) => typeof(ITestInterface).IsAssignableFrom(t));
            protectedMock.Setup(x => x.HasAccess(It.IsAny<ITestInterface>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protections = new List<IProtected> { protectedMock.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(ITestInterface));
            var result = await (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!;

            // Assert
            Assert.True(result);
            protectedMock.Verify(x => x.HasAccess(It.IsAny<ITestInterface>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HasAccess_DoesNotDenyAccess_WhenLockTypeIsIncompatible()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock1 = new Mock<IProtected<SecondTestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.HasAccess(It.IsAny<SecondTestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var protections = new List<IProtected> { protectedMock1.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(TestEntity));
            var result = await (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!;

            // Assert
            Assert.True(result);
            protectedMock1.Verify(x => x.HasAccess(It.IsAny<SecondTestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void Secure_AppliesMultipleCompatibleLocks_Correctly()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            // Test data: all TestEntity instances
            var testData = new List<TestEntity>
            {
                new TestEntity { Id = 1, Name = "Entity1" }, // Excluded by protectedMock1
                new TestEntity { Id = 2, Name = "Entity2" }, // Should remain
                new TestEntity { Id = 3, Name = "Entity3" }  // Excluded by protectedMock2
            }.AsQueryable();

            var dbSetMock = new Mock<DbSet<TestEntity>>();
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(testData.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(testData.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(testData.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(testData.GetEnumerator());

            var dbContextMock = new Mock<DbContext>();
            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            // Mock lock for ITestInterface that excludes Id == 1
            var protectedMock1 = new Mock<IProtected<ITestInterface>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.Secured(identityId)).Returns(testData.Where(e => e.Id != 1));

            // Mock lock for TestEntity that excludes Id == 3
            var protectedMock2 = new Mock<IProtected<TestEntity>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns((Type t) => typeof(TestEntity).IsAssignableFrom(t));
            protectedMock2.Setup(x => x.Secured(identityId)).Returns(testData.Where(e => e.Id != 3));

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(dbContextMock.Object, identityInfoMock.Object, protections);

            // Act
            var result = repository.Secure<TestEntity>().ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].Id); // Only TestEntity with Id == 2 remains
        }

        [Fact]
        public async Task HasAccess_DoesNotThrow_InvalidCastException()
        {
            // Arrange
            var identityId = 1;
            var identityInfoMock = new Mock<IIdentityInfo>();
            identityInfoMock.Setup(x => x.HasRole(EntitySecurityRoleEnum.Developer)).Returns(false);
            identityInfoMock.Setup(x => x.GetIdentityId()).Returns(identityId);

            var protectedMock1 = new Mock<IProtected<TestEntity>>();
            protectedMock1.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock1.Setup(x => x.HasAccess(It.IsAny<TestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protectedMock2 = new Mock<IProtected<SecondTestEntity>>();
            protectedMock2.Setup(x => x.IsMatch(It.IsAny<Type>())).Returns(true);
            protectedMock2.Setup(x => x.HasAccess(It.IsAny<SecondTestEntity>(), RepositoryOperationEnum.Update, identityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var protections = new List<IProtected> { protectedMock1.Object, protectedMock2.Object };

            var repository = new JudgedRepository<DbContext>(new Mock<DbContext>().Object, identityInfoMock.Object, protections);

            var testEntity = new TestEntity { Id = 1, Name = "Entity1" };

            // Act and Assert
            var hasAccessMethod = repository.GetType().GetMethod("HasAccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.MakeGenericMethod(typeof(ITestInterface));
            var exception = await Record.ExceptionAsync(() => (Task<bool>)hasAccessMethod.Invoke(repository, new object[] { testEntity, RepositoryOperationEnum.Update, CancellationToken.None })!);

            Assert.Null(exception);
        }
    }
}
