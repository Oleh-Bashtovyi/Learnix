namespace Learnix.Application.Wishlist.Queries.GetMyWishlist;

public sealed record WishlistCourseDto(
    Guid CourseId,
    string Title,
    string? CoverImageUrl,
    decimal Price,
    bool IsFree,
    DateTime AddedAt);
