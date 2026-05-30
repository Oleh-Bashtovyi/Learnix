export const PROFILE = {
    PAGE_TITLE: 'My Profile',
    SECTIONS: {
        PERSONAL: 'Personal Information',
        AVATAR: 'Profile Photo',
        PREFERENCES: 'Learning Preferences',
    },
    FIELDS: {
        FIRST_NAME: 'First Name',
        LAST_NAME: 'Last Name',
        EMAIL: 'Email',
        BIO: 'Bio',
        BIO_PLACEHOLDER: 'Tell others a bit about yourself...',
    },
    AVATAR: {
        UPLOAD: 'Upload Photo',
        CHANGE: 'Change Photo',
        REMOVE: 'Remove',
        UPLOADING: 'Uploading...',
        HINT: 'JPG or PNG, max 5 MB',
    },
    PREFERENCES: {
        COMING_SOON: 'Coming Soon',
        DESCRIPTION:
            'Category preferences will help personalise your course recommendations. This feature is under development.',
    },
    ACTIONS: {
        SAVE: 'Save Changes',
        SAVING: 'Saving...',
        CANCEL: 'Cancel',
    },
    MESSAGES: {
        SAVE_SUCCESS: 'Profile updated successfully',
        SAVE_ERROR: 'Failed to update profile',
    },
    ACHIEVEMENTS: {
        SECTION_TITLE: 'Achievements',
        EARNED_COUNT: (earned: number, total: number) => `${earned} of ${total} earned`,
        VIEW_ALL: 'View all achievements →',
    },
    CERTIFICATES_NAV: {
        TITLE: 'Certificates',
        DESC: 'Download your course completion certificates',
    },
    BECOME_INSTRUCTOR_NAV: {
        TITLE: 'Become an Instructor',
        DESC: 'Share your knowledge and earn by creating courses',
    },
} as const;
