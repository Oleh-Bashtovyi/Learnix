using System.Net;
using Learnix.IntegrationTests.Infrastructure;

namespace Learnix.IntegrationTests.Lessons;

/// <summary>DELETE /api/courses/{courseId}/lessons/{lessonId}.</summary>
public sealed class DeleteLessonTests(LearnixApp app) : IntegrationTestBase(app)
{
    private static string Url(Guid courseId, Guid lessonId) => $"/api/courses/{courseId}/lessons/{lessonId}";

    [Fact]
    public async Task The_owner_deletes_a_lesson_and_it_leaves_the_section()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var deleted = await course.Owner.DeleteAsync(Url(course.CourseId, lessonId));

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await course.LessonsAsync(sectionId)).Should().NotContain(l => l.Id == lessonId);
    }

    [Fact]
    public async Task Deleting_an_unknown_lesson_is_not_found()
    {
        var course = await App.NewCourseAsync();

        var response = await course.Owner.DeleteAsync(Url(course.CourseId, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_instructor_who_does_not_own_the_course_is_forbidden()
    {
        var course = await App.NewCourseAsync();
        var sectionId = await course.AddSectionAsync("Intro");
        var lessonId = await course.AddPostLessonAsync(sectionId, "Lesson");

        var response = await App.Stranger().DeleteAsync(Url(course.CourseId, lessonId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
