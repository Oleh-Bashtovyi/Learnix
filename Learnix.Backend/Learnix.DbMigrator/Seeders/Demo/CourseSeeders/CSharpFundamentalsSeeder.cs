using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class CSharpFundamentalsSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
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
        ],
        "csharp_thumbnail.png");
}


