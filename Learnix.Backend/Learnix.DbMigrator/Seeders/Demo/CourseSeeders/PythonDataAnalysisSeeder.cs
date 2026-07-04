using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class PythonDataAnalysisSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "data-science",
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
        ],
        "pythin_thumbnail.png");
}


