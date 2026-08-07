using System.Data.Common;
using Learnix.Application.Auth.Abstractions;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Learnix.Infrastructure.Persistence.EntityFramework.DatabaseObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Learnix.IntegrationTests.Infrastructure;

/// <summary>
/// The real API, booted in-memory against real Postgres and Redis in throwaway Docker containers.
/// <para>
/// Not the EF in-memory provider: it has no constraints, no real SQL and no transactions, so a test on
/// it passes exactly where production fails. The point of an integration test is to run the pipeline the
/// request actually takes — routing, `[Authorize]`, the MediatR behaviors, EF against Postgres, the
/// Redis cache — so all of those are real. Only two things are swapped: blob storage (an in-memory stub,
/// since no container CRUD touches a file) and the hosted background services (removed, so the outbox
/// worker and SignalR do not run mid-test and make it non-deterministic).
/// </para>
/// </summary>
public sealed class LearnixApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private DbConnection _connection = null!;
    private Respawner _respawner = null!;
    private ConnectionMultiplexer _redisAdmin = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development so the app inherits its dev JWT secret, Google placeholder and AI provider from
        // appsettings.Development.json — everything except where the data actually lives, which is the
        // containers below.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                // Never dialed — IBlobStorageService is stubbed below — but the client is constructed at
                // startup and its connection string must parse.
                ["ConnectionStrings:AzureBlobStorage"] = "UseDevelopmentStorage=true",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddScoped<IBlobStorageService, StubBlobStorageService>();

            // The outbox processor holds a LISTEN/NOTIFY connection and SignalR spins up a hub; neither is
            // exercised by these tests, and both would run concurrently with Respawn's table truncation.
            services.RemoveAll<IHostedService>();

            // The 100-req/min global limiter partitions anonymous traffic by client IP; every test shares
            // one IP, so a full run trips it (429). These tests assert behavior, not throughput — drop the
            // global limiter. Per-endpoint [EnableRateLimiting] policies (auth, uploads, …) stay in force.
            services.PostConfigure<RateLimiterOptions>(options => options.GlobalLimiter = null);
        });
    }

    /// <summary>
    /// Every client this factory hands out is built here instead of via the inherited
    /// <c>CreateClient()</c> (its handler chain is not extensible — see <c>CreateDefaultClient</c> in
    /// <c>WebApplicationFactory</c>). Tests address endpoints by their pre-versioning path (e.g.
    /// <c>/api/wishlist</c>); <see cref="ApiVersionPathHandler"/> inserts the current API version segment
    /// before the request goes out, so a version bump is one line here instead of an edit to every test
    /// file's hardcoded path.
    /// </summary>
    private HttpClient CreateVersionedClient()
    {
        var client = new HttpClient(new ApiVersionPathHandler { InnerHandler = Server.CreateHandler() });
        ConfigureClient(client);
        return client;
    }

    private sealed class ApiVersionPathHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { IsAbsoluteUri: true } uri && uri.AbsolutePath.StartsWith("/api/"))
            {
                var versionedPath = "/api/v1" + uri.AbsolutePath["/api".Length..];
                request.RequestUri = new UriBuilder(uri) { Path = versionedPath }.Uri;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>An HTTP client whose bearer token carries <paramref name="roles"/> under a throwaway user
    /// id — a real, signed JWT validated by the same middleware production uses. No roles → an anonymous
    /// client. Use this where identity does not matter (role gates); use <see cref="ClientForUser"/> where
    /// ownership does.</summary>
    public HttpClient ClientWithRoles(params string[] roles) =>
        roles.Length == 0 ? CreateVersionedClient() : ClientForUser(Guid.NewGuid(), roles);

    /// <summary>An HTTP client for a specific user id, so the same caller can create a resource and then be
    /// recognised as its owner. Course ownership is checked as <c>InstructorId == currentUser.UserId</c>
    /// (ADR-BACK-AUTH-013), which only lines up if the token carries the same id both times.</summary>
    public HttpClient ClientForUser(Guid userId, params string[] roles)
    {
        var client = CreateVersionedClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", MintToken(userId, roles));
        return client;
    }

    /// <summary>An HTTP client for a real, persisted <see cref="User"/> row — needed wherever the endpoint
    /// writes a row with a real foreign key to <c>AspNetUsers</c> (e.g. <c>WishlistItems.UserId</c>).
    /// <see cref="ClientForUser"/>'s throwaway id is enough everywhere ownership is a bare Guid column with
    /// no FK (courses, enrollments) — it is cheaper, so prefer it unless the write actually needs the row.
    /// Roles still ride the JWT only, same as every other client here: nothing is written to
    /// AspNetRoles/AspNetUserRoles.</summary>
    public async Task<HttpClient> ClientForRegisteredUserAsync(params string[] roles)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User($"{Guid.NewGuid():N}@learnix.test", "Probe", "User");
        var created = await userManager.CreateAsync(user, "Passw0rd!123");
        created.Succeeded.Should().BeTrue(string.Join("; ", created.Errors.Select(e => e.Description)));

        return ClientForUser(user.Id, roles);
    }

    private string MintToken(Guid userId, IReadOnlyList<string> roles)
    {
        using var scope = Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokens.GenerateAccessToken(
            userId, $"{userId:N}@learnix.test", "Probe", "User", roles, emailConfirmed: true).Token;
    }

    /// <summary>
    /// Clears every store a test can write to — Postgres tables and the Redis cache. Redis is not
    /// optional: the public category list is cached, so a row Respawn truncated would still be served
    /// from a stale cache entry, and a test priming that cache would leak into the next one. (A failing
    /// test is what proved this the first time the reset only touched Postgres.)
    /// </summary>
    public async Task ResetStateAsync()
    {
        await _respawner.ResetAsync(_connection);
        await _redisAdmin.GetServer(_redisAdmin.GetEndPoints()[0]).FlushDatabaseAsync();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // The app never migrates itself (ADR-BACK-MIGR-001); the DbMigrator does. In tests we stand in for
        // it, applying the same migrations to the fresh container so the schema is real, constraints and all.
        // The migrator also applies the repeatable database objects EF cannot model (the deferrable ordering
        // constraints), so we apply them too — a reorder over HTTP relies on them.
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var databaseObjects = scope.ServiceProvider.GetRequiredService<DatabaseObjectsApplier>();
            await databaseObjects.ApplyAsync();
        }

        _connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            // The schema itself is not data — leave the migration ledger alone, or every reset re-migrates.
            TablesToIgnore = ["__EFMigrationsHistory"],
        });

        // allowAdmin so the reset can issue FLUSHDB; the app's own connection is not admin-enabled.
        _redisAdmin = await ConnectionMultiplexer.ConnectAsync($"{_redis.GetConnectionString()},allowAdmin=true");
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redisAdmin.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
