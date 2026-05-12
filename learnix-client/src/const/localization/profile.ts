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
    NAV: {
        SECTION_TITLE: 'My Learning',
        ACHIEVEMENTS_TITLE: 'Achievements',
        ACHIEVEMENTS_DESC: 'View your earned badges and progress',
        CERTIFICATES_TITLE: 'Certificates',
        CERTIFICATES_DESC: 'Download your course completion certificates',
    },
} as const;
