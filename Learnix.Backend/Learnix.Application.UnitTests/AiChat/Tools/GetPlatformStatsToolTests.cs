using FluentResults;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Courses.Queries.GetPublishedCourseCount;
using MediatR;

namespace Learnix.Application.UnitTests.AiChat.Tools;

public class GetPlatformStatsToolTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly GetPlatformStatsTool _sut;

    public GetPlatformStatsToolTests()
    {
        _sut = new GetPlatformStatsTool(_mediator);
    }

    [Fact]
    public void IsAvailableIn_ShouldBePlatformOnly()
    {
        _sut.IsAvailableIn(ChatScopeType.Platform).Should().BeTrue();
        _sut.IsAvailableIn(ChatScopeType.Course).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPublishedCourseCount_AsAJsonObject()
    {
        // Arrange
        _mediator
            .Send(Arg.Any<GetPublishedCourseCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(128));

        // Act
        var payload = await _sut.ExecuteAsync(
            new ChatToolInvocation("{}", new ChatToolContext(null, null)), CancellationToken.None);

        // Assert
        payload.Should().Be("{\"publishedCourseCount\":128}");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnError_WhenTheQueryFails()
    {
        // Arrange
        _mediator
            .Send(Arg.Any<GetPublishedCourseCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<int>("boom"));

        // Act
        var payload = await _sut.ExecuteAsync(
            new ChatToolInvocation("{}", new ChatToolContext(null, null)), CancellationToken.None);

        // Assert
        payload.Should().Contain("error");
    }
}
