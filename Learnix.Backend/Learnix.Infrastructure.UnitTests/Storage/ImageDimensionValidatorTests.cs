using Learnix.Infrastructure.Storage;

namespace Learnix.Infrastructure.UnitTests.Storage;

public class ImageDimensionValidatorTests
{
    private static readonly ImageDimensionValidator.Rule SquareRule = new(MinWidth: 100, MinHeight: 100, Aspect: 1.0);
    private static readonly ImageDimensionValidator.Rule WideRule = new(MinWidth: 640, MinHeight: 360, Aspect: 16.0 / 9.0);

    [Theory]
    [InlineData(100, 100)]     // exactly the minimum
    [InlineData(512, 512)]     // the cropper's actual output size
    [InlineData(2000, 2000)]
    public void Validate_WhenSquareImageMeetsMinimumAndAspect_ShouldReturnNull(int width, int height)
        => ImageDimensionValidator.Validate(width, height, SquareRule).Should().BeNull();

    [Theory]
    [InlineData(99, 100)]
    [InlineData(100, 99)]
    [InlineData(1, 1)]
    public void Validate_WhenSquareImageIsSmallerThanMinimum_ShouldReturnAMessage(int width, int height)
        => ImageDimensionValidator.Validate(width, height, SquareRule).Should().NotBeNull();

    [Theory]
    [InlineData(100, 200)]    // 1:2, nowhere near square
    [InlineData(200, 100)]    // 2:1
    public void Validate_WhenAspectIsOffByMoreThanTolerance_ShouldReturnAMessage(int width, int height)
        => ImageDimensionValidator.Validate(width, height, SquareRule).Should().NotBeNull();

    [Fact]
    public void Validate_WhenAspectIsWithinTolerance_ShouldReturnNull()
    {
        // Arrange — 101x100 is a 1.01 aspect, inside the 2% slack around 1.0
        const int width = 101;
        const int height = 100;

        // Act
        var result = ImageDimensionValidator.Validate(width, height, SquareRule);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Validate_WhenAspectIsJustOutsideTolerance_ShouldReturnAMessage()
    {
        // Arrange — 103x100 is a 1.03 aspect, outside the 2% slack around 1.0
        const int width = 103;
        const int height = 100;

        // Act
        var result = ImageDimensionValidator.Validate(width, height, SquareRule);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(640, 360)]    // exactly the minimum, exactly 16:9
    [InlineData(1280, 720)]   // the cropper's actual output size
    public void Validate_WhenWideImageMeetsMinimumAndAspect_ShouldReturnNull(int width, int height)
        => ImageDimensionValidator.Validate(width, height, WideRule).Should().BeNull();

    [Fact]
    public void Validate_WhenWideImageIsSmallerThanMinimum_ShouldReturnAMessage()
        => ImageDimensionValidator.Validate(639, 360, WideRule).Should().NotBeNull();

    [Fact]
    public void Validate_WhenWideImageAspectIsWrong_ShouldReturnAMessage()
        // 4:3, not 16:9
        => ImageDimensionValidator.Validate(640, 480, WideRule).Should().NotBeNull();
}
