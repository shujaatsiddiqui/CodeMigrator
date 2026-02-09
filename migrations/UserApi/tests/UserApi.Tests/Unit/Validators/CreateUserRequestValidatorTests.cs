using FluentAssertions;
using UserApi.Library.Application.DTOs;
using UserApi.Library.Application.Validators;

namespace UserApi.Tests.Unit.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _sut;

    public CreateUserRequestValidatorTests()
    {
        _sut = new CreateUserRequestValidator();
    }

    [Fact]
    public void Given_ValidRequest_When_Validate_Then_ReturnsValid()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = 25 };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_EmptyName_When_Validate_Then_ReturnsInvalid(string? name)
    {
        // Arrange
        var request = new CreateUserRequest { Name = name!, Age = 25 };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Given_InvalidAge_When_Validate_Then_ReturnsInvalid(int age)
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = age };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    [Fact]
    public void Given_AgeTooHigh_When_Validate_Then_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest { Name = "John", Age = 151 };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Age");
    }

    [Fact]
    public void Given_NullRequest_When_Validate_Then_ReturnsInvalid()
    {
        // Arrange
        CreateUserRequest? request = null;

        // Act
        var result = _sut.Validate(request!);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sa")]
    public void Given_BannedName_When_Validate_Then_ReturnsInvalid(string bannedName)
    {
        // Arrange
        var request = new CreateUserRequest { Name = bannedName, Age = 25 };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("not allowed"));
    }
}
