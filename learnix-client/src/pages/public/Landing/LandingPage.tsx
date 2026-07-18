import { useTranslation } from 'react-i18next';
import { ProjectNoticeBanner } from '@/components/common/elements/ProjectNoticeBanner';
import { Seo } from '@/components/common/seo/Seo';
import { organizationJsonLd, webSiteJsonLd } from '@/utils/seo';
import { AIAssistantSection } from './components/AIAssistantSection';
import { CategoriesSection } from './components/CategoriesSection';
import { FaqSection } from './components/FaqSection';
import { FeaturedCoursesSection } from './components/FeaturedCoursesSection';
import { FinalCTASection } from './components/FinalCTASection';
import { HeroSection } from './components/HeroSection';
import { HowItWorksSection } from './components/HowItWorksSection';
import { InstructorsCTASection } from './components/InstructorsCTASection';
import { StatsSection } from './components/StatsSection';
import { TestimonialsSection } from './components/TestimonialsSection';

export default function LandingPage() {
    const { t } = useTranslation('landing');

    return (
        <>
            <Seo
                title={t('seo.title')}
                description={t('seo.description')}
                jsonLd={[organizationJsonLd(), webSiteJsonLd()]}
            />
            {/* <AnnouncementBar /> */}
            <ProjectNoticeBanner />
            <HeroSection />
            <StatsSection />
            <CategoriesSection />
            <FeaturedCoursesSection />
            <HowItWorksSection />
            <AIAssistantSection />
            <TestimonialsSection />
            <InstructorsCTASection />
            <FaqSection />
            <FinalCTASection />
        </>
    );
}
