using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class CloudArchitectureSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "web-development",
        "Cloud Architecture Fundamentals",
        "Learn how to design scalable, resilient, and secure systems in the cloud.",
        39.99m,
        ["cloud", "architecture", "aws", "azure", "gcp"],
        [
            new("Cloud Concepts",
            [
                new SeedVideo("What is Cloud Computing?", "An overview of IaaS, PaaS, and SaaS."),
                new SeedPost("Regions and Availability Zones", "Understanding geographic deployment in the cloud."),
            ]),
            new("Architecting for Scale",
            [
                new SeedVideo("Load Balancing and Auto Scaling", "How to handle variable traffic."),
                new SeedTest("Cloud Architecture Assessment",
                    [
                        SC("Which service model provides the highest level of control over infrastructure?", "IaaS", "PaaS", "SaaS", "FaaS"),
                        SC("Which cloud concept refers to the ability to automatically add resources to handle load?", "Elasticity", "Reliability", "Durability", "Latency"),
                        MC("Which are common cloud deployment models?", ["Public", "Private", "Hybrid"], ["Distributed", "Local"]),
                        MC("Which providers are considered the big three public clouds?", ["AWS", "Azure", "GCP"], ["DigitalOcean", "Linode"]),
                        TI("What does IaaS stand for?", "Infrastructure as a Service", ignoreCase: true, fuzzy: true),
                        TI("What does PaaS stand for?", "Platform as a Service", ignoreCase: true, fuzzy: false),
                        SC("What is the main benefit of a multi-AZ deployment?", "High Availability", "Lower Cost", "Higher Latency", "Simpler Management"),
                        SC("Which service routes traffic to multiple servers?", "Load Balancer", "Firewall", "NAT Gateway", "Router"),
                        MC("Which are compute services?", ["Virtual Machines", "Containers", "Serverless Functions"], ["Object Storage", "Block Storage"]),
                        TI("What does SaaS stand for?", "Software as a Service", ignoreCase: true, fuzzy: false),
                        SC("Which database type is best for unstructured data?", "NoSQL", "Relational", "SQL", "In-Memory"),
                        MC("Which are types of cloud storage?", ["Object", "Block", "File"], ["Compute", "Network"]),
                        TI("What type of storage is AWS S3?", "Object", ignoreCase: true, fuzzy: true),
                        SC("What principle suggests giving users only the permissions they need?", "Least Privilege", "Zero Trust", "Role-Based", "Defense in Depth"),
                        SC("What does a CDN do?", "Caches content close to users", "Computes data", "Stores databases", "Manages users"),
                        MC("Which are characteristics of cloud native applications?", ["Microservices", "Containerized", "Dynamically orchestrated"], ["Monolithic", "Stateful by default"]),
                        TI("What does CDN stand for?", "Content Delivery Network", ignoreCase: true, fuzzy: true),
                        TI("What network component prevents unauthorized access?", "Firewall", ignoreCase: true, fuzzy: false),
                        SC("Which architecture pattern decouples components using queues?", "Asynchronous Messaging", "Synchronous HTTP", "Monolithic", "Tightly Coupled"),
                        SC("What is a cold start in serverless computing?", "Initial latency when a function is first invoked", "Booting a VM", "Restarting a database", "Cooling the server room"),
                        MC("Which concepts apply to disaster recovery?", ["RPO", "RTO"], ["API", "SDK", "IDE"]),
                        TI("What does RPO stand for in disaster recovery?", "Recovery Point Objective", ignoreCase: true, fuzzy: false),
                        SC("Which type of scaling involves upgrading to a larger server?", "Vertical", "Horizontal", "Diagonal", "Auto")
                    ],
                    PassingThreshold: 75,
                    AttemptLimit: 5,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


