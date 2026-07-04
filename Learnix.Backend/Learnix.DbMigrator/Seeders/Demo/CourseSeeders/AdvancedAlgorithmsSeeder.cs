using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class AdvancedAlgorithmsSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Advanced Algorithms",
        "A deep dive into advanced algorithmic concepts and data structures.",
        49.99m,
        ["algorithms", "computer-science", "advanced"],
        [
            new("Graph Algorithms",
            [
                new SeedVideo("Introduction to Graphs", "Learn about directed, undirected, weighted and unweighted graphs."),
                new SeedPost("Graph Representations", "Adjacency matrix vs Adjacency list."),
            ]),
            new("Dynamic Programming",
            [
                new SeedVideo("Memoization vs Tabulation", "Top-down vs bottom-up approaches to DP."),
                new SeedTest("Algorithms Final Exam",
                    [
                        SC("Which algorithm finds the shortest path in a weighted graph with non-negative weights?",
                            "Dijkstra's Algorithm", "BFS", "DFS", "Prim's Algorithm"),
                        SC("What is the time complexity of binary search?",
                            "O(log n)", "O(n)", "O(n log n)", "O(1)"),
                        MC("Which of the following are sorting algorithms?",
                            ["Merge Sort", "Quick Sort", "Heap Sort"],
                            ["Binary Search", "Dijkstra's", "Kruskal's"]),
                        MC("Which data structures are typically used to implement a priority queue?",
                            ["Heap"],
                            ["Stack", "Queue", "Linked List"]),
                        TI("What does DFS stand for?", "Depth First Search", ignoreCase: true, fuzzy: true),
                        TI("What data structure uses LIFO principle?", "Stack", ignoreCase: true, fuzzy: false),
                        SC("Which algorithm is used for finding minimum spanning tree?", "Kruskal's", "Dijkstra's", "Floyd-Warshall", "Bellman-Ford"),
                        SC("What is the worst-case time complexity of Quick Sort?", "O(n^2)", "O(n log n)", "O(n)", "O(1)"),
                        MC("Which algorithms are used for pattern matching?", ["KMP", "Rabin-Karp"], ["Dijkstra", "A*"]),
                        TI("What is the average time complexity of hash table lookups?", "O(1)", ignoreCase: true, fuzzy: false),
                        SC("In a balanced binary search tree, what is the search time complexity?", "O(log n)", "O(n)", "O(1)", "O(n log n)"),
                        MC("Which graph representations are common?", ["Adjacency Matrix", "Adjacency List"], ["Binary Tree", "Heap"]),
                        TI("What type of algorithm makes the optimal choice at each step?", "Greedy", ignoreCase: true, fuzzy: true),
                        SC("Which tree traversal visits the root first?", "Pre-order", "In-order", "Post-order", "Level-order"),
                        SC("Which DP problem finds the longest common subsequence?", "LCS", "Knapsack", "Coin Change", "Edit Distance"),
                        MC("Which problems can be solved using Dynamic Programming?", ["Fibonacci", "Knapsack", "Matrix Chain Multiplication"], ["Binary Search"]),
                        TI("What does BFS stand for?", "Breadth First Search", ignoreCase: true, fuzzy: true),
                        TI("What algorithm finds the shortest paths between all pairs of vertices?", "Floyd-Warshall", ignoreCase: true, fuzzy: false),
                        SC("What is the time complexity of accessing an element in an array by index?", "O(1)", "O(n)", "O(log n)", "O(n^2)"),
                        SC("Which algorithm is used to find strongly connected components?", "Tarjan's", "Kruskal's", "Prim's", "Dijkstra's"),
                        MC("Which are self-balancing binary search trees?", ["AVL Tree", "Red-Black Tree"], ["B-Tree", "Heap", "Trie"])
                    ],
                    PassingThreshold: 70,
                    AttemptLimit: 5,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


