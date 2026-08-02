import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useMyCoursesQuery } from '@/hooks/instructor/useMyCoursesQuery';
import { cn } from '@/utils/cn';
import { CourseFilter } from './components/CourseFilter';
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

    // Owned here rather than inside ReviewsTab: it sits in the tab row next to the tab list (not on
    // a row of its own above the charts), and living on the page means it isn't reset every time
    // Radix unmounts the inactive tab and the instructor switches back to Reviews.
    const { data: coursesData } = useMyCoursesQuery({ take: 100 });
    const courses = coursesData?.items ?? [];
    // Seeded from the URL only when it's this tab's own deep link (a course row's "View reviews" /
    // "View test results" shortcut) — otherwise a link into one tab would also silently preset the
    // other's selection the first time it's opened.
    const [reviewsCourseId, setReviewsCourseId] = useState(() =>
        activeTab === 'reviews' ? (searchParams.get('courseId') ?? '') : '',
    );
    // Tests has no "all courses" option — unlike Reviews, it isn't just a display filter: the query
    // itself only loads section/lesson data for the course(s) being asked about, so defaulting to
    // "all" would mean fetching every course's full curriculum on every visit. Defaults to the first
    // course so the tab is never empty on open, same as the drop-off chart on Engagement.
    const [testsCoursePicked, setTestsCoursePicked] = useState(() =>
        activeTab === 'tests' ? (searchParams.get('courseId') ?? '') : '',
    );
    const testsCourseId = testsCoursePicked || courses[0]?.id || '';

    return (
        <div className="space-y-6 p-8">
            <header>
                <h1 className="font-heading text-3xl font-bold text-foreground">{t('title')}</h1>
                <p className="mt-1 text-muted-foreground">{t('subtitle')}</p>
            </header>

            {/* Radix unmounts the inactive tab's content, so each tab hits only its own endpoints when
                opened — no eager fetch of the whole dashboard. */}
            <Tabs value={activeTab} onValueChange={selectTab}>
                {/* The course filter only means something on Reviews and Tests, but it lives in this
                    row — not a row of its own above those tabs' charts — so it costs no extra
                    vertical space on the tabs that need it. Always mounted (never conditionally
                    rendered) and hidden via `invisible` rather than `hidden` on the other tabs: an
                    element that only sometimes takes part in the layout changes the row's height
                    when it appears, which reads as the whole tab bar jumping. `invisible` keeps its
                    footprint reserved at all times, so switching tabs never resizes this row.
                    `items-end` + its own bottom margin (unlike the tabs, whose active-indicator
                    border is deliberately flush with the line below) is what keeps the taller
                    select control from touching that border. Reviews and Tests keep independent
                    selections — picking a course on one doesn't carry over to the other. */}
                <div className="flex flex-wrap items-end justify-between gap-x-4 gap-y-2 border-b border-border">
                    <TabsList className="border-b-0">
                        {TABS.map((tab) => (
                            <TabsTrigger key={tab} value={tab}>
                                {t(`tabs.${tab}`)}
                            </TabsTrigger>
                        ))}
                    </TabsList>
                    <CourseFilter
                        courses={courses}
                        value={activeTab === 'tests' ? testsCourseId : reviewsCourseId}
                        onChange={activeTab === 'tests' ? setTestsCoursePicked : setReviewsCourseId}
                        includeAll={activeTab !== 'tests'}
                        className={cn(
                            'mb-2',
                            activeTab !== 'reviews' && activeTab !== 'tests' && 'invisible',
                        )}
                    />
                </div>

                <TabsContent value="overview" className="mt-6">
                    <OverviewTab />
                </TabsContent>
                <TabsContent value="reviews" className="mt-6">
                    <ReviewsTab courseId={reviewsCourseId} />
                </TabsContent>
                <TabsContent value="tests" className="mt-6">
                    <TestsTab courseId={testsCourseId} />
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
