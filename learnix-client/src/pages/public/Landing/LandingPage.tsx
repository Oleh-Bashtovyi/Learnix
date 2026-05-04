import { featuredCourses, landingCategories } from '@/mocks/landing.mock';
import { AIAssistantSection } from './components/AIAssistantSection';
import { AnnouncementBar } from './components/AnnouncementBar';
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
    return (
        <>
            <AnnouncementBar />
            <HeroSection />
            <StatsSection />
            <CategoriesSection categories={landingCategories} />
            <FeaturedCoursesSection courses={featuredCourses} />
            <HowItWorksSection />
            <AIAssistantSection />
            <TestimonialsSection />
            <InstructorsCTASection />
            <FaqSection />
            <FinalCTASection />
        </>
    );
}