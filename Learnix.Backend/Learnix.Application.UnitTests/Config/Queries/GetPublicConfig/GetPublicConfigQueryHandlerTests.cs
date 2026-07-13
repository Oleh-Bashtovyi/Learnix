using Learnix.Application.Common.Options;
using Learnix.Application.Config.Queries.GetPublicConfig;
using Microsoft.Extensions.Options;

namespace Learnix.Application.UnitTests.Config.Queries.GetPublicConfig;

public class GetPublicConfigQueryHandlerTests
{
    private readonly IOptions<AiChatOptions> _options = Substitute.For<IOptions<AiChatOptions>>();
    private readonly GetPublicConfigQueryHandler _sut;

    public GetPublicConfigQueryHandlerTests()
    {
        _sut = new GetPublicConfigQueryHandler(_options);
    }

    [Fact]
    public async Task Handle_ShouldReturnPublicConfigDto()
    {
        // Arrange
        var aiChatOptions = new AiChatOptions { Provider = "TestProvider" };
        _options.Value.Returns(aiChatOptions);

        var query = new GetPublicConfigQuery();

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AiProvider.Should().Be("TestProvider");
    }
}
