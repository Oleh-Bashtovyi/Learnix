using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <summary>
/// Creates the trigger that <c>OutboxNotificationListener</c> has always been listening for
/// (ADR-BACK-INFRA-008).
///
/// The listener and the in-process signal shipped; the trigger that fires
/// <c>pg_notify('outbox_new')</c> never made it into a migration, so no database ever had it —
/// verified against a live one: zero user triggers, no <c>notify_outbox_insert</c> function. The
/// outbox still delivered, because the 10-second polling fallback covered for it, which is exactly
/// why nobody noticed the push path was listening to a channel nobody published on.
///
/// FOR EACH STATEMENT, not FOR EACH ROW: one SaveChanges writing five outbox rows should wake the
/// processor once. The payload is empty on purpose — the processor runs its own filtered SELECT,
/// so the only thing worth transmitting is "there is something new".
/// </summary>
public partial class AddOutboxNotifyTrigger : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION notify_outbox_insert() RETURNS trigger AS $$
            BEGIN
                PERFORM pg_notify('outbox_new', '');
                RETURN NULL;
            END;
            $$ LANGUAGE plpgsql;
            """);

        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_outbox_notify ON "OutboxMessages";
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER trg_outbox_notify
            AFTER INSERT ON "OutboxMessages"
            FOR EACH STATEMENT EXECUTE FUNCTION notify_outbox_insert();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_outbox_notify ON "OutboxMessages";
            """);

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS notify_outbox_insert();");
    }
}
