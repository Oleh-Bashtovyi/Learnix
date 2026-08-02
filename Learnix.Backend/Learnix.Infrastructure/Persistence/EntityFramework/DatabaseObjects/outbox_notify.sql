-- Wakes OutboxProcessorService the moment an outbox row is committed (ADR-BACK-INFRA-008).
--
-- This lives here, and not in an EF migration, because it is a *repeatable* object: its definition is
-- the whole truth about it, and re-applying it costs nothing. A versioned migration would state it once
-- and then quietly lose it the next time the migration history is squashed — which is precisely how the
-- trigger went missing before. The migrator re-applies this file on every run, so the object exists in
-- any database the migrator has touched, whatever happened to the migration history.
--
-- FOR EACH STATEMENT, not FOR EACH ROW: one SaveChanges writing five outbox rows should wake the
-- processor once. The payload is empty on purpose — the processor runs its own filtered SELECT, so the
-- only thing worth transmitting is "there is something new".
--
-- NOTIFY is delivered only after COMMIT: the processor is never woken for a row that got rolled back.

CREATE OR REPLACE FUNCTION notify_outbox_insert() RETURNS trigger AS $$
BEGIN
    PERFORM pg_notify('outbox_new', '');
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_outbox_notify ON "OutboxMessages";

CREATE TRIGGER trg_outbox_notify
AFTER INSERT ON "OutboxMessages"
FOR EACH STATEMENT EXECUTE FUNCTION notify_outbox_insert();
