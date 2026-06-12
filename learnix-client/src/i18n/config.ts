import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import enCommon from './locales/en/common.json';
import enHeader from './locales/en/header.json';
import enAuth from './locales/en/auth.json';
import enLanding from './locales/en/landing.json';
import enCatalog from './locales/en/catalog.json';
import enCourseDetail from './locales/en/courseDetail.json';
import enInstructor from './locales/en/instructor.json';
import enAdmin from './locales/en/admin.json';
import enAiChat from './locales/en/aiChat.json';
import enAchievements from './locales/en/achievements.json';
import enCertificates from './locales/en/certificates.json';
import enEmailConfirmation from './locales/en/emailConfirmation.json';
import enFaq from './locales/en/faq.json';
import enInstructorProfile from './locales/en/instructorProfile.json';
import enLessonPlayer from './locales/en/lessonPlayer.json';
import enMessages from './locales/en/messages.json';
import enMyLearning from './locales/en/myLearning.json';
import enPayment from './locales/en/payment.json';
import enProfile from './locales/en/profile.json';
import enTestLesson from './locales/en/testLesson.json';
import enWishlist from './locales/en/wishlist.json';
import enNotifications from './locales/en/notifications.json';

import ukCommon from './locales/uk/common.json';
import ukHeader from './locales/uk/header.json';
import ukAuth from './locales/uk/auth.json';
import ukLanding from './locales/uk/landing.json';
import ukCatalog from './locales/uk/catalog.json';
import ukCourseDetail from './locales/uk/courseDetail.json';
import ukInstructor from './locales/uk/instructor.json';
import ukAdmin from './locales/uk/admin.json';
import ukAiChat from './locales/uk/aiChat.json';
import ukAchievements from './locales/uk/achievements.json';
import ukCertificates from './locales/uk/certificates.json';
import ukEmailConfirmation from './locales/uk/emailConfirmation.json';
import ukFaq from './locales/uk/faq.json';
import ukInstructorProfile from './locales/uk/instructorProfile.json';
import ukLessonPlayer from './locales/uk/lessonPlayer.json';
import ukMessages from './locales/uk/messages.json';
import ukMyLearning from './locales/uk/myLearning.json';
import ukPayment from './locales/uk/payment.json';
import ukProfile from './locales/uk/profile.json';
import ukTestLesson from './locales/uk/testLesson.json';
import ukWishlist from './locales/uk/wishlist.json';
import ukNotifications from './locales/uk/notifications.json';

export const SUPPORTED_LANGUAGES = ['en', 'uk'] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

i18n.use(LanguageDetector)
    .use(initReactI18next)
    .init({
        resources: {
            en: {
                common: enCommon,
                header: enHeader,
                auth: enAuth,
                landing: enLanding,
                catalog: enCatalog,
                courseDetail: enCourseDetail,
                instructor: enInstructor,
                admin: enAdmin,
                aiChat: enAiChat,
                achievements: enAchievements,
                certificates: enCertificates,
                emailConfirmation: enEmailConfirmation,
                faq: enFaq,
                instructorProfile: enInstructorProfile,
                lessonPlayer: enLessonPlayer,
                messages: enMessages,
                myLearning: enMyLearning,
                payment: enPayment,
                profile: enProfile,
                testLesson: enTestLesson,
                wishlist: enWishlist,
                notifications: enNotifications,
            },
            uk: {
                common: ukCommon,
                header: ukHeader,
                auth: ukAuth,
                landing: ukLanding,
                catalog: ukCatalog,
                courseDetail: ukCourseDetail,
                instructor: ukInstructor,
                admin: ukAdmin,
                aiChat: ukAiChat,
                achievements: ukAchievements,
                certificates: ukCertificates,
                emailConfirmation: ukEmailConfirmation,
                faq: ukFaq,
                instructorProfile: ukInstructorProfile,
                lessonPlayer: ukLessonPlayer,
                messages: ukMessages,
                myLearning: ukMyLearning,
                payment: ukPayment,
                profile: ukProfile,
                testLesson: ukTestLesson,
                wishlist: ukWishlist,
                notifications: ukNotifications,
            },
        },
        fallbackLng: 'en',
        supportedLngs: SUPPORTED_LANGUAGES,
        interpolation: {
            escapeValue: false,
        },
        detection: {
            order: ['localStorage', 'navigator'],
            caches: ['localStorage'],
            lookupLocalStorage: 'learnix-language',
        },
        pluralSeparator: '_',
    });

export default i18n;
