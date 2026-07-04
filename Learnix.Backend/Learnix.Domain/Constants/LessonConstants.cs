namespace Learnix.Domain.Constants;

public static class LessonConstants
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 5000;
    public const int ContentMaxLength = 50_000;


    // Video
    public const int VideoUrlMaxLength = 2048;
    public const int VideoDescriptionMaxLength = 2000;

    // Post
    public const int PostContentMaxLength = 50_000;

    // Test
    public const int DefaultPassingThreshold = 70;
    public const int MinPassingThreshold = 0;
    public const int MaxPassingThreshold = 100;
}
