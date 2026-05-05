export const LANDING_PAGE = {
    ANNOUNCEMENT: {
        text: '🎓 New: AI-assisted learning is now in every course.',
        linkLabel: 'See how →',
        linkHref: '#features',
    },

    HERO: {
        badge: 'New cohorts every week',
        heading: {
            line1: 'Learn skills that',
            highlight: 'ship',
            line2: 'actually',
        },
        subtitle:
            "Project-based courses from real practitioners. A built-in AI tutor that's available 24/7. Certificates you can actually share.",
        cta: {
            primary: 'Browse courses',
            secondary: 'Become an instructor',
        },
        socialProof: 'Join 85,000+ learners worldwide',
        aiTutorCard: {
            label: 'AI Tutor',
            question: 'Why does',
            codeSnippet: 'useEffect',
            questionEnd: 'run twice in dev mode?',
            answer: 'Strict Mode mounts components twice to surface side-effect bugs early...',
        },
        achievementCard: {
            label: 'Achievement unlocked',
            title: 'Knowledge Seeker',
            xp: '+50 XP',
        },
        progressCard: {
            label: 'Course progress',
            title: 'Advanced React Patterns',
            progress: '32 of 48 lessons · 68%',
        },
    },

    STATS: [
        { value: '1,200+', label: 'Courses across\n20+ categories' },
        { value: '85k', label: 'Active learners\nworldwide' },
        { value: '4.8★', label: 'Average course\nrating', highlightStar: true },
        { value: '24/7', label: 'AI tutor\navailability' },
    ] as Array<{ value: string; label: string; highlightStar?: boolean }>,

    CATEGORIES: {
        tag: 'Browse',
        heading: 'Explore by category',
        viewAll: 'All categories →',
        coursesLabel: 'courses',
    },

    FEATURED_COURSES: {
        tag: 'Popular',
        heading: 'Top courses this week',
        subtitle: 'Hand-picked by our community of learners',
        viewAll: 'View all courses →',
        viewMore: 'View 1,200+ more courses →',
    },

    HOW_IT_WORKS: {
        tag: 'Process',
        heading: 'How Learnix works',
        subtitle: 'From browsing to certification in three simple steps',
        steps: [
            {
                n: 1,
                title: 'Pick your course',
                text: 'Browse 1,200+ courses by category, rating, or price. Try free lessons before you commit.',
            },
            {
                n: 2,
                title: 'Learn at your pace',
                text: 'Video, articles, quizzes. Stuck? The AI tutor is always one click away to explain anything.',
            },
            {
                n: 3,
                title: 'Earn your certificate',
                text: 'Complete the course → unlock a verifiable certificate. Share it on LinkedIn or your portfolio.',
            },
        ],
    },

    AI_ASSISTANT: {
        badge: '✨ Powered by Claude',
        heading: {
            line1: 'Your personal',
            line2: 'AI tutor.',
            highlight: 'Always on.',
        },
        subtitle:
            'No more hunting through Stack Overflow at 2 AM. Ask the AI tutor anything — it knows your course context, your progress, and how you learn best.',
        features: [
            'Streaming responses — get answers as fast as you can read them',
            "Knows the context of every lesson you've taken",
            'Explains code, debugs, and quizzes you on weak spots',
            'Conversation history saved across all your devices',
        ],
        chat: {
            title: 'AI Tutor',
            status: 'Active · Lesson context loaded',
            inputPlaceholder: 'Ask anything about this lesson...',
            userLabel: 'YOU',
            aiLabel: '✨',
            messages: {
                q1: 'Why does',
                q1Code: 'useEffect',
                q1End: 'run twice in development?',
                a1: "Great question — this is React's",
                a1Bold: 'Strict Mode',
                a1End: 'in dev. It intentionally mounts components twice to surface side-effect bugs early.',
                a1Note: 'In production it runs once. Want me to show how to detect and fix common Strict Mode issues?',
                q2: 'Yes please',
            },
        },
    },

    TESTIMONIALS: {
        tag: 'Reviews',
        heading: 'Loved by learners',
        subtitle: '85,000+ students, 4.8 average rating across 1,200+ courses',
        items: [
            {
                initials: 'MK',
                name: 'Maksym K.',
                role: 'Frontend Engineer · Kyiv',
                rating: 5 as 4 | 5,
                text: "Best React course I've taken. Jane shows real-world code, not toy examples. The performance module alone is worth the price.",
                avatarBg: 'bg-primary/20',
            },
            {
                initials: 'SJ',
                name: 'Sarah Johnson',
                role: 'Self-taught dev · Berlin',
                rating: 5 as 4 | 5,
                text: "The AI tutor is a game changer. I no longer feel stuck. It explains things in ways tutorials never did, and it knows where I'm in the course.",
                avatarBg: 'bg-accent/20',
            },
            {
                initials: 'RT',
                name: 'Rohan T.',
                role: 'Junior Developer · Bengaluru',
                rating: 5 as 4 | 5,
                text: "Got my first dev job after finishing two Learnix courses and shipping the projects to GitHub. The certificates actually got recruiters' attention.",
                avatarBg: 'bg-warning/20',
            },
            {
                initials: 'LO',
                name: 'Léa Okafor',
                role: 'Product Designer · Paris',
                rating: 5 as 4 | 5,
                text: 'Finally a platform that respects my time. Clear curriculum, no fluff, no hour-long intros. I can binge a section on a lunch break.',
                avatarBg: 'bg-success/20',
            },
            {
                initials: 'DC',
                name: 'David Chen',
                role: 'Instructor · San Francisco',
                rating: 5 as 4 | 5,
                text: "As an instructor, the dashboard is clean and the editor doesn't get in my way. I shipped my first course in a weekend.",
                avatarBg: 'bg-primary/20',
            },
            {
                initials: 'AN',
                name: 'Aiko Nakamura',
                role: 'Full-stack dev · Tokyo',
                rating: 4 as 4 | 5,
                text: "Course quality varies (as it does everywhere) but ratings make it easy to find the gems. I've completed 12 courses and only one was meh.",
                avatarBg: 'bg-accent/20',
            },
        ],
    },

    INSTRUCTORS_CTA: {
        tag: 'For instructors',
        heading: {
            line1: 'Teach what you know.',
            line2: 'Earn while you sleep.',
        },
        subtitle:
            'Share your expertise with thousands of learners. Our editor handles video hosting, payments, and certificates — so you can focus on creating great content.',
        stats: {
            earnings: {
                value: '$2.4k',
                label: 'Average monthly earnings (top 100 instructors)',
            },
            revenue: {
                value: '85%',
                label: 'Revenue share — among the highest in the industry',
            },
        },
        cta: {
            primary: 'Apply to teach',
            secondary: 'Learn more',
        },
        preview: {
            instructorName: 'Jane Developer',
            instructorMeta: '12,484 students · 7 courses',
            thisMonth: 'This month',
            thisMonthValue: '$3,240',
            thisMonthGrowth: '↑ 18%',
            newStudents: 'New students',
            newStudentsValue: '187',
            newStudentsGrowth: '↑ 12%',
            enrollmentsLabel: 'Enrollments (last 14 days)',
        },
    },

    FAQ: {
        tag: 'FAQ',
        heading: 'Common questions',
        subtitle: "Can't find what you need?",
        contactLabel: 'Contact support',
        viewAll: 'View all questions →',
        items: [
            {
                q: 'How does Learnix differ from other LMS platforms?',
                a: "Three things: (1) Every course has an AI tutor that knows your progress and lesson context, available 24/7. (2) We focus on project-based learning — you finish with shippable artifacts, not just notes. (3) Our instructors keep 85% of revenue, so they're motivated to deliver real value.",
                defaultOpen: true,
            },
            {
                q: 'Are courses one-time purchase or subscription?',
                a: 'One-time purchase — you own a course forever once you buy it. There are no recurring fees, no expiring access. Free courses stay free.',
                defaultOpen: false,
            },
            {
                q: "What if I don't like a course after buying?",
                a: "Every paid course offers a free preview lesson, and we have a 30-day refund policy if you've completed less than 50% of the course. No questions, no friction.",
                defaultOpen: false,
            },
            {
                q: 'Does the AI tutor cost extra?',
                a: 'No. Unlimited AI tutor usage is included with every course you own — both free and paid. Conversation history is saved across sessions and devices.',
                defaultOpen: false,
            },
        ],
    },

    FINAL_CTA: {
        heading: {
            line1: 'Ready to learn something',
            line2: "you'll actually use?",
        },
        subtitle:
            'Join 85,000+ learners. Start with a free course today — no credit card required.',
        cta: {
            primary: 'Get started free',
            secondary: 'Browse courses',
        },
        guarantees: '✓ Free forever for free courses · ✓ 30-day refund on paid · ✓ Cancel anytime',
    },
} as const;
