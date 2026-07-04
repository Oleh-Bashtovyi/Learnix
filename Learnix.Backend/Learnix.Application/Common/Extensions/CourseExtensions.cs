using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;

namespace Learnix.Application.Common.Extensions;

internal static class CourseExtensions
{
    internal static bool IsOwnerOrAdmin(this Course course, ICurrentUserService currentUser)
    {
        return course.InstructorId == currentUser.UserId || currentUser.IsInRole(Roles.Admin);
    }
}
