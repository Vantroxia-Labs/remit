using AegisEInvoicing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextMock = new Mock<HttpContext>();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);
        _currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);
    }

    [Fact]
    public void Constructor_WithValidHttpContextAccessor_ShouldNotThrow()
    {
        // Act & Assert
        var service = new CurrentUserService(_httpContextAccessorMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void UserId_WithNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);
        var service = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = service.UserId;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UserId_WithValidUserIdClaim_ShouldReturnUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void UserName_WithValidNameClaim_ShouldReturnUserName()
    {
        // Arrange
        var userName = "testuser";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.UserName;

        // Assert
        result.Should().Be(userName);
    }

    [Fact]
    public void Email_WithValidEmailClaim_ShouldReturnEmail()
    {
        // Arrange
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.Email;

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedUser_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedUser_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims); // No authentication type
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void BusinessId_WithValidBusinessIdClaim_ShouldReturnBusinessId()
    {
        // Arrange
        var businessId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("businessId", businessId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.BusinessId;

        // Assert
        result.Should().Be(businessId);
    }

    [Fact]
    public void BranchId_WithValidBranchIdClaim_ShouldReturnBranchId()
    {
        // Arrange
        var branchId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("branchId", branchId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.BranchId;

        // Assert
        result.Should().Be(branchId);
    }

    [Fact]
    public void IsBusinessLevel_WithTrueClaim_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("isBusinessLevel", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsBusinessLevel;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBusinessLevel_WithNoClaim_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsBusinessLevel;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBranchLevel_WithTrueClaim_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("isBranchLevel", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsBranchLevel;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAegisUser_WithTrueClaim_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("isAegisUser", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.IsAegisUser;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AegisRole_WithValidClaim_ShouldReturnAegisRole()
    {
        // Arrange
        var AegisRole = "Administrator";
        var claims = new List<Claim>
        {
            new("AegisRole", AegisRole)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.AegisRole;

        // Assert
        result.Should().Be(AegisRole);
    }

    [Fact]
    public void AegisEmployeeId_WithValidClaim_ShouldReturnEmployeeId()
    {
        // Arrange
        var employeeId = "EMP12345";
        var claims = new List<Claim>
        {
            new("AegisEmployeeId", employeeId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.AegisEmployeeId;

        // Assert
        result.Should().Be(employeeId);
    }

    [Fact]
    public void AegisDepartment_WithValidClaim_ShouldReturnDepartment()
    {
        // Arrange
        var department = "IT Department";
        var claims = new List<Claim>
        {
            new("AegisDepartment", department)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.AegisDepartment;

        // Assert
        result.Should().Be(department);
    }

    [Fact]
    public void Roles_WithMultipleRoleClaims_ShouldReturnAllRoles()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "User"),
            new(ClaimTypes.Role, "Manager")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.Roles;

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Admin");
        result.Should().Contain("User");
        result.Should().Contain("Manager");
    }

    [Fact]
    public void Permissions_WithMultiplePermissionClaims_ShouldReturnAllPermissions()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("permission", "read"),
            new("permission", "write"),
            new("permission", "delete")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.Permissions;

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("read");
        result.Should().Contain("write");
        result.Should().Contain("delete");
    }

    [Fact]
    public void HasRole_WithExistingRole_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.HasRole("Admin");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasRole_WithNonExistingRole_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.HasRole("Admin");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasRole_WithNullRole_ShouldThrowArgumentException()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act & Assert
        _currentUserService.Invoking(s => s.HasRole(null!))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasPermission_WithExistingPermission_ShouldReturnTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("permission", "read"),
            new("permission", "write")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.HasPermission("read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WithNonExistingPermission_ShouldReturnFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("permission", "read")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act
        var result = _currentUserService.HasPermission("write");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WithNullPermission_ShouldThrowArgumentException()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        _httpContextMock.Setup(x => x.User).Returns(principal);

        // Act & Assert
        _currentUserService.Invoking(s => s.HasPermission(null!))
            .Should().Throw<ArgumentException>();
    }
}