using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class DigitalMarketingSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "marketing",
        "Digital Marketing Fundamentals",
        "Understand SEO, content marketing, paid advertising, email campaigns, and analytics. Build an online audience from zero.",
        9.99m,
        ["marketing", "seo", "analytics", "beginner"],
        [
            new("Marketing in the Digital Age",
            [
                new SeedVideo("The Digital Marketing Landscape",
                    "We map the full digital marketing funnel — Awareness → Interest → Decision → Action — and identify which channels drive each stage, from SEO and social to email and retargeting."),
                new SeedPost("Owned, Earned, and Paid Media",
                    "Owned media: your website, blog, email list, social profiles — assets you control. Earned media: press coverage, reviews, social shares — third-party validation you cannot buy directly. Paid media: search ads, display, sponsored posts — immediate reach with a cost. A mature marketing strategy balances all three."),
            ]),
            new("Search Engine Optimisation",
            [
                new SeedVideo("How Search Engines Work",
                    "We trace a search query from crawling → indexing → ranking, explore Google's core ranking signals, and demonstrate how a site's technical health directly affects organic visibility."),
                new SeedPost("Keyword Research",
                    "Keyword research identifies what your audience searches for. Use Google Search Console (free, shows actual queries) or Ahrefs/Semrush (paid). Target a mix of head terms (high volume, high competition) and long-tail phrases (lower volume, easier to rank, higher purchase intent). Map one primary keyword per page."),
                new SeedPost("On-Page SEO",
                    "On-page signals: `<title>` tag (50–60 chars, include primary keyword), `<meta description>` (preview text, 120–160 chars), H1 (one per page, matches search intent), internal links to related content, fast load time (<2.5s LCP). Write for humans first, then optimise. Keyword stuffing is penalised by modern Google algorithms."),
                new SeedPost("Link Building",
                    "Backlinks are votes of confidence. Quality matters more than quantity — one link from a high-authority domain outweighs 100 links from low-quality sites. Earn links by publishing original research, tools, or comprehensive guides worth citing. Avoid paid link schemes — they risk manual Google penalties."),
            ]),
            new("Content and Social Media",
            [
                new SeedVideo("Content Marketing Strategy",
                    "We build a content calendar for a hypothetical SaaS product: blog posts for SEO, short-form videos for social, a newsletter for retention, and a lead magnet for list growth."),
                new SeedPost("Creating High-Value Content",
                    "The 10× content rule: create content 10× better than anything currently ranking. Use the Skyscraper Technique: find high-ranking posts, identify gaps, and publish a more complete, more up-to-date version. Add original data, expert quotes, and visual assets. Repurpose: turn a long post into a video, infographic, and newsletter."),
                new SeedPost("Social Media Marketing",
                    "Choose channels based on audience, not trends. LinkedIn for B2B professionals; Instagram/TikTok for visual B2C brands; X (Twitter) for tech and media. Post at consistent times. The 80/20 rule: 80% educational or entertaining content, 20% promotional. Track engagement rate, not just follower count."),
                new SeedVideo("Paid Advertising: Google and Meta Ads",
                    "A beginner's walkthrough of Google Search Ads (intent-based), Google Display Ads (awareness), and Meta Ads (interest-based targeting). We set up a small campaign with a $10/day budget and analyse the results."),
            ]),
            new("Email and Analytics",
            [
                new SeedVideo("Email Marketing That Converts",
                    "We design a 5-email welcome sequence for a new subscriber, cover subject line best practices, segment a list by behaviour, and interpret open rates, click rates, and unsubscribe rates."),
                new SeedPost("Building and Segmenting an Email List",
                    "A permission-based list built slowly outperforms a purchased list dramatically. Offer a lead magnet (free guide, checklist, template) on a dedicated landing page. Segment by: source, interest, behaviour (opened last 3 emails vs inactive). Inactive subscribers hurt deliverability — run a re-engagement campaign, then remove non-responders."),
                new SeedPost("Google Analytics and Key Metrics",
                    "Install GA4 and verify data in the Realtime report. Key metrics: Sessions, Users, Bounce Rate, Avg Session Duration, Goal Completions. Set up Conversions for form submissions, purchases, and sign-ups. Use UTM parameters (`utm_source`, `utm_medium`, `utm_campaign`) on all off-site links to attribute traffic accurately."),
            ]),
            new("Marketing Assessments",
            [
                new SeedTest("Quick Check: Marketing Basics",
                    [
                        SC("What does SEO stand for?",
                            "Search Engine Optimisation", "Social Engagement Optimisation",
                            "Site Exposure Online", "Search Experience Offering"),
                        SC("Which type of media includes your own website and email list?",
                            "Owned media", "Earned media", "Paid media", "Shared media"),
                        SC("What metric measures the percentage of email recipients who click a link?",
                            "Click-through rate", "Open rate", "Conversion rate", "Bounce rate"),
                    ],
                    PassingThreshold: 60,
                    AttemptLimit: null),

                new SeedTest("Concept Quiz: SEO and Content",
                    [
                        MC("Which are on-page SEO ranking factors?",
                            ["<title> tag content", "H1 heading", "Page load speed", "Internal links"],
                            ["Domain age", "Number of social followers", "Hosting provider"]),
                        MC("Which are legitimate link-building strategies?",
                            ["Publishing original research", "Guest posting on relevant sites",
                             "Creating link-worthy tools or resources"],
                            ["Buying links in bulk", "Hiding links in white text", "Automated link exchanges"]),
                        MC("Which are common content marketing formats?",
                            ["Blog posts", "Video", "Newsletters", "Infographics", "Podcasts"],
                            ["Server logs", "Database backups"]),
                    ],
                    PassingThreshold: 70,
                    AttemptLimit: 3,
                    CooldownMinutes: 2),

                new SeedTest("Comprehensive Exam",
                    [
                        SC("What UTM parameter identifies the traffic source in a tagged URL?",
                            "utm_source", "utm_medium", "utm_campaign", "utm_term"),
                        MC("Which are key email marketing metrics to track?",
                            ["Open rate", "Click-through rate", "Unsubscribe rate", "Conversion rate"],
                            ["CPU usage", "Server uptime", "Database query time"]),
                        TI("What free Google tool shows which search queries bring users to your site?",
                            "Search Console", ignoreCase: true),
                    ],
                    Description: "Covers UTM parameters, email KPIs, and Google Search Console.",
                    PassingThreshold: 80,
                    AttemptLimit: 2,
                    CooldownMinutes: 3),

                new SeedTest("Terminology Test",
                    [
                        TI("What three-letter acronym stands for 'Call to Action'?", "CTA"),
                        TI("What word describes the path a user takes from first visit to purchase?", "funnel"),
                        TI("What adjective describes the unpaid search results on a Google results page?", "organic"),
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 1),

                new SeedTest("Mastery Challenge",
                    [
                        SC("What ad targeting approach shows ads to users who previously visited your site?",
                            "Retargeting", "Contextual targeting", "Lookalike audience", "Keyword targeting"),
                        SC("What UTM parameter identifies the marketing channel (e.g. email, cpc, social)?",
                            "utm_medium", "utm_source", "utm_campaign", "utm_content"),
                        MC("Which marketing tactics build long-term owned assets?",
                            ["Email list growth", "SEO content creation", "Brand community building"],
                            ["Paid search ads", "Sponsored Instagram posts", "Pop-up display ads"]),
                        TI("What email marketing practice sends different content to subgroups of your list?",
                            "segmentation", ignoreCase: true),
                    ],
                    PassingThreshold: 100,
                    AttemptLimit: 5,
                    CooldownMinutes: 1),
            ]),
        ],
        "marketing_thumbnail.png");
}


