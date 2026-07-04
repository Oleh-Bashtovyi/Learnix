using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Errors;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Commands.AdminRemoveRole;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Domain.UnitTests.Users;

public class AdminRemoveRoleCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserRoleService _roleService = Substitute.For<IUserRoleService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AdminRemoveRoleCommandHandler _sut;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();

    public AdminRemoveRoleCommandHandlerTests()
    {
        _sut = new AdminRemoveRoleCommandHandler(_currentUser, _userRepository, _roleService, _unitOfWork);

        // Default: authenticated admin
        _currentUser.UserId.Returns(AdminId);
        _currentUser.IsInRole(Roles.Admin).Returns(true);
    }

    // Auth / authorisation 

    [Fact]
    public async Task Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Instructor), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is AuthenticationError);
    }

    [Fact]
    public async Task Should_Fail_When_Caller_Is_Not_Admin()
    {
        _currentUser.IsInRole(Roles.Admin).Returns(false);

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Instructor), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ForbiddenError);
    }

    // Target user lookup 

    [Fact]
    public async Task Should_Fail_When_Target_User_Not_Found()
    {
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<AdminUserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Instructor), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    // Role preconditions 

    [Fact]
    public async Task Should_Fail_When_User_Does_Not_Have_Role()
    {
        SetupUserFound();
        _roleService.GetRolesAsync(TargetId, Arg.Any<CancellationToken>())
                    .Returns(new List<string> { Roles.Student });

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Instructor), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task Should_Fail_When_Admin_Tries_To_Remove_Own_Admin_Role()
    {
        SetupUserFound();
        _roleService.GetRolesAsync(AdminId, Arg.Any<CancellationToken>())
                    .Returns(new List<string> { Roles.Admin, Roles.Student });

        var result = await _sut.Handle(new AdminRemoveRoleCommand(AdminId, Roles.Admin), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    [Fact]
    public async Task Should_Fail_When_Removing_Last_Admin()
    {
        SetupUserFound();
        _roleService.GetRolesAsync(TargetId, Arg.Any<CancellationToken>())
                    .Returns(new List<string> { Roles.Admin, Roles.Student });
        _roleService.CountUsersInRoleAsync(Roles.Admin, Arg.Any<CancellationToken>())
                    .Returns(1);

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Admin), default);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is ConflictError);
    }

    // Happy paths 

    [Fact]
    public async Task Should_Succeed_When_Removing_Instructor_Role()
    {
        SetupUserFound();
        _roleService.GetRolesAsync(TargetId, Arg.Any<CancellationToken>())
                    .Returns(new List<string> { Roles.Student, Roles.Instructor });

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Instructor), default);

        result.IsSuccess.Should().BeTrue();
        await _roleService.Received(1)
                          .RemoveRoleAsync(TargetId, Roles.Instructor, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Succeed_When_Removing_Admin_Role_If_Other_Admins_Exist()
    {
        SetupUserFound();
        _roleService.GetRolesAsync(TargetId, Arg.Any<CancellationToken>())
                    .Returns(new List<string> { Roles.Admin, Roles.Student });
        _roleService.CountUsersInRoleAsync(Roles.Admin, Arg.Any<CancellationToken>())
                    .Returns(2);

        var result = await _sut.Handle(new AdminRemoveRoleCommand(TargetId, Roles.Admin), default);

        result.IsSuccess.Should().BeTrue();
        await _roleService.Received(1)
                          .RemoveRoleAsync(TargetId, Roles.Admin, Arg.Any<CancellationToken>());
    }

    // Helpers 

    private void SetupUserFound()
    {
        var user = new User("test@example.com", "Test", "User");
        _userRepository
            .FirstOrDefaultAsync(Arg.Any<AdminUserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(user);
    }
}
