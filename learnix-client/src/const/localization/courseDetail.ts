export const COURSE_DETAIL = {
    CURRICULUM: {
        TITLE: 'Course Curriculum',
        SECTION_COUNT: (n: number) => `${n} section${n !== 1 ? 's' : ''}`,
        LESSON_COUNT: (n: number) => `${n} lesson${n !== 1 ? 's' : ''}`,
        LESSON_TYPES: {
            Video: 'Video',
            Post: 'Article',
            Test: 'Quiz',
        },
    },
    ENROLL: {
        FREE: 'Enroll for Free',
        PAID: (price: number) => `Enroll — $${price}`,
        ENROLLED: 'Go to Course',
        ENROLLING: 'Enrolling...',
        LOGIN_REQUIRED: 'Sign in to enroll',
        OWN_COURSE: 'Your Course',
    },
    REVIEWS: {
        TITLE: 'Student Reviews',
        NO_REVIEWS: 'No reviews yet. Be the first to share your experience!',
        WRITE_REVIEW: 'Write a Review',
        EDIT_REVIEW: 'Edit Your Review',
        YOUR_REVIEW: 'Your Review',
        RATING_LABEL: 'Your Rating',
        COMMENT_PLACEHOLDER: 'Share what you liked or what could be improved...',
        SUBMIT: 'Submit Review',
        UPDATE: 'Update Review',
        DELETE: 'Delete Review',
        SUBMITTING: 'Submitting...',
        AVERAGE_RATING: 'Average Rating',
        REVIEW_COUNT: (n: number) => `${n} review${n !== 1 ? 's' : ''}`,
        ENROLL_TO_REVIEW: 'Enroll in this course to leave a review',
        COMPLETE_TO_REVIEW: 'Complete lessons to unlock reviews',
    },
    INSTRUCTOR: {
        LABEL: 'Instructor',
    },
    PRICE: {
        FREE: 'Free',
    },
    WISHLIST: {
        SAVE: 'Save',
        SAVED: 'Saved',
        SAVING: 'Saving...',
        REMOVING: 'Removing...',
    },
} as const;
