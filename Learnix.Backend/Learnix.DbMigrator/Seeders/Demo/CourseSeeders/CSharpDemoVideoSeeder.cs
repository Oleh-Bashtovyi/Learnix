using Learnix.Domain.Enums;
using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class CSharpDemoVideoSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        CategorySlug: "programming",
        Title: "C# in Practice: Real-World Concepts",
        Description: """
            Welcome to "C# in Practice: Real-World Concepts"—the definitive guide designed to take you from a basic understanding of C# to writing robust, production-ready applications. 
            
            This course is uniquely crafted to blend foundational theory with advanced, real-world mechanics. You won't just learn syntax; you will learn how the .NET runtime actually manages your code under the hood. 
            
            We start by demystifying the .NET environment and memory management. Have you ever wondered how the Garbage Collector decides what to clean up? Or why the Large Object Heap matters for high-performance applications? We break it all down with clear, easy-to-understand explanations and code snippets.
            
            From there, we dive into the deep end of Asynchronous Programming. `async` and `await` are everywhere, but writing asynchronous code incorrectly can lead to deadlocks, thread-pool starvation, and memory leaks. We will explore the `IAsyncStateMachine`, understand the difference between CPU-bound and I/O-bound operations, and learn why you should never use `async void`.
            
            Throughout the course, you will encounter two major checkpoints designed to test your knowledge through different evaluation modes. Whether you are preparing for a technical interview or just want to solidify your grasp of the .NET ecosystem, this course provides practical, hands-on knowledge.
            """,
        Price: 0m,
        Tags: ["csharp", "dotnet", "oop", "async"],
        ImageName: "csharp_thumbnail.webp",
        Sections:
        [
            new SeedSection("Section 1: Fundamentals", [
                new SeedVideo(
                    "Setting Up the .NET Environment",
                    """
                    # Setting Up the .NET Environment
                    
                    In this video, we will walk you through the very basics of getting your local machine ready for C# development.
                    
                    ### What you will learn:
                    - Installing the `.NET SDK`
                    - Configuring **Visual Studio** or **VS Code**
                    - Running your first `dotnet run` command
                    
                    > Make sure to install the latest Long Term Support (LTS) version of .NET to follow along smoothly!
                    """),

                new SeedPost(
                    "Memory Management & Garbage Collection",
                    """
                    # Memory Management & Garbage Collection in C#
                    
                    In modern programming languages, memory management can be a huge bottleneck if not understood correctly. C# is a **managed language**, meaning the **Garbage Collector (GC)** takes care of releasing memory for you. But how does it actually work?
                    
                    ## The Generational Model
                    
                    The GC optimizes performance by splitting objects into three generations:
                    
                    1. **Generation 0:** This is the youngest generation. It contains short-lived objects (like temporary variables inside a method). Garbage collection occurs most frequently here.
                    2. **Generation 1:** This acts as a buffer between short-lived and long-lived objects. If an object survives a Gen 0 sweep, it gets promoted here.
                    3. **Generation 2:** This contains long-lived objects (e.g., Singletons, static data). Collections here are expensive and infrequent.
                    
                    ## The Large Object Heap (LOH)
                    
                    Not all objects are created equal. If an object is larger than **85,000 bytes** (like a huge array), it skips Gen 0 and goes straight to the Large Object Heap.
                    
                    ```csharp
                    // Example of allocating on the LOH
                    byte[] massiveBuffer = new byte[100_000]; 
                    Console.WriteLine("This buffer is on the Large Object Heap!");
                    ```
                    
                    ### Best Practices
                    *   **Avoid large allocations in tight loops:** Allocating on the LOH can cause memory fragmentation.
                    *   **Use `ArrayPool<T>`:** Instead of creating new arrays, rent them from a pool.
                    *   **Dispose of Unmanaged Resources:** Always use the `using` statement for streams and database connections.
                    
                    > **Key Takeaway:** You don't need to manually `free()` memory like in C++, but writing memory-conscious code is still critical for high-performance applications!
                    """),

                new SeedTest(
                    "Fundamentals Quiz",
                    [
                        TI("What is the core concept of organizing code into reusable, encapsulated structures in C#? (Hint: 3 letters)", "OOP", ignoreCase: true, fuzzy: true),
                        SC("Which generation is collected most frequently by the .NET Garbage Collector?", "Generation 0", "Generation 1", "Generation 2", "Large Object Heap"),
                        MC("Which of the following are value types in C#?", ["int", "bool", "struct"], ["string", "class"]),
                        SC("What is the default access modifier for a class in C#?", "internal", "public", "private", "protected"),
                        MC("Which keywords are used for exception handling in C#?", ["try", "catch", "finally"], ["throw", "error", "except"])
                    ],
                    """
                    # Fundamentals Knowledge Check
                    
                    Test your knowledge of the basics covered in this section. This test is extremely lenient and will show you the correct answers upon submission.
                    
                    **Topics included:**
                    - Garbage Collection
                    - Value vs Reference Types
                    - Access Modifiers
                    """,
                    PassingThreshold: 60,
                    ReviewMode: TestReviewMode.FullReview)
            ]),

            new SeedSection("Section 2: Advanced Topics", [
                new SeedVideo(
                    "Asynchronous Programming Deep Dive",
                    """
                    # Asynchronous Programming Deep Dive
                    
                    Asynchronous programming is essential for building scalable applications. In this video, we'll dive straight into the deep end!
                    
                    ### Topics Covered:
                    - The `async` and `await` keywords
                    - What actually happens to your thread
                    - Why `Task.Run` isn't always the answer
                    
                    **Warning:** This is an advanced topic. Make sure you are comfortable with basic delegates and `Task` objects before proceeding.
                    """),

                new SeedPost(
                    "Understanding Async/Await State Machines",
                    """
                    # Understanding Async/Await State Machines
                    
                    When you mark a method as `async` in C#, it isn't just a simple keyword—the compiler completely rewrites your method under the hood!
                    
                    ## The State Machine Transformation
                    
                    Under the hood, the compiler generates a struct that implements `IAsyncStateMachine`. This struct keeps track of:
                    - The current execution state
                    - Local variables (which are hoisted into fields)
                    - The builder used to return the final `Task`
                    
                    Whenever you `await` an incomplete task, the state machine saves your local variables, hooks up a continuation, and **returns control to the calling thread**.
                    
                    ### Code Example
                    
                    Look at this simple method:
                    ```csharp
                    public async Task<string> FetchDataAsync()
                    {
                        Console.WriteLine("Starting fetch...");
                        var data = await httpClient.GetStringAsync("https://api.example.com");
                        return data.ToUpper();
                    }
                    ```
                    
                    Behind the scenes, it's converted into something drastically more complex involving a `MoveNext()` method and an `AsyncTaskMethodBuilder`.
                    
                    ## Why Does This Matter?
                    
                    By returning control to the thread pool while waiting for I/O (like a network request or database call), your application can serve thousands of concurrent users without needing thousands of threads.
                    
                    - **Fewer Threads = Less Memory:** Each thread takes 1MB of stack space.
                    - **Fewer Context Switches:** Your CPU spends more time running code and less time juggling threads.
                    
                    > **Rule of Thumb:** Use `async/await` for **I/O bound** operations. Use `Task.Run` for **CPU bound** operations!
                    """),

                new SeedTest(
                    "Async Programming Quiz",
                    [
                        TI("What interface does the C# compiler generate to manage an async method's execution? (Hint: IAsync...)", "IAsyncStateMachine", ignoreCase: true, fuzzy: true),
                        SC("What happens to the calling thread when an `await` is reached on an incomplete task?", "It is freed to do other work.", "It blocks until the task completes.", "It throws a ThreadAbortException.", "It creates a new background thread."),
                        MC("Which return types are valid for an async method in C#?", ["Task", "Task<T>", "void", "ValueTask<T>"], ["int", "string", "IEnumerable<T>"]),
                        SC("Why should you avoid `async void`?", "It crashes the application if an exception occurs.", "It makes the method run synchronously.", "It requires too much memory.", "It is deprecated in C# 10."),
                        MC("Which scenarios benefit most from asynchronous programming?", ["I/O bound operations", "Network requests", "Database queries"], ["CPU-heavy calculations", "Simple arithmetic"])
                    ],
                    """
                    # Advanced Knowledge Check
                    
                    A slightly stricter test. You will see which answers you got right, but the correct answers won't be revealed if you make a mistake.
                    
                    **Topics included:**
                    - `IAsyncStateMachine`
                    - Thread pool behavior
                    - `async void` pitfalls
                    """,
                    PassingThreshold: 80,
                    ReviewMode: TestReviewMode.AnswersAndCorrectness)
            ])
        ]);
}
