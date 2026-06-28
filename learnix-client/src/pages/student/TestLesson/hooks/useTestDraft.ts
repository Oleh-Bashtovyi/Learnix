export interface AnswerState {
    selectedOptions: number[];
    textValue: string;
}

export interface TestDraft {
    attemptId: string;
    answers: Record<number, AnswerState>;
}

export function getDraft(lessonId: string): TestDraft | null {
    try {
        const raw = sessionStorage.getItem(`test-draft-${lessonId}`);
        return raw ? (JSON.parse(raw) as TestDraft) : null;
    } catch {
        return null;
    }
}

export function saveDraft(lessonId: string, draft: TestDraft): void {
    try {
        sessionStorage.setItem(`test-draft-${lessonId}`, JSON.stringify(draft));
    } catch {
        // sessionStorage might be full or blocked — fail silently
    }
}

export function clearDraft(lessonId: string): void {
    try {
        sessionStorage.removeItem(`test-draft-${lessonId}`);
    } catch {
        // ignore
    }
}
