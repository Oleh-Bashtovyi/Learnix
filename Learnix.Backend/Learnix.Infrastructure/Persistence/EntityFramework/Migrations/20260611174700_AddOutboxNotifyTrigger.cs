using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxNotifyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_outbox_insert() RETURNS trigger AS $$
                BEGIN
                  PERFORM pg_notify('outbox_new', '');
                  RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_outbox_notify
                  AFTER INSERT ON ""OutboxMessages""
                  FOR EACH STATEMENT EXECUTE FUNCTION notify_outbox_insert();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_outbox_notify ON ""OutboxMessages"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_outbox_insert();");
        }
    }
}
