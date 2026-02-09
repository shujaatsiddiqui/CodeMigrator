using FluentAssertions;
using Moq;
using UserApi.Library.Application.DTOs;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Application.Services;
using UserApi.Library.Domain.Entities;
using UserApi.Library.Domain.Interfaces;

namespace UserApi.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IUserService _sut;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _sut = new UserService(_userRepositoryMock.Object);
    }

    #region GetAllUsersAsync Tests

    [Fact]
    public async Task Given_UsersExist_When_GetAllUsersAsync_Then_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "John", Age = 25 },
            new() { Id = Guid.NewGuid(), Name = "Jane", Age = 30 },
            new() { Id = Guid.NewGuid(), Name = "Bob", Age = 35 }
        };
        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().HaveCount(3);
        _userRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task Given_NoUsersExist_When_GetAllUsersAsync_Then_ReturnsEmptyList()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        // Act
        var result = await _sut.GetAllUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_DatabaseError_When_GetAllUsersAsync_Then_ExceptionBubblesUp()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.GetAllUsersAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task Given_UserExists_When_GetUserByIdAsync_Then_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Age = 25 };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task Given_UserNotFound_When_GetUserByIdAsync_Then_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_DatabaseError_When_GetUserByIdAsync_Then_ExceptionBubblesUp()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task Given_ValidUserRequest_When_CreateUserAsync_Then_CreatesAndReturnsUser()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = 25 };
        var createdUser = new User { Id = Guid.NewGuid(), Name = "John", Age = 25 };

        _userRepositoryMock.Setup(r => r.AddAsync(It.Is<User>(u => u.Name == "John" && u.Age == 25)))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _sut.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
        result.Age.Should().Be(25);
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sa")]
    public async Task Given_BannedUsername_When_CreateUserAsync_Then_ThrowsArgumentException(string bannedName)
    {
        // Arrange
        var request = new CreateUserRequest { Name = bannedName, Age = 25 };

        // Act
        var act = async () => await _sut.CreateUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"The name {bannedName} is not allowed");
    }

    [Fact]
    public async Task Given_DatabaseError_When_CreateUserAsync_Then_ExceptionBubblesUp()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = 25 };
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.CreateUserAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task Given_UserExists_When_DeleteUserAsync_Then_DeletesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Age = 25 };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.DeleteAsync(user)).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteUserAsync(userId);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(r => r.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task Given_UserNotFound_When_DeleteUserAsync_Then_DoesNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        await _sut.DeleteUserAsync(userId);

        // Assert
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Given_DatabaseErrorOnGet_When_DeleteUserAsync_Then_ExceptionBubblesUp()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.DeleteUserAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Given_DatabaseErrorOnDelete_When_DeleteUserAsync_Then_ExceptionBubblesUp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Age = 25 };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.DeleteAsync(user))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.DeleteUserAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
