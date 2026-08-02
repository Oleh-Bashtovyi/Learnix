-- Deferrable uniqueness for the ordered collections (Section within a Course, Lesson within a Section).
--
-- Each collection numbers its members with a compact, gap-free DisplayOrder, unique per parent. A
-- reorder is a permutation of those numbers, and a permutation transiently puts two rows on the same
-- number — which a normal (immediate) unique constraint rejects the instant the first row lands on a
-- slot another still holds. DEFERRABLE INITIALLY DEFERRED tells Postgres to check the constraint once,
-- at COMMIT, by which point the whole permutation is applied and the numbers are unique again. So a
-- plain EF SaveChanges over the reordered entities just works.
--
-- Why a repeatable script and not the EF model. The chain is closed — every EF option dead-ends:
--   .IsUnique()        -> creates a unique INDEX  -> Postgres cannot make an index DEFERRABLE
--   want DEFERRABLE    -> needs a CONSTRAINT      -> EF models a unique constraint as an alternate key
--   alternate key      -> its columns are a KEY   -> EF forbids mutating them
--   immutable column   -> a reorder mutates it    -> SaveChanges throws "DisplayOrder is part of a key"
--   => uniqueness cannot live in the EF model at all; it lives here, outside it.
--
-- As a repeatable, idempotent script it also survives a squash of the migration history
-- (ADR-BACK-MIGR-003), same as the outbox notify trigger. EF leaves (parent, DisplayOrder) unconstrained
-- and this script is the single source of truth for it. The constraint's backing index also serves the
-- parent-id lookups, so no extra index is needed.

-- Sections: unique (CourseId, DisplayOrder)
ALTER TABLE "Sections" DROP CONSTRAINT IF EXISTS "UQ_Sections_CourseId_DisplayOrder";
ALTER TABLE "Sections" ADD CONSTRAINT "UQ_Sections_CourseId_DisplayOrder"
    UNIQUE ("CourseId", "DisplayOrder") DEFERRABLE INITIALLY DEFERRED;

-- Lessons: unique (SectionId, DisplayOrder)
ALTER TABLE "Lessons" DROP CONSTRAINT IF EXISTS "UQ_Lessons_SectionId_DisplayOrder";
ALTER TABLE "Lessons" ADD CONSTRAINT "UQ_Lessons_SectionId_DisplayOrder"
    UNIQUE ("SectionId", "DisplayOrder") DEFERRABLE INITIALLY DEFERRED;
