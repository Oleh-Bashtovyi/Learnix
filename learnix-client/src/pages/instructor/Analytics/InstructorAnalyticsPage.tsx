import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { EarningsTab } from './tabs/EarningsTab';
import { EngagementTab } from './tabs/EngagementTab';
import { OverviewTab } from './tabs/OverviewTab';
import { ReviewsTab } from './tabs/ReviewsTab';
import { TestsTab } from './tabs/TestsTab';

const TABS = ['overview', 'reviews', 'tests', 'engagement', 'earnings'] as const;
type TabKey = (typeof TABS)[number];

export default function InstructorAnalyticsPage() {
    const { t } = useTranslation('instructorAnalytics');
    const [searchParams, setSearchParams] = useSearchParams();

    const param = searchParams.get('tab');
    const activeTab: TabKey = TABS.includes(param as TabKey) ? (param as TabKey) : 'overview';

    const selectTab = (tab: string) => {
        setSearchParams(
            (prev) => {
                const next = new URLSearchParams(prev);
                next.set('tab', tab);
                return next;
            },
            { replace: true },
        );
    };

    return (
        <div className="space-y-6 p-8">
            <header>
                <h1 className="font-heading text-3xl font-bold text-foreground">{t('title')}</h1>
                <p className="mt-1 text-muted-foreground">{t('subtitle')}</p>
            </header>

            {/* Radix unmounts the inactive tab's content, so each tab hits only its own endpoints when
                opened — no eager fetch of the whole dashboard. */}
            <Tabs value={activeTab} onValueChange={selectTab}>
                <TabsList className="w-full">
                    {TABS.map((tab) => (
                        <TabsTrigger key={tab} value={tab}>
                            {t(`tabs.${tab}`)}
                        </TabsTrigger>
                    ))}
                </TabsList>

                <TabsContent value="overview" className="mt-6">
                    <OverviewTab />
                </TabsContent>
                <TabsContent value="reviews" className="mt-6">
                    <ReviewsTab />
                </TabsContent>
                <TabsContent value="tests" className="mt-6">
                    <TestsTab />
                </TabsContent>
                <TabsContent value="engagement" className="mt-6">
                    <EngagementTab />
                </TabsContent>
                <TabsContent value="earnings" className="mt-6">
                    <EarningsTab />
                </TabsContent>
            </Tabs>
        </div>
    );
}
