import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import i18n, { type SupportedLanguage } from '@/i18n/config';

interface LocaleState {
    language: SupportedLanguage;
    setLanguage: (lang: SupportedLanguage) => void;
}

export const useLocaleStore = create<LocaleState>()(
    persist(
        (set) => ({
            language: 'en',
            setLanguage: (lang) => {
                i18n.changeLanguage(lang);
                set({ language: lang });
            },
        }),
        {
            name: 'locale-store',
            onRehydrateStorage: () => (state) => {
                if (state) i18n.changeLanguage(state.language);
            },
        },
    ),
);
