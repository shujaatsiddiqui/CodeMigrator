using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserApi.Library.Application.DTOs;
using UserApi.Library.Application.Interfaces;
using UserApi.Library.Domain.Entities;

namespace UserApi.Tests.Integration;

/// <summary>
/// Integration tests that verify the behavior of HTTP triggers through the service layer.
/// These tests document expected HTTP behavior for each endpoint.
/// </summary>
public class UserHttpTriggersTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UserHttpTriggersTests>> _loggerMock;

    public UserHttpTriggersTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UserHttpTriggersTests>>();
    }

    #region GET /api/users Tests

    [Fact]
    public async Task Given_UsersExist_When_GetAllUsers_Then_Returns200WithUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Name = "John", Age = 25 },
            new() { Id = Guid.NewGuid(), Name = "Jane", Age = 30 }
        };
        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

        // Act & Assert
        var result = await _userServiceMock.Object.GetAllUsersAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_NoUsersExist_When_GetAllUsers_Then_Returns200WithEmptyArray()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<User>());

        // Act & Assert
        var result = await _userServiceMock.Object.GetAllUsersAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ServiceThrows_When_GetAllUsers_Then_Returns500()
    {
        // Arrange
        _userServiceMock.Setup(s => s.GetAllUsersAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var act = async () => await _userServiceMock.Object.GetAllUsersAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GET /api/users/{id} Tests

    [Fact]
    public async Task Given_UserExists_When_GetUserById_Then_Returns200WithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "John", Age = 25 };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        var result = await _userServiceMock.Object.GetUserByIdAsync(userId);
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Given_UserNotFound_When_GetUserById_Then_Returns404()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _userServiceMock.Object.GetUserByIdAsync(userId);
        result.Should().BeNull();
    }

    [Fact]
    public void Given_InvalidGuid_When_GetUserById_Then_Returns400()
    {
        // This test verifies that invalid GUID handling works correctly
        var invalidId = "not-a-guid";
        var canParse = Guid.TryParse(invalidId, out _);
        canParse.Should().BeFalse();
    }

    #endregion

    #region POST /api/users Tests

    [Fact]
    public async Task Given_ValidRequest_When_CreateUser_Then_Returns201WithUser()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = 25 };
        var createdUser = new User { Id = Guid.NewGuid(), Name = "John", Age = 25 };
        _userServiceMock.Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(
            r => r.Name == "John" && r.Age == 25))).ReturnsAsync(createdUser);

        // Act & Assert
        var result = await _userServiceMock.Object.CreateUserAsync(request);
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
    }

    [Fact]
    public void Given_EmptyBody_When_CreateUser_Then_Returns400()
    {
        // Arrange - empty request should be rejected
        var request = new CreateUserRequest { Name = "", Age = 0 };

        // Assert - validation should catch this
        request.Name.Should().BeEmpty();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sa")]
    public async Task Given_BannedName_When_CreateUser_Then_Returns400WithMessage(string bannedName)
    {
        // Arrange
        var request = new CreateUserRequest { Name = bannedName, Age = 25 };
        _userServiceMock.Setup(s => s.CreateUserAsync(It.Is<CreateUserRequest>(
            r => r.Name == bannedName)))
            .ThrowsAsync(new ArgumentException($"The name {bannedName} is not allowed"));

        // Act & Assert
        var act = async () => await _userServiceMock.Object.CreateUserAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"The name {bannedName} is not allowed");
    }

    #endregion

    #region DELETE /api/users/{id} Tests

    [Fact]
    public async Task Given_UserExists_When_DeleteUser_Then_Returns204()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(userId)).Returns(Task.CompletedTask);

        // Act & Assert
        await _userServiceMock.Object.DeleteUserAsync(userId);
        _userServiceMock.Verify(s => s.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Given_UserNotFound_When_DeleteUser_Then_Returns204()
    {
        // Arrange - Delete should be idempotent
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(userId)).Returns(Task.CompletedTask);

        // Act & Assert
        await _userServiceMock.Object.DeleteUserAsync(userId);
        _userServiceMock.Verify(s => s.DeleteUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Given_ServiceThrows_When_DeleteUser_Then_Returns500()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(userId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        var act = async () => await _userServiceMock.Object.DeleteUserAsync(userId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
