using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class DesignPatternsSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Design Patterns in C#",
        "Learn the most important Gang of Four design patterns with practical C# examples. Assumes basic OOP knowledge.",
        29.99m,
        ["csharp", "design-patterns", "intermediate"],
        [
            new("Introduction to Design Patterns",
            [
                new SeedVideo("What Are Design Patterns?",
                    "We explore the history of design patterns, the Gang of Four book, and why knowing patterns makes you a better communicator and architect."),
                new SeedPost("History and the Gang of Four",
                    "In 1994 Gamma, Helm, Johnson, and Vlissides published 'Design Patterns: Elements of Reusable Object-Oriented Software'. The book catalogues 23 patterns in three categories: Creational, Structural, and Behavioral. Patterns are solutions to recurring problems — not copy-paste code, but a shared vocabulary."),
            ]),
            new("Creational Patterns",
            [
                new SeedVideo("Creational Patterns Overview",
                    "A high-level tour of Singleton, Factory Method, Abstract Factory, Builder, and Prototype — with guidance on which to reach for in .NET applications."),
                new SeedPost("Singleton Pattern",
                    "Ensures a class has only one instance. In modern C#, prefer the DI container (singleton lifetime) — it is thread-safe and testable. If you must implement manually, use `Lazy<T>` which is guaranteed thread-safe. Avoid double-checked locking."),
                new SeedPost("Factory Method Pattern",
                    "Defines an interface for creating objects and lets subclasses decide the concrete type. In C# typically implemented as a static `Create()` factory or an abstract factory interface registered in DI. Use when the exact type to create is not known until runtime."),
                new SeedPost("Builder Pattern",
                    "Constructs complex objects step-by-step. Common in C#: `StringBuilder`, `WebApplication.CreateBuilder()`, and FluentValidation rule chains. Implement via a fluent API (returning `this`) so each method can be chained. Useful when a constructor would need more than 4 parameters."),
            ]),
            new("Structural Patterns",
            [
                new SeedVideo("Structural Patterns in Practice",
                    "We build a caching decorator and an adapter over a third-party email API, showing how structural patterns solve real integration problems without modifying existing classes."),
                new SeedPost("Repository Pattern",
                    "Abstracts data access behind an interface. Handlers depend on `ICourseRepository`, not EF Core directly — making unit testing straightforward and future persistence changes isolated. Implement queries via the Specification pattern to avoid LINQ leaking into handlers."),
                new SeedPost("Decorator Pattern",
                    "Attaches additional behaviour to an object dynamically by wrapping it. ASP.NET Core middleware and IHttpClientFactory pipelines are decorator chains. Use Scrutor's `.Decorate<IService, Decorator>()` for easy DI registration without losing the original registration."),
                new SeedVideo("Adapter and Facade",
                    "Adapter converts an incompatible interface into one a client expects. Facade simplifies a complex subsystem behind a single entry point. We build both against a legacy payment SDK."),
            ]),
            new("Behavioral Patterns",
            [
                new SeedVideo("Observer and Strategy",
                    "Observer decouples event producers from consumers; Strategy swaps algorithms at runtime. We implement both with .NET events and MediatR notifications."),
                new SeedPost("Observer Pattern",
                    "Defines a one-to-many dependency so that when one object changes state, all dependants are notified. In .NET: `event` keyword, `IObservable<T>`, or MediatR `INotificationHandler<T>`. Domain events in DDD are the Observer pattern applied to aggregate state changes."),
                new SeedPost("Strategy Pattern",
                    "Defines a family of algorithms, encapsulates each one, and makes them interchangeable. Inject the strategy via DI. Example: a tax calculation service that swaps strategies based on country code. Register all strategies as `IEnumerable<IStrategy>` and pick at runtime by key."),
            ]),
            new("Design Patterns Assessments",
            [
                new SeedTest("Quick Check: Pattern Identification",
                    [
                        SC("Which pattern ensures a class has exactly one instance?",
                            "Singleton", "Factory", "Prototype", "Builder"),
                        SC("Which pattern wraps an object to add behaviour without modifying it?",
                            "Decorator", "Adapter", "Proxy", "Strategy"),
                        SC("Which pattern converts one interface into another expected by the client?",
                            "Adapter", "Bridge", "Facade", "Mediator"),
                    ],
                    PassingThreshold: 60,
                    AttemptLimit: null),

                new SeedTest("Concept Quiz: Pattern Categories",
                    [
                        MC("Which are Creational patterns from the GoF catalogue?",
                            ["Singleton", "Factory Method", "Builder", "Prototype"],
                            ["Observer", "Strategy", "Adapter"]),
                        MC("Which patterns are commonly used in ASP.NET Core DI?",
                            ["Repository", "Factory", "Decorator"],
                            ["Flyweight", "Interpreter", "Memento"]),
                        MC("Which are Behavioral patterns?",
                            ["Strategy", "Observer", "Command", "Template Method"],
                            ["Singleton", "Facade", "Adapter"]),
                    ],
                    PassingThreshold: 70,
                    AttemptLimit: 3,
                    CooldownMinutes: 2),

                new SeedTest("Comprehensive Exam",
                    [
                        SC("How many patterns does the original GoF book define?",
                            "23", "12", "16", "36"),
                        MC("Which patterns reduce coupling between components?",
                            ["Observer", "Mediator", "Facade"],
                            ["Singleton", "Iterator"]),
                        TI("What pattern defines a skeleton algorithm in a base class with steps overridden by subclasses?",
                            "Template Method", ignoreCase: true),
                    ],
                    Description: "Covers GoF catalogue size, coupling-reduction patterns, and Template Method.",
                    PassingThreshold: 80,
                    AttemptLimit: 2,
                    CooldownMinutes: 3),

                new SeedTest("Terminology Test",
                    [
                        TI("What pattern ensures only one instance of a class exists?", "Singleton"),
                        TI("What pattern provides a simplified interface to a complex subsystem?", "Facade"),
                        TI("What pattern is most useful for implementing undo/redo?", "Command"),
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 1),

                new SeedTest("Mastery Challenge",
                    [
                        SC("Which .NET library simplifies Decorator registration in DI?",
                            "Scrutor", "Autofac", "Ninject", "StructureMap"),
                        SC("Which pattern is the foundation of MediatR's notification pipeline?",
                            "Observer", "Command", "Strategy", "Chain of Responsibility"),
                        MC("Which patterns support the Open/Closed Principle?",
                            ["Strategy", "Decorator", "Observer"],
                            ["Singleton", "Prototype"]),
                        TI("What three-letter acronym describes 'depend on abstractions, not concretions'?",
                            "DIP"),
                    ],
                    PassingThreshold: 100,
                    AttemptLimit: 5,
                    CooldownMinutes: 1),
            ]),
        ],
        "desing_paterns_thumbnail.png");
}


