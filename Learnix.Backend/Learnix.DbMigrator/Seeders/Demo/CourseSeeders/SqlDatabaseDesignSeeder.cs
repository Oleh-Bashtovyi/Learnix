using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class SqlDatabaseDesignSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "data-science",
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
        ],
        "database_thumbnail.png");
}


