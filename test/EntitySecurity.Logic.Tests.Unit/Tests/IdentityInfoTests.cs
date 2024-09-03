using EntitySecurity.Contract.Security;
using EntitySecurity.Domain.Constants;
using EntitySecurity.Domain.Enums;
using EntitySecurity.Logic.Security;
using FluentAssertions;
using NSubstitute;
using System.Security.Claims;

namespace EntitySecurity.Logic.Tests.Unit.Tests
{
    public class IdentityInfoTests
    {
        private readonly IInfoSetter _infoSetter;
        private readonly IdentityInfo _identityInfo;

        public IdentityInfoTests()
        {
            _infoSetter = Substitute.For<IInfoSetter>();
            _identityInfo = new IdentityInfo(_infoSetter);
        }

        [Fact]
        public void GetIdentityId_ShouldReturnIdentityId_WhenValidIdExists()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(EntitySecurityClaimTypes.sub, "123") };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.GetIdentityId();

            // Assert
            result.Should().Be(123);
        }

        [Fact]
        public void GetIdentityId_ShouldReturnZero_WhenInvalidIdExists()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(EntitySecurityClaimTypes.sub, "invalid") };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.GetIdentityId();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void HasRole_ShouldReturnTrue_WhenRoleExists()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, EntitySecurityRoleEnum.Admin.ToString()) };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.HasRole(EntitySecurityRoleEnum.Admin);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasRole_ShouldReturnTrue_WhenRoleInOtherRoleExists()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, EntitySecurityRoleEnum.Developer.ToString()) };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.HasRole(EntitySecurityRoleEnum.Admin);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasRole_ShouldReturnFalse_WhenRoleDoesNotExist()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, EntitySecurityRoleEnum.Admin.ToString()) };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.HasRole(EntitySecurityRoleEnum.Developer);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetValue_ShouldReturnValue_WhenClaimExists()
        {
            // Arrange
            var claim = new Claim("name", "value");
            _infoSetter.GetEnumerator().Returns(new List<Claim> { claim }.GetEnumerator());

            // Act
            var result = _identityInfo.GetValue("name");

            // Assert
            result.Should().Be("value");
        }

        [Fact]
        public void GetValue_ShouldReturnNull_WhenClaimDoesNotExist()
        {
            // Arrange
            _infoSetter.GetEnumerator().Returns(new List<Claim>().GetEnumerator());

            // Act
            var result = _identityInfo.GetValue("name");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void HasValue_ShouldReturnTrue_WhenClaimExists()
        {
            // Arrange
            var claims = new List<Claim> { new Claim("name", "value") };
            _infoSetter.GetEnumerator().Returns(claims.GetEnumerator());

            // Act
            var result = _identityInfo.HasValue("name");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HasValue_ShouldReturnFalse_WhenClaimDoesNotExist()
        {
            // Arrange
            _infoSetter.GetEnumerator().Returns(new List<Claim>().GetEnumerator());

            // Act
            var result = _identityInfo.HasValue("name");

            // Assert
            result.Should().BeFalse();
        }
    }
}
