using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using Learnix.Infrastructure.Persistence;
using Learnix.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Services;

/// <summary>
/// Opt-in dev seeder: creates a seed instructor and published courses across system categories.
/// Activate by setting SeedDevData:Enabled = true in appsettings.Development.json.
/// Idempotent — skips entirely if the seed instructor already owns any courses.
/// Each course contains PostLessons, VideoLessons, and TestLessons (all 3 types, ≥5 each).
/// Tests deliberately vary PassingThreshold and AttemptLimit for app-testing purposes.
/// </summary>
public sealed class DevCourseSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment environment,
    IOptions<BlobStorageOptions> blobOptions,
    ILogger<DevCourseSeederHostedService> logger) : IHostedService
{
    // 100×100 blue (#3B82F6) → white horizontal gradient PNG — used as placeholder for both
    // course covers and lesson video blobs (dev only; the video blob path just needs to be non-empty).
    private static readonly byte[] PlaceholderPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAGQAAABkCAYAAABw4pVUAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8" +
        "YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAI7SURBVHhe7dGLao4BAIBh9yVJkiRJkiRJkkSSJEnSMGuG" +
        "WTMMs2aYNcMwa4ZZM8zaMMtx1gxzWjPs/87fq1yG3ucWnhkziwrM2ltg9v4Cc4oLzD1QYF5JgfmlAQsO" +
        "BiwsC1h0KGDxkYAl5QFLjwYsqwhYXhmw4ljAyqqAVcdDVp8MWVMdsvZUyLozIetrQjacDdlYG7KpLmTz" +
        "uZAt9SFbz0dsuxixvSFix6WInY0Ru5oidl+O2NMcUXQlYt+1iOKWiJLrMaU3Y8paYw7fiilvi6loj6m8" +
        "HVPVEXPiTkz1vZjTnTE19xNquxLquhPqHyRc6EloeJjQ+DihqTeh+UnC1b6Elv6EGwMprU9T2p6ntA+m" +
        "dLxIuTuU0vkypetVSvfrlJ43KY/epfQOZ/S9z+gfyRgYzXj2IWNwLGPoY8bLzxmvxzPefskY/pYx8j1n" +
        "9EfO2ETOp8mc8Z85X6dyvv/KmfiTMzmdM1XI+R3kTEcQxBAlkKSQ5fxjiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGG" +
        "GGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYoghhhhiiCGGGGKIIYYYYsh/HgJ/ATQ2" +
        "fg8xyAy9AAAAAElFTkSuQmCC");

    // ── Seed record hierarchy ─────────────────────────────────────────────────

    private abstract record SeedLesson(string Title);

    private record SeedPost(string Title, string Content) : SeedLesson(Title);

    private record SeedVideo(string Title, string? Description = null) : SeedLesson(Title);

    /// <param name="PassingThreshold">0–100 percentage needed to pass.</param>
    /// <param name="AttemptLimit">null = unlimited attempts.</param>
    /// <param name="CooldownMinutes">Minutes between attempts; null = no cooldown.</param>
    private record SeedTest(
        string Title,
        QuestionBlueprint[] Questions,
        string? Description = null,
        int PassingThreshold = LessonConstants.DefaultPassingThreshold,
        int? AttemptLimit = null,
        int? CooldownMinutes = null) : SeedLesson(Title);

    private record SeedSection(string Title, SeedLesson[] Lessons);

    private record SeedCourseDefinition(
        string CategorySlug,
        string Title,
        string Description,
        decimal Price,
        string[] Tags,
        SeedSection[] Sections);

    // ── Question builder helpers ──────────────────────────────────────────────

    /// <summary>Single-choice question. First argument is the correct answer; rest are wrong.</summary>
    private static QuestionBlueprint SC(string text, string correct, params string[] wrong)
        => new(text, QuestionType.SingleChoice,
            wrong.Prepend(correct)
                 .Select((t, i) => new QuestionOptionBlueprint(t, i == 0))
                 .ToArray(),
            null);

    /// <summary>Multiple-choice question.</summary>
    private static QuestionBlueprint MC(string text, string[] correct, string[] wrong)
        => new(text, QuestionType.MultipleChoice,
            correct.Select(t => new QuestionOptionBlueprint(t, true))
                   .Concat(wrong.Select(t => new QuestionOptionBlueprint(t, false)))
                   .ToArray(),
            null);

    /// <summary>Text-input question.</summary>
    private static QuestionBlueprint TI(string text, string answer,
        bool ignoreCase = true, bool fuzzy = false)
        => new(text, QuestionType.TextInput, null,
            new TextAnswerBlueprint(answer, ignoreCase, fuzzy));

    // ── Course definitions ────────────────────────────────────────────────────
    // Structure per course (5 V, 8 P, 5 T — all ≥5):
    //   Section 1 "Intro"      → [Video, Post]
    //   Section 2 "Foundations"→ [Video, Post, Post, Post]
    //   Section 3 "Core"       → [Video, Post, Post, Video]
    //   Section 4 "Advanced"   → [Video, Post, Post]
    //   Section 5 "Assessments"→ [Test ×5]
    //
    // Test PassingThreshold / AttemptLimit matrix per course (all 5 tests):
    //   #1 Quick Check:       PT=60, AttemptLimit=null  (unlimited)
    //   #2 Concept Quiz:      PT=70, AttemptLimit=3,    Cooldown=60 min
    //   #3 Comprehensive Exam:PT=80, AttemptLimit=2,    Cooldown=120 min  ← has ALL 3 question types
    //   #4 Terminology Test:  PT=50, AttemptLimit=1     (one shot)
    //   #5 Mastery Challenge: PT=100,AttemptLimit=5,    Cooldown=30 min

    private static readonly SeedCourseDefinition[] SeedCourses =
    [
        // ═══════════════════════════════════════════════════════════════
        // 1. C# Fundamentals
        // ═══════════════════════════════════════════════════════════════
        new("programming",
            "C# Fundamentals",
            "A complete introduction to C# for beginners. Learn variables, control flow, OOP, and the basics of the .NET ecosystem.",
            0m,
            ["csharp", "beginner", "dotnet"],
            [
                new("Getting Started",
                [
                    new SeedVideo("Course Introduction",
                        "An overview of what you will build by the end of this course: a fully functional C# console application using OOP, LINQ, and async/await."),
                    new SeedPost("Why Learn C#?",
                        "C# powers everything from enterprise web apps (ASP.NET Core) to game development (Unity) and mobile apps (MAUI). It is statically typed, garbage-collected, and runs on the cross-platform .NET 8 runtime."),
                ]),
                new("Core Language Concepts",
                [
                    new SeedVideo("Variables, Types, and Operators",
                        "A hands-on walkthrough of C#'s built-in types, arithmetic operators, and the difference between value and reference semantics."),
                    new SeedPost("Variables and Data Types",
                        "C# is strongly typed. Value types (int, bool, decimal, struct) live on the stack; reference types (string, class, array) live on the heap. Use `var` to let the compiler infer the type. Nullable value types are written as `int?`."),
                    new SeedPost("Operators and Expressions",
                        "Arithmetic (+, -, *, /, %), comparison (==, !=, <, >), logical (&&, ||, !), and the null-coalescing operator (??) are the building blocks of every expression. Prefer `switch` expressions over chains of `if/else` for multi-way branching."),
                    new SeedPost("Type Conversion and Casting",
                        "Implicit conversion happens automatically when no data is lost (int → long). Explicit casting with `(Type)` is required when data may be lost. Use `int.Parse()` or `Convert.ToInt32()` for strings. The `is` and `as` operators enable safe runtime type checks."),
                ]),
                new("Control Flow and Methods",
                [
                    new SeedVideo("Branching and Loops",
                        "Live-coding session: FizzBuzz and a grade calculator illustrate if/else, switch expressions, for, foreach, and while loops."),
                    new SeedPost("If/Else and Switch Expressions",
                        "Use `if`/`else` for simple binary decisions. For multiple cases, prefer the C# 8+ `switch` expression — it is exhaustive, returns a value, and eliminates fall-through bugs. Pattern matching with `is`, `when`, and type patterns makes switch even more powerful."),
                    new SeedPost("Loops: for, foreach, while",
                        "Use `foreach` when iterating collections — it is the most readable. Use `for` when you need the index. `while` is best when the iteration count is unknown. `break` exits the loop early; `continue` skips to the next iteration."),
                    new SeedVideo("Writing Reusable Methods",
                        "We extract duplicated logic into methods, explore optional and named parameters, and introduce local functions and expression-bodied members."),
                ]),
                new("Object-Oriented Programming",
                [
                    new SeedVideo("OOP Fundamentals in C#",
                        "An interactive session covering the four OOP pillars — encapsulation, inheritance, polymorphism, and abstraction — with runnable C# examples."),
                    new SeedPost("Classes and Objects",
                        "A class is a blueprint; an object is an instance. Classes encapsulate state (fields/properties) and behaviour (methods). Use `private` fields with `public` properties to enforce invariants. Record types (C# 9+) provide value-semantics classes with built-in equality and immutability."),
                    new SeedPost("Inheritance and Polymorphism",
                        "Inheritance (`class Dog : Animal`) lets a derived class reuse and extend a base class. Mark methods `virtual` to allow overriding. Use `abstract` for mandatory overrides. Prefer composition over inheritance when you have a 'has-a' relationship. Interfaces define contracts without implementation."),
                ]),
                new("C# Assessments",
                [
                    new SeedTest("Quick Check: Language Basics",
                        [
                            SC("Which keyword lets the compiler infer a variable's type?",
                                "var", "let", "auto", "type"),
                            SC("Which value type stores 32-bit whole numbers in C#?",
                                "int", "float", "decimal", "string"),
                            SC("Which keyword defines a contract with no implementation?",
                                "interface", "abstract", "virtual", "base"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: Types and OOP",
                        [
                            MC("Which are value types in C#?",
                                ["int", "bool", "struct"],
                                ["string", "class", "object"]),
                            MC("Which access modifiers are valid in C#?",
                                ["public", "private", "protected", "internal"],
                                ["final", "friend"]),
                            MC("Which keywords participate in exception handling?",
                                ["try", "catch", "finally", "throw"],
                                ["handle", "error", "rescue"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("What does CLR stand for?",
                                "Common Language Runtime",
                                "Compiled Library Registry",
                                "Core Load Runtime",
                                "Common Library Resolver"),
                            MC("Which LINQ methods use deferred execution?",
                                ["Where", "Select", "OrderBy"],
                                ["ToList", "Count", "First"]),
                            TI("What dotnet CLI command runs a .NET project?", "dotnet run"),
                        ],
                        Description: "Covers CLR, LINQ execution model, and .NET tooling.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What keyword exits a loop immediately?", "break"),
                            TI("What keyword skips to the next loop iteration?", "continue"),
                            TI("What keyword instantiates a new object?", "new"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("What does the `sealed` modifier prevent on a class?",
                                "Inheritance", "Instantiation", "Overloading", "Overriding"),
                            SC("Which C# feature removes the need for an explicit class wrapper around Main?",
                                "Top-level statements", "Primary constructors", "File-scoped namespaces", "Record types"),
                            MC("Which are C# 12 language features?",
                                ["Primary constructors", "Collection expressions", "Default lambda parameters"],
                                ["Arrow functions", "Null types", "Goto expressions"]),
                            TI("What keyword enables asynchronous method execution in C#?", "async"),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 2. Design Patterns in C#
        // ═══════════════════════════════════════════════════════════════
        new("programming",
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
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 3. React 19 with TypeScript
        // ═══════════════════════════════════════════════════════════════
        new("web-development",
            "React 19 with TypeScript",
            "Build modern, type-safe web UIs with React 19, hooks, TanStack Query, and Zustand. Covers component patterns, state management, and API integration.",
            19.99m,
            ["react", "typescript", "frontend", "tanstack-query"],
            [
                new("React & TypeScript Setup",
                [
                    new SeedVideo("Scaffolding a React + TypeScript Project",
                        "We scaffold a Vite project with the react-ts template, configure path aliases, add Tailwind CSS, and wire up ESLint and Prettier."),
                    new SeedPost("Why React with TypeScript?",
                        "TypeScript catches entire classes of runtime errors at compile time: mistyped prop names, missing required props, and incorrect return types. React 19's improved compiler makes re-render optimisation largely automatic, so type safety is the biggest remaining reason to be explicit — and TypeScript handles that."),
                ]),
                new("Components and Hooks",
                [
                    new SeedVideo("Functional Components and JSX",
                        "We build a Card component from scratch, type its props interface, and explore how JSX is sugar over React.createElement — and why that matters for understanding rendering."),
                    new SeedPost("useState and useEffect",
                        "`useState` manages local component state. `useEffect` synchronises with external systems. The dependency array controls re-execution. The golden rule: never put server data in `useState` — use TanStack Query instead to avoid stale data and double-fetching."),
                    new SeedPost("Props and Component Composition",
                        "Props flow down; events flow up. Prefer composition (the `children` prop, named slot props) over deep prop drilling. Context is for slow-changing cross-cutting values (theme, locale, auth user) — not for frequently updating server data."),
                    new SeedPost("Custom Hooks",
                        "Extract stateful logic into custom hooks (`useWindowSize`, `useDebounce`). A custom hook is any function whose name starts with `use` and that calls at least one built-in hook. Co-locate hook files with the feature they serve, not in a global `/hooks` folder."),
                ]),
                new("State and Data Fetching",
                [
                    new SeedVideo("Server State with TanStack Query",
                        "A live session fetching a paginated list with `useQuery`, handling loading and error states, and invalidating the cache after a mutation — all without a single `useState` for server data."),
                    new SeedPost("Fetching Data with useQuery",
                        "`useQuery({ queryKey: ['courses'], queryFn: fetchCourses })` fetches, caches, and keeps server state fresh. The `queryKey` is a stable cache identifier — include all variables the query depends on. `isLoading`, `isError`, and `data` give you everything needed to render the three UI states."),
                    new SeedPost("Mutations with useMutation",
                        "`useMutation` sends writes (POST, PUT, DELETE) and hooks into loading/error/success lifecycle. Call `queryClient.invalidateQueries` in `onSuccess` to trigger a refetch. Optimistic updates via `onMutate` / `onError` rollback provide instant UI feedback."),
                    new SeedVideo("Client State with Zustand",
                        "We build a theme store and an auth store using Zustand's `create<T>()` API, use selectors to minimise re-renders, and see how Zustand avoids Redux boilerplate while remaining fully typed."),
                ]),
                new("Advanced React Patterns",
                [
                    new SeedVideo("Performance Optimisation",
                        "We profile a slow list render in React DevTools Profiler, apply `React.memo`, `useMemo`, and `useCallback` surgically, then let the React 19 Compiler handle the rest automatically."),
                    new SeedPost("useMemo and useCallback",
                        "`useMemo` memoises an expensive computed value; `useCallback` memoises a function reference. Both are only useful when the memoised value is passed to a `React.memo`-wrapped child or used as a `useEffect` dependency. Profile first — premature memoisation adds cost without benefit."),
                    new SeedPost("React 19 New Features",
                        "React 19 ships the React Compiler (auto-memoisation), the `use()` hook for suspense-based data reading, Actions for async form handling, and improved server component support. In many codebases the compiler eliminates the need for manual `useMemo`/`useCallback` entirely."),
                ]),
                new("React Assessments",
                [
                    new SeedTest("Quick Check: Hooks Basics",
                        [
                            SC("Which hook manages local component state in React?",
                                "useState", "useRef", "useMemo", "useReducer"),
                            SC("What file extension is standard for TypeScript React components?",
                                ".tsx", ".ts", ".jsx", ".js"),
                            SC("What is the purpose of the dependency array in useEffect?",
                                "Controls when the effect re-runs",
                                "Declares imported modules",
                                "Lists component props",
                                "Defines state variables"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: State Management",
                        [
                            MC("Which hooks are intended for synchronising side effects?",
                                ["useEffect", "useLayoutEffect"],
                                ["useState", "useMemo", "useCallback"]),
                            MC("Which are valid React hook rules?",
                                ["Only call at the top level of a function component",
                                 "Only call inside React function components or custom hooks"],
                                ["Can be called inside loops",
                                 "Can be called inside conditionals",
                                 "Can be called in class components"]),
                            MC("Which libraries manage client-side state well in React apps?",
                                ["Zustand", "Jotai", "Recoil"],
                                ["TanStack Query", "React Router", "Axios"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("What is the TanStack Query `queryKey` used for?",
                                "Identifying and sharing the cached result",
                                "Specifying the API base URL",
                                "Declaring query dependencies in package.json",
                                "Setting the HTTP request timeout"),
                            MC("Which strategies prevent unnecessary re-renders in React?",
                                ["React.memo on pure components",
                                 "Stable query keys in TanStack Query",
                                 "Selecting minimal slices from Zustand stores"],
                                ["Storing all state in a single useState",
                                 "Using class components"]),
                            TI("What Zustand function creates a typed store?", "create"),
                        ],
                        Description: "Covers TanStack Query caching, render optimisation, and Zustand store creation.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What hook reads a context value inside a function component?", "useContext"),
                            TI("What prop name passes child elements into a parent component?", "children"),
                            TI("What React function wraps a component to skip re-renders when props are unchanged?",
                                "memo"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("What does the `key` prop help React do during list reconciliation?",
                                "Identify list items to minimise DOM mutations",
                                "Apply CSS class names",
                                "Register event listeners",
                                "Define component display names"),
                            SC("Which TanStack Query hook is used for write operations?",
                                "useMutation", "useQuery", "useInfiniteQuery", "useQueryClient"),
                            MC("Which are anti-patterns in React development?",
                                ["Storing server data in useState",
                                 "Deep prop drilling across many levels",
                                 "Creating new object references in render without useMemo"],
                                ["Using TanStack Query for server state",
                                 "Co-locating components with their feature"]),
                            TI("What React 19 feature auto-memoises components without manual useMemo?",
                                "compiler", ignoreCase: true),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 4. Python for Data Analysis
        // ═══════════════════════════════════════════════════════════════
        new("data-science",
            "Python for Data Analysis",
            "Master pandas, NumPy, and matplotlib for real-world data analysis tasks. Assumes basic Python knowledge.",
            24.99m,
            ["python", "pandas", "numpy", "data-science"],
            [
                new("Getting Started with Pandas",
                [
                    new SeedVideo("Your First Data Analysis",
                        "We load a real CSV dataset, inspect it with .head() and .info(), fix data types, and produce our first chart — all in under 30 minutes."),
                    new SeedPost("Python for Data Science Overview",
                        "The Python data science stack: NumPy (array math), pandas (tabular data), matplotlib/seaborn (visualisation), and scikit-learn (ML). pandas is the glue layer — it reads CSVs, SQL tables, and JSON, and hands clean arrays to NumPy and scikit-learn."),
                ]),
                new("Data Wrangling with Pandas",
                [
                    new SeedVideo("DataFrames and Series",
                        "A hands-on introduction to creating DataFrames from dicts, CSVs, and SQL queries. We explore the index, column access, and the difference between a view and a copy."),
                    new SeedPost("Creating and Loading DataFrames",
                        "Create from a dict: `pd.DataFrame({'col': [1,2,3]})`. Load CSV: `pd.read_csv('data.csv', parse_dates=['date'])`. Inspect with `.head()`, `.info()`, `.describe()`. Set `.dtypes` explicitly to save memory — use `category` for low-cardinality strings."),
                    new SeedPost("Filtering and Selecting Data",
                        "Boolean indexing: `df[df['age'] > 30]`. Chain conditions with `&` and `|`. `.loc[rows, cols]` selects by label; `.iloc[rows, cols]` by integer position. Avoid chained assignment (`df['a']['b'] = x`) — use `.loc` to prevent SettingWithCopyWarning."),
                    new SeedPost("Handling Missing Values",
                        "Detect nulls with `.isnull().sum()`. Drop rows/columns with `.dropna()`. Fill with `.fillna(value)` or forward-fill with `.ffill()`. For numeric columns, fill with median (robust to outliers) rather than mean. Document imputation decisions — they affect downstream analysis."),
                ]),
                new("Analysis and Grouping",
                [
                    new SeedVideo("Groupby and Aggregation",
                        "We analyse a sales dataset: group by region and product, compute revenue per group, and build a pivot table — live, with commentary on common pitfalls."),
                    new SeedPost("GroupBy and Pivot Tables",
                        "`.groupby('col').mean()` splits, applies, and combines in one call. `.agg({'sales': 'sum', 'returns': 'count'})` applies different functions per column. Pivot tables: `df.pivot_table(values='sales', index='region', columns='product', aggfunc='sum')`. Use `.reset_index()` after groupby to restore a flat DataFrame."),
                    new SeedPost("Merging and Joining DataFrames",
                        "`pd.merge(left, right, on='key', how='inner')` SQL-style join. Options: 'inner' (default), 'left', 'right', 'outer'. `pd.concat([df1, df2])` stacks vertically. Always check for key duplicates before merging — they silently produce a cartesian product."),
                    new SeedVideo("Time Series Analysis",
                        "We parse dates, resample daily data to monthly totals, compute rolling 7-day averages, and plot a time series with matplotlib — a typical workflow for sales or event data."),
                ]),
                new("Visualisation and EDA",
                [
                    new SeedVideo("Matplotlib and Seaborn",
                        "Side-by-side comparison: matplotlib for full control, seaborn for statistical plots with sensible defaults. We build a correlation heatmap and a distribution chart."),
                    new SeedPost("Creating Charts with Matplotlib",
                        "`fig, ax = plt.subplots()` is the object-oriented API — prefer it over `plt.plot()` for multi-panel figures. Always label axes: `ax.set_xlabel()`, `ax.set_ylabel()`, `ax.set_title()`. Save with `fig.savefig('chart.png', dpi=150, bbox_inches='tight')`."),
                    new SeedPost("Exploratory Data Analysis Workflow",
                        "EDA steps: (1) inspect shape/dtypes, (2) check nulls and duplicates, (3) distribution histograms for numeric columns, (4) value counts for categorical columns, (5) correlation heatmap, (6) scatter plots for target vs features, (7) outlier detection via IQR or z-score."),
                ]),
                new("Python & Pandas Assessments",
                [
                    new SeedTest("Quick Check: Pandas Basics",
                        [
                            SC("Which method shows the first N rows of a DataFrame?",
                                ".head()", ".first()", ".top()", ".show()"),
                            SC("Which pandas object represents a single column of data?",
                                "Series", "DataFrame", "Index", "Column"),
                            SC("Which method displays column names, dtypes, and non-null counts?",
                                ".info()", ".describe()", ".shape", ".dtypes"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: Selection and Aggregation",
                        [
                            MC("Which are valid ways to select a column from a DataFrame?",
                                ["df['col']", "df.col", "df.loc[:, 'col']"],
                                ["df.select('col')", "df.get_col('col')"]),
                            MC("Which aggregation methods work with .groupby()?",
                                [".mean()", ".sum()", ".count()", ".std()"],
                                [".render()", ".display()"]),
                            MC("Which are standard EDA steps?",
                                ["Check for nulls and duplicates", "Plot distributions", "Compute correlation matrix"],
                                ["Fit a machine learning model", "Deploy to production"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("What does `.loc[]` select rows and columns by?",
                                "Label", "Integer position", "Column data type", "Row count"),
                            MC("Which methods handle missing values in pandas?",
                                [".fillna()", ".dropna()", ".ffill()", ".bfill()"],
                                [".fillmissing()", ".removena()", ".cleannulls()"]),
                            TI("What pandas function merges two DataFrames on a shared key column?", "merge"),
                        ],
                        Description: "Covers .loc vs .iloc, null handling, and DataFrame merging.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What alias is used for NumPy by convention?", "np"),
                            TI("What pandas method groups rows with identical column values?", "groupby"),
                            TI("What alias is used for matplotlib.pyplot by convention?", "plt"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("Which parameter of pd.merge controls the join type (inner, left, right, outer)?",
                                "how", "on", "join", "type"),
                            SC("Which method provides descriptive statistics for all numeric columns at once?",
                                ".describe()", ".info()", ".stat()", ".summary()"),
                            MC("Which are valid plot types in matplotlib?",
                                ["plt.plot()", "plt.bar()", "plt.scatter()", "plt.hist()"],
                                ["plt.chart()", "plt.draw_line()", "plt.column()"]),
                            TI("What alias is used for pandas by convention?", "pd"),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 5. UI/UX Design Principles
        // ═══════════════════════════════════════════════════════════════
        new("design",
            "UI/UX Design Principles",
            "Learn the fundamentals of user interface and user experience design. Covers visual design, information architecture, and prototyping in Figma.",
            14.99m,
            ["design", "ux", "ui", "figma"],
            [
                new("Design Fundamentals",
                [
                    new SeedVideo("Introduction to UI/UX Design",
                        "A tour of the UX design process: discovery → definition → ideation → prototyping → testing → launch. We look at real before/after redesigns to see the impact of user-centred design."),
                    new SeedPost("The Difference Between UI and UX",
                        "UX (User Experience) is the overall feeling of using a product — is it useful, usable, and delightful? UI (User Interface) is the visual layer — colours, typography, spacing, and interactions. Good UX without good UI is a rough road on a well-planned route. Both disciplines are essential."),
                ]),
                new("Visual Design",
                [
                    new SeedVideo("Colour Theory and Typography",
                        "We build a complete colour palette from a single brand colour using HSL, check contrast ratios with a Figma plugin, and pair a geometric sans-serif with a humanist body typeface."),
                    new SeedPost("Colour Theory for UI",
                        "Use a primary, secondary, and semantic palette (success/warning/error). Ensure 4.5:1 contrast ratio for body text (WCAG AA) and 3:1 for large text. HSL makes it easy to create tints and shades — adjust L while keeping H consistent. Never rely on colour alone to convey meaning; always add an icon or label."),
                    new SeedPost("Typography and Type Hierarchy",
                        "Limit typefaces to two: one for headings, one for body. Use a typographic scale (1.25× ratio). Establish hierarchy through size, weight, and spacing — not colour alone. Aim for 60–80 characters per line. Line height: 1.4–1.6× for body text."),
                    new SeedPost("Layout and Spacing Systems",
                        "The 8px grid system aligns elements to multiples of 8 (8, 16, 24, 32, 48px), creating visual rhythm and making hand-off to developers predictable. Use Figma's Auto Layout to make components responsive. Whitespace is not empty space — it gives elements room to breathe and guides attention."),
                ]),
                new("UX Process",
                [
                    new SeedVideo("User Research Methods",
                        "A practical guide to choosing the right research method: when to run interviews vs surveys vs usability tests, how many participants you need, and how to synthesise findings into actionable insights."),
                    new SeedPost("Conducting User Interviews",
                        "Prepare 5–8 open-ended questions. Start with warm-up questions about the participant's role and context. Avoid leading questions. Use the 5 Whys technique to dig beneath surface answers. Synthesise findings with affinity mapping — cluster quotes and observations into themes."),
                    new SeedPost("Creating Wireframes in Figma",
                        "Start with low-fidelity wireframes (grey boxes, placeholder text) to explore structure without getting distracted by visual details. Use Figma Frames, not groups, for responsive layouts. Add Auto Layout to simulate how the design adapts to content changes."),
                    new SeedVideo("Prototyping and Usability Testing",
                        "We turn static wireframes into a clickable prototype in Figma using Prototype mode, then run a 5-participant usability test, record findings, and prioritise fixes."),
                ]),
                new("Design Systems",
                [
                    new SeedVideo("Building a Design System",
                        "We structure a Figma file into a design system with a type scale, colour tokens, and a component library — then show how to publish it and consume it in a product file."),
                    new SeedPost("Components and Variants in Figma",
                        "A Component in Figma is a reusable element. Variants group related states (default, hover, active, disabled) into a single component set, accessible via the properties panel. Use named props (State, Size, Icon) not numbered suffixes. Publish your library so the whole team uses the same source of truth."),
                    new SeedPost("Accessibility and WCAG Guidelines",
                        "WCAG 2.1 defines three levels: A (minimum), AA (standard requirement), AAA (enhanced). At AA: 4.5:1 contrast for body text, 3:1 for large text and UI components, all interactive elements reachable by keyboard, images have descriptive alt text. Check contrast during design — not after development."),
                ]),
                new("Design Assessments",
                [
                    new SeedTest("Quick Check: Design Basics",
                        [
                            SC("What contrast ratio does WCAG AA require for body text?",
                                "4.5:1", "3:1", "7:1", "2.1:1"),
                            SC("What tool is most widely used for UI design and prototyping today?",
                                "Figma", "Adobe XD", "Sketch", "InVision"),
                            SC("What research method reveals WHY users behave a certain way?",
                                "User interviews", "A/B testing", "Analytics dashboards", "Quantitative surveys"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: Visual Design",
                        [
                            MC("Which are principles used to establish visual hierarchy?",
                                ["Size", "Weight", "Colour", "Spacing"],
                                ["Random placement", "Equal sizing throughout"]),
                            MC("Which are qualitative user research methods?",
                                ["User interviews", "Contextual inquiry", "Diary studies"],
                                ["A/B tests", "Analytics", "Conversion rate tracking"]),
                            MC("Which meet WCAG AA requirements?",
                                ["4.5:1 contrast for body text", "3:1 contrast for large text",
                                 "Keyboard-accessible interactive elements"],
                                ["Colour alone used to convey state", "Text embedded in images"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("Nielsen's research suggests how many participants for a usability test to surface 85% of issues?",
                                "5", "10", "3", "20"),
                            MC("Which are widely used spacing systems in UI design?",
                                ["4px grid", "8px grid", "Fibonacci-based scale"],
                                ["No fixed spacing system", "100px grid"]),
                            TI("What Figma feature groups multiple states of a component (e.g. default, hover, disabled) into one?",
                                "Variants", ignoreCase: true),
                        ],
                        Description: "Covers usability testing numbers, spacing systems, and Figma Variants.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What CSS colour model uses Hue, Saturation, and Lightness?", "HSL"),
                            TI("What term describes designing products usable by people with disabilities?",
                                "accessibility"),
                            TI("What typeface style has decorative strokes at the ends of letterforms?", "Serif"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("Which principle removes unnecessary elements to reduce cognitive load?",
                                "Minimalism", "Skeuomorphism", "Flat design", "Brutalism"),
                            SC("What contrast ratio is the WCAG AA threshold for large text and UI components?",
                                "3:1", "4.5:1", "7:1", "2:1"),
                            MC("Which are UX design deliverables?",
                                ["Wireframes", "User flow diagrams", "Personas", "Usability test reports"],
                                ["JavaScript source code", "Database schemas", "Server logs"]),
                            TI("What Figma feature makes components adapt to their content size automatically?",
                                "Auto Layout", ignoreCase: true),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 6. SQL & Database Design  (NEW)
        // ═══════════════════════════════════════════════════════════════
        new("data-science",
            "SQL & Database Design",
            "Master SQL querying and relational database design. Covers SELECT, JOINs, aggregations, indexing, transactions, and normalisation.",
            19.99m,
            ["sql", "database", "postgres", "data-science"],
            [
                new("Introduction to Databases",
                [
                    new SeedVideo("What Is a Relational Database?",
                        "We explore the table/row/column model, the role of primary and foreign keys, and why relational databases remain the default choice for structured transactional data."),
                    new SeedPost("SQL vs NoSQL: When to Use What",
                        "SQL databases (PostgreSQL, MySQL) excel at structured data with complex relationships and ACID guarantees. NoSQL (MongoDB, Redis, Cassandra) trade ACID for scale, schema flexibility, or specialised access patterns. Most applications need both: a relational DB for core business data and a document store or cache for specific workloads."),
                ]),
                new("SQL Fundamentals",
                [
                    new SeedVideo("SELECT, WHERE, and ORDER BY",
                        "We write 15 progressively complex queries against a sample e-commerce database, covering column selection, filtering, sorting, and LIMIT/OFFSET pagination."),
                    new SeedPost("Basic SQL Queries",
                        "The anatomy of a SELECT: `SELECT col1, col2 FROM table WHERE condition ORDER BY col ASC LIMIT 10`. Always specify columns explicitly — `SELECT *` is fine for exploration but breaks APIs when schema changes. Use table aliases (`FROM orders o`) to shorten long queries."),
                    new SeedPost("Filtering Data with WHERE",
                        "Comparison: `=`, `!=`, `<`, `>`, `BETWEEN`. Pattern matching: `LIKE 'J%'` (case-insensitive with `ILIKE` in Postgres). Set membership: `IN (1, 2, 3)`. Null checks: `IS NULL` / `IS NOT NULL` (never `= NULL`). Wrap OR conditions in parentheses to avoid precedence bugs."),
                    new SeedPost("Sorting and Limiting Results",
                        "`ORDER BY` sorts results (ASC by default). Sort by multiple columns: `ORDER BY status ASC, created_at DESC`. `LIMIT n` caps rows; `OFFSET n` skips rows. Warning: OFFSET pagination degrades at large offsets — prefer keyset pagination (`WHERE id > last_seen_id LIMIT 20`) for large tables."),
                ]),
                new("JOINs and Aggregations",
                [
                    new SeedVideo("JOIN Types Explained",
                        "A visual walkthrough of INNER, LEFT, RIGHT, and FULL OUTER JOIN with Venn diagrams, then live queries against a normalised orders-customers-products schema."),
                    new SeedPost("INNER JOIN and LEFT JOIN",
                        "INNER JOIN returns rows with a match in both tables. LEFT JOIN returns all rows from the left table plus matched rows from the right — nulls for non-matches. Use LEFT JOIN to find unmatched rows: `WHERE right_table.id IS NULL`. Always add indexes on join columns."),
                    new SeedPost("Aggregation: SUM, COUNT, AVG, MIN, MAX",
                        "`COUNT(*)` counts all rows; `COUNT(col)` excludes nulls. Use with `GROUP BY` to aggregate per category. `HAVING` filters after grouping (`HAVING SUM(amount) > 1000`). Always use `GROUP BY` when mixing aggregate and non-aggregate columns in SELECT."),
                    new SeedVideo("Subqueries and CTEs",
                        "We rewrite a nested subquery as a CTE (`WITH orders_summary AS (...)`) and see how CTEs improve readability, enable recursion, and can be referenced multiple times in the outer query."),
                ]),
                new("Database Design and Performance",
                [
                    new SeedVideo("Normalisation and Schema Design",
                        "We normalise a denormalised spreadsheet through 1NF, 2NF, and 3NF, discuss when denormalisation is intentional, and design a schema for a small e-commerce system."),
                    new SeedPost("Primary Keys and Foreign Keys",
                        "Primary key: uniquely identifies each row. Use `SERIAL` or `UUID` — not business data. Foreign key: enforces referential integrity (`REFERENCES orders(id) ON DELETE CASCADE`). Never expose auto-increment integer PKs in public APIs — they leak record counts; use UUIDs or opaque IDs."),
                    new SeedPost("Indexes and Query Performance",
                        "A B-tree index speeds up equality and range lookups. Create indexes on FK columns, frequently filtered columns, and ORDER BY columns. `EXPLAIN ANALYZE` shows the query plan and actual row counts. A Seq Scan on a large table with a WHERE clause is a warning sign."),
                ]),
                new("SQL Assessments",
                [
                    new SeedTest("Quick Check: SQL Basics",
                        [
                            SC("Which SQL clause filters rows before aggregation?",
                                "WHERE", "HAVING", "FILTER", "LIMIT"),
                            SC("Which keyword removes duplicate rows from a SELECT result?",
                                "DISTINCT", "UNIQUE", "DEDUPE", "ONLY"),
                            SC("What does NULL represent in a relational database?",
                                "A missing or unknown value", "Zero", "An empty string", "False"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: JOINs and Aggregation",
                        [
                            MC("Which JOIN types can return rows with NULLs for non-matching columns?",
                                ["LEFT JOIN", "RIGHT JOIN", "FULL OUTER JOIN"],
                                ["INNER JOIN", "CROSS JOIN"]),
                            MC("Which are standard SQL aggregate functions?",
                                ["COUNT()", "SUM()", "AVG()", "MIN()", "MAX()"],
                                ["FIRST()", "ARRAY()", "GROUP()"]),
                            MC("Which strategies improve SQL query performance?",
                                ["Adding indexes on frequently filtered columns",
                                 "Using EXPLAIN ANALYZE to understand query plans",
                                 "Selecting only needed columns"],
                                ["Using SELECT * on every query", "Adding more columns to all indexes"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("What does the ACID acronym stand for?",
                                "Atomicity, Consistency, Isolation, Durability",
                                "Atomicity, Cohesion, Index, Distribution",
                                "Abstraction, Consistency, Isolation, Delivery",
                                "Association, Cohesion, Integrity, Durability"),
                            MC("Which are recognised Database Normal Forms?",
                                ["1NF", "2NF", "3NF", "BCNF"],
                                ["4.5NF", "ZeroNF", "HyperNF"]),
                            TI("Which SQL clause filters groups produced by GROUP BY?", "HAVING"),
                        ],
                        Description: "Covers ACID properties, normalisation forms, and HAVING vs WHERE.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What SQL clause groups rows with identical column values?", "GROUP BY"),
                            TI("What SQL command permanently saves an open transaction?", "COMMIT"),
                            TI("What SQL keyword creates a reusable virtual table from a query?", "VIEW"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("What named SQL construct allows a query to reference itself recursively?",
                                "CTE", "Subquery", "View", "Stored Procedure"),
                            SC("What index type is created automatically for a PRIMARY KEY in PostgreSQL?",
                                "B-tree", "Hash", "GIN", "BRIN"),
                            MC("Which are valid SQL transaction isolation levels?",
                                ["READ COMMITTED", "REPEATABLE READ", "SERIALIZABLE", "READ UNCOMMITTED"],
                                ["READ ALWAYS", "SNAPSHOT ONLY", "LOCKED READ"]),
                            TI("What PostgreSQL command shows a query's execution plan?", "EXPLAIN"),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 7. Node.js & REST APIs  (NEW)
        // ═══════════════════════════════════════════════════════════════
        new("web-development",
            "Node.js & REST APIs",
            "Build production-ready REST APIs with Node.js, Express, JWT authentication, input validation, and MongoDB integration.",
            24.99m,
            ["nodejs", "express", "rest", "backend", "mongodb"],
            [
                new("Node.js Fundamentals",
                [
                    new SeedVideo("How Node.js Works",
                        "We demystify the event loop, the call stack, the callback queue, and the microtask queue — then show why blocking the event loop with synchronous I/O destroys throughput."),
                    new SeedPost("The Event Loop and Async Patterns",
                        "Node.js runs on a single thread with a non-blocking I/O model: when an I/O operation starts, Node registers a callback and continues processing other requests. Callbacks → Promises → async/await is the evolution. Always `await` Promises; never mix callbacks and Promises. Unhandled promise rejections crash the process in Node 18+."),
                ]),
                new("Express and Routing",
                [
                    new SeedVideo("Building HTTP Servers with Express",
                        "We go from a raw `http.createServer()` server to a structured Express app with router modules, a global error handler, and an async wrapper to catch unhandled rejections."),
                    new SeedPost("Routing and Middleware",
                        "Express middleware is a function `(req, res, next) => void`. Call `next()` to pass control; call `next(err)` to jump to the error handler. Register middleware in order — authentication before route handlers. Use `express.Router()` to group related routes; mount with `app.use('/api/v1/courses', courseRouter)`."),
                    new SeedPost("Request and Response Objects",
                        "`req.params` — URL path parameters (`/users/:id`). `req.query` — query string (`?page=2`). `req.body` — parsed JSON body (requires `express.json()` middleware). `res.status(201).json(data)` chains status and body. Never call `res.json()` twice — it throws a 'headers already sent' error."),
                    new SeedPost("Error Handling Middleware",
                        "Express error handlers have 4 parameters: `(err, req, res, next)`. Register them last with `app.use((err, req, res, next) => { ... })`. Wrap async route handlers: `const asyncHandler = fn => (req, res, next) => Promise.resolve(fn(req, res, next)).catch(next)`. Distinguish operational errors (validation, 404) from programmer errors (null dereference)."),
                ]),
                new("REST API Design",
                [
                    new SeedVideo("RESTful API Principles",
                        "We design a courses API following REST constraints: resource naming (plural nouns), HTTP method semantics, status code conventions, HATEOAS links, and versioning strategy."),
                    new SeedPost("HTTP Methods and Status Codes",
                        "GET (read, idempotent), POST (create), PUT (replace, idempotent), PATCH (partial update, idempotent), DELETE (remove, idempotent). Status codes: 200 OK, 201 Created, 204 No Content, 400 Bad Request, 401 Unauthorised, 403 Forbidden, 404 Not Found, 422 Unprocessable Entity, 500 Internal Server Error. Never return 200 for an error."),
                    new SeedPost("Versioning and Pagination",
                        "URL versioning (`/api/v1/`) is the most explicit approach. Cursor-based pagination (`after=<cursor>&limit=20`) scales better than offset pagination for large datasets. Return pagination metadata: `{ data: [...], nextCursor, hasMore }`. Validate and cap `limit` parameters server-side to prevent abuse."),
                    new SeedVideo("Authentication with JWT",
                        "We implement a full JWT auth flow: register → login → issue access + refresh tokens → protect routes with a `verifyToken` middleware → rotate refresh tokens on use."),
                ]),
                new("Database Integration",
                [
                    new SeedVideo("Connecting to MongoDB with Mongoose",
                        "We define Mongoose schemas with validators, create models, and write a CRUD service layer — then discuss when to use lean queries and when to avoid Mongoose in favour of the native driver."),
                    new SeedPost("CRUD Operations with Mongoose",
                        "`Model.find(filter)`, `.findById(id)`, `.findOneAndUpdate(filter, update, { new: true })`, `.findByIdAndDelete(id)`. Use `.lean()` on read queries to get plain JS objects — faster and smaller than Mongoose documents. Always handle `null` returns from `.findById()` — they mean the document does not exist."),
                    new SeedPost("Input Validation and Sanitisation",
                        "Never trust client input. Validate on the server even if you validate on the client. Use Joi (`joi.object({ email: joi.string().email().required() })`) or express-validator. Sanitise to prevent XSS: strip or encode HTML in text fields. Prevent NoSQL injection by whitelisting allowed query operators — never pass `req.body` directly as a MongoDB filter."),
                ]),
                new("Node.js Assessments",
                [
                    new SeedTest("Quick Check: REST Basics",
                        [
                            SC("What HTTP status code indicates a new resource was successfully created?",
                                "201", "200", "204", "301"),
                            SC("What does REST stand for?",
                                "Representational State Transfer",
                                "Remote Service Template",
                                "Request Exchange Structure",
                                "Resource State Transport"),
                            SC("What Node.js object provides access to environment variables?",
                                "process.env", "env.config", "config.env", "system.env"),
                        ],
                        PassingThreshold: 60,
                        AttemptLimit: null),

                    new SeedTest("Concept Quiz: HTTP and Middleware",
                        [
                            MC("Which HTTP methods are considered idempotent?",
                                ["GET", "PUT", "DELETE", "HEAD"],
                                ["POST", "PATCH"]),
                            MC("Which are valid use cases for Express middleware?",
                                ["Authentication and authorisation", "Request logging", "Body parsing", "Rate limiting"],
                                ["Compiling TypeScript at runtime", "Building Docker images"]),
                            MC("Which are the three structural parts of a JWT token?",
                                ["Header", "Payload", "Signature"],
                                ["Encryption key", "Expiry date field", "Issuer URL"]),
                        ],
                        PassingThreshold: 70,
                        AttemptLimit: 3,
                        CooldownMinutes: 2),

                    new SeedTest("Comprehensive Exam",
                        [
                            SC("In Express, how many parameters does an error-handling middleware function have?",
                                "4", "2", "3", "1"),
                            MC("Which are REST API best practices?",
                                ["Use plural nouns for resource URLs",
                                 "Return appropriate HTTP status codes",
                                 "Version the API in the URL"],
                                ["Store session state on the server per user",
                                 "Use GET for write operations",
                                 "Return 200 for all responses"]),
                            TI("What Node.js utility converts a callback-based function into a Promise?", "promisify"),
                        ],
                        Description: "Covers Express error handling, REST best practices, and Node.js util.promisify.",
                        PassingThreshold: 80,
                        AttemptLimit: 2,
                        CooldownMinutes: 3),

                    new SeedTest("Terminology Test",
                        [
                            TI("What Express argument calls the next middleware in the chain?", "next"),
                            TI("What HTTP method partially updates an existing resource?", "PATCH"),
                            TI("What HTTP status code means the client is not authenticated?", "401"),
                        ],
                        PassingThreshold: 50,
                        AttemptLimit: 1),

                    new SeedTest("Mastery Challenge",
                        [
                            SC("What design pattern separates HTTP handling from business logic in Node.js?",
                                "Service layer", "Repository", "MVC", "Singleton"),
                            SC("Which HTTP header carries a Bearer JWT in an API request?",
                                "Authorization", "Authentication", "Token", "X-Access-Token"),
                            MC("Which are security risks REST APIs must protect against?",
                                ["SQL / NoSQL injection",
                                 "Broken object-level authorisation",
                                 "Missing rate limiting",
                                 "Cross-Site Request Forgery (CSRF)"],
                                ["Using HTTPS", "Returning JSON", "Documenting endpoints"]),
                            TI("What npm file locks every dependency to an exact version for reproducible installs?",
                                "package-lock.json", ignoreCase: true),
                        ],
                        PassingThreshold: 100,
                        AttemptLimit: 5,
                        CooldownMinutes: 1),
                ]),
            ]),

        // ═══════════════════════════════════════════════════════════════
        // 8. Digital Marketing Fundamentals  (NEW)
        // ═══════════════════════════════════════════════════════════════
        new("marketing",
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
            ]),
    ];

    // ── IHostedService ────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Hard-stop in any non-Development environment — this service must never run in staging/production.
        if (!environment.IsDevelopment())
            return;

        var enabled = configuration["SeedDevData:Enabled"];

        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
            return;

        var email = configuration["SeedDevData:InstructorEmail"] ?? "instructor@learnix.dev";
        var password = configuration["SeedDevData:InstructorPassword"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Dev seeder: SeedDevData:InstructorPassword is not set — skipping course seeding.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

        var instructor = await EnsureInstructorAsync(userManager, email, password);
        if (instructor is null)
            return;

        var alreadySeeded = await context.Courses
            .AnyAsync(c => c.InstructorId == instructor.Id, cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation(
                "Dev seeder: courses already exist for {Email} — skipping.", email);
            return;
        }

        // Upload placeholder blobs once for the whole seeding run.
        // VideoLessons reference placeholderVideoPath; the LessonVideoAttachedDomainEvent
        // outbox handler will call MarkConfirmedAsync again on that path — idempotent.
        var coverPath = $"{blobOptions.Value.CourseCoverContainer}/seed-placeholder.png";
        var videoPath = $"{blobOptions.Value.LessonVideoContainer}/seed-placeholder.png";

        try
        {
            await blobStorage.UploadAsync(
                coverPath, new MemoryStream(PlaceholderPng), "image/png", cancellationToken);
            await blobStorage.MarkConfirmedAsync(coverPath, cancellationToken);

            await blobStorage.UploadAsync(
                videoPath, new MemoryStream(PlaceholderPng), "image/png", cancellationToken);
            await blobStorage.MarkConfirmedAsync(videoPath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Dev seeder: could not upload placeholder blobs — is blob storage running?");
            return;
        }

        var categoryIdBySlug = await context.Categories
            .Where(c => c.IsSystem)
            .ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);

        var seededCount = 0;
        foreach (var definition in SeedCourses)
        {
            if (!categoryIdBySlug.TryGetValue(definition.CategorySlug, out var categoryId))
            {
                logger.LogWarning(
                    "Dev seeder: category '{Slug}' not found — skipping '{Title}'.",
                    definition.CategorySlug, definition.Title);
                continue;
            }

            await SeedSingleCourseAsync(
                context, instructor.Id, categoryId, definition,
                coverPath, videoPath, cancellationToken);
            seededCount++;
        }

        logger.LogInformation(
            "Dev seeder: seeded {Count} courses for instructor {Email}.", seededCount, email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<User?> EnsureInstructorAsync(
        UserManager<User> userManager,
        string email,
        string password)
    {
        var instructor = await userManager.FindByEmailAsync(email);

        if (instructor is null)
        {
            instructor = new User(email, "Dev", "Instructor") { EmailConfirmed = true };
            var result = await userManager.CreateAsync(instructor, password);

            if (!result.Succeeded)
            {
                logger.LogError(
                    "Dev seeder: failed to create instructor {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return null;
            }

            logger.LogInformation("Dev seeder: created instructor account {Email}.", email);
        }

        if (!await userManager.IsInRoleAsync(instructor, Roles.Instructor))
            await userManager.AddToRoleAsync(instructor, Roles.Instructor);

        return instructor;
    }

    private static async Task SeedSingleCourseAsync(
        ApplicationDbContext context,
        Guid instructorId,
        Guid categoryId,
        SeedCourseDefinition definition,
        string placeholderCoverPath,
        string placeholderVideoPath,
        CancellationToken ct)
    {
        // Step 1 — persist course + sections so their IDs are stable before adding lessons.
        var course = Course.Create(
            instructorId, categoryId,
            definition.Title, definition.Description,
            definition.Price, definition.Tags);

        context.Courses.Add(course);

        var sections = definition.Sections
            .Select(s => (Entity: course.AddSection(s.Title), Def: s))
            .ToList();

        await context.SaveChangesAsync(ct);

        // Step 2 — add lessons via DbContext (Section.AddLesson is internal to the Domain assembly).
        // The unique (SectionId, DisplayOrder) index requires stable order values.
        //
        // Notes on visibility:
        //   PostLesson.Create()  → sets IsHidden = false automatically when content is non-empty.
        //   VideoLesson.Create() → IsHidden stays true (base default); must call SetVisibility(false).
        //   TestLesson.Create()  → IsHidden stays true even after ReplaceQuestions(); must call SetVisibility(false).
        foreach (var (section, sectionDef) in sections)
        {
            for (var order = 0; order < sectionDef.Lessons.Length; order++)
            {
                switch (sectionDef.Lessons[order])
                {
                    case SeedPost post:
                        context.Set<PostLesson>().Add(
                            PostLesson.Create(section.Id, post.Title, order, post.Content));
                        break;

                    case SeedVideo vid:
                        var vl = VideoLesson.Create(
                            section.Id, vid.Title, order, placeholderVideoPath, vid.Description);
                        vl.SetVisibility(false);
                        context.Set<VideoLesson>().Add(vl);
                        break;

                    case SeedTest test:
                        var tl = TestLesson.Create(
                            section.Id, test.Title, order,
                            test.Description, test.AttemptLimit,
                            test.CooldownMinutes, test.PassingThreshold);
                        tl.ReplaceQuestions(test.Questions);
                        tl.SetVisibility(false);
                        context.Set<TestLesson>().Add(tl);
                        break;
                }
            }
        }

        await context.SaveChangesAsync(ct);

        // Step 3 — reload with full navigation so Publish() can validate the in-memory
        // lesson collection. Section._lessons is a backing field populated by EF on load.
        var fullCourse = await context.Courses
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .FirstAsync(c => c.Id == course.Id, ct);

        fullCourse.SetCoverImage(placeholderCoverPath);
        fullCourse.Publish();

        await context.SaveChangesAsync(ct);
    }
}
