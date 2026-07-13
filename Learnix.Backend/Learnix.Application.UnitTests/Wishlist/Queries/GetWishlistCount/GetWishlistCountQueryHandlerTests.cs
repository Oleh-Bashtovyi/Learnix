using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Wishlist.Abstractions;
using Learnix.Application.Wishlist.Queries.GetWishlistCount;

namespace Learnix.Application.UnitTests.Wishlist.Queries.GetWishlistCount;

public class GetWishlistCountQueryHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>();
    private readonly GetWishlistCountQueryHandler _sut;

    public GetWishlistCountQueryHandlerTests()
    {
        _sut = new GetWishlistCountQueryHandler(_currentUserService, _wishlistRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);
        var query = new GetWishlistCountQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.HasError<AuthenticationError>().Should().BeTrue();
        result.Errors[0].Message.Should().Be(CommonMessages.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_ShouldReturnCount_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        _wishlistRepository.CountAsync(userId, Arg.Any<CancellationToken>())
            .Returns(5);

        var query = new GetWishlistCountQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(5);
    }
}
