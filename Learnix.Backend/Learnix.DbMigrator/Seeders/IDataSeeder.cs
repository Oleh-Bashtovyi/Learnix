namespace Learnix.DbMigrator.Seeders;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
