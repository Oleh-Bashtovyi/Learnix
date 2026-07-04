using FluentValidation.TestHelper;
using Learnix.Application.Users.Commands.AdminRemoveRole;
using Learnix.Domain.Constants;

namespace Learnix.Domain.UnitTests.Users;

public class AdminRemoveRoleValidatorTests
{
    private readonly AdminRemoveRoleValidator _sut = new();
    private static readonly Guid AnyUserId = Guid.NewGuid();

    [Fact]
    public void Should_Pass_For_Valid_Instructor_Role()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, Roles.Instructor));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_For_Valid_Admin_Role()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, Roles.Admin));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Role_Is_Student()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, Roles.Student));

        result.ShouldHaveValidationErrorFor(x => x.Role)
              .WithErrorMessage($"The '{Roles.Student}' role is the base role and cannot be removed.");
    }

    [Fact]
    public void Should_Fail_When_Role_Is_Unknown()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, "SuperAdmin"));

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Should_Fail_When_Role_Is_Empty()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, string.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Role)
              .WithErrorMessage("Role must not be empty.");
    }

    [Fact]
    public void Should_Fail_When_UserId_Is_Empty()
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(Guid.Empty, Roles.Instructor));

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Theory]
    [InlineData("student")]
    [InlineData("STUDENT")]
    [InlineData("Student")]
    public void Should_Fail_For_Student_Role_Regardless_Of_Case(string role)
    {
        var result = _sut.TestValidate(new AdminRemoveRoleCommand(AnyUserId, role));

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
