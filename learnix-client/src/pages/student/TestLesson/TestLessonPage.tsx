import { useState, useEffect, useRef, useCallback } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { ChevronLeft, ClipboardList, Clock, AlertCircle, Info } from 'lucide-react';
import { cn } from '@/utils/cn';
import { useTestLesson } from '@/hooks/useTestLesson';
import { useStartTestAttempt } from '@/hooks/useStartTestAttempt';
import { useSubmitTestAttempt } from '@/hooks/useSubmitTestAttempt';
import { useMyTestAttempts } from '@/hooks/useMyTestAttempts';
import { queryKeys } from '@/api/queryKeys';
import type { SubmitAttemptResponse, SubmittedAnswerDto } from '@/types/lesson.types';
import { QuestionCard } from './components/QuestionCard';
import { TestResults } from './components/TestResults';
import { TestAttemptHistory } from './components/TestAttemptHistory';
import { TEST_LESSON } from '@/const/localization/testLesson';

// ---------------------------------------------------------------------------
// SessionStorage draft helpers — keyed by lessonId so each test has its own slot
// ---------------------------------------------------------------------------
interface AnswerState {
    selectedOptions: number[];
    textValue: string;
}

interface TestDraft {
    attemptId: string;
    answers: Record<number, AnswerState>;
}

function getDraft(lessonId: string): TestDraft | null {
    try {
        const raw = sessionStorage.getItem(`test-draft-${lessonId}`);
        return raw ? (JSON.parse(raw) as TestDraft) : null;
    } catch {
        return null;
    }
}

function saveDraft(lessonId: string, draft: TestDraft): void {
    try {
        sessionStorage.setItem(`test-draft-${lessonId}`, JSON.stringify(draft));
    } catch {
        // sessionStorage might be full or blocked — fail silently
    }
}

function clearDraft(lessonId: string): void {
    try {
        sessionStorage.removeItem(`test-draft-${lessonId}`);
    } catch {}
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------
type PageState = 'testing' | 'submitted';

export default function TestLessonPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const queryClient = useQueryClient();

    const { data: test, isLoading, isError } = useTestLesson(courseId!, lessonId!);
    const { data: attempts = [] } = useMyTestAttempts(courseId!, lessonId!);
    const startAttempt = useStartTestAttempt(courseId!, lessonId!);
    const submit = useSubmitTestAttempt(courseId!, lessonId!);

    // Core page state
    const [pageState, setPageState] = useState<PageState>('testing');
    const [attemptId, setAttemptId] = useState<string | null>(null);
    const [answers, setAnswers] = useState<Record<number, AnswerState>>({});
    const [submitResult, setSubmitResult] = useState<SubmitAttemptResponse | null>(null);
    const [submittedAnswers, setSubmittedAnswers] = useState<Record<number, AnswerState>>({});
    const [draftRestored, setDraftRestored] = useState(false);

    // Cooldown countdown (seconds)
    const [cooldownSeconds, setCooldownSeconds] = useState<number | null>(null);
    const cooldownIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    const status = test?.studentStatus;
    const canAttempt = status?.canAttempt ?? false;

    // ---------------------------------------------------------------------------
    // On test load: restore from sessionStorage or server in-progress attempt
    // ---------------------------------------------------------------------------
    const didInitRef = useRef(false);

    useEffect(() => {
        if (!test || !lessonId || didInitRef.current) return;
        if (!canAttempt) return;

        const draft = getDraft(lessonId);

        if (draft) {
            // Restore draft from this browser tab/session
            setAttemptId(draft.attemptId);
            setAnswers(draft.answers);
            setDraftRestored(true);
            didInitRef.current = true;
            return;
        }

        if (status?.inProgressAttemptId) {
            // Server knows about an in-progress attempt (e.g., after browser restart)
            setAttemptId(status.inProgressAttemptId);
            const emptyAnswers = Object.fromEntries(
                test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
            );
            setAnswers(emptyAnswers);
            didInitRef.current = true;
            return;
        }

        // No draft, no in-progress — auto-start a new attempt
        startAttempt.mutate(undefined, {
            onSuccess: (res) => {
                setAttemptId(res.attemptId);
                const emptyAnswers = Object.fromEntries(
                    test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
                );
                setAnswers(emptyAnswers);
                saveDraft(lessonId, { attemptId: res.attemptId, answers: emptyAnswers });
            },
        });

        didInitRef.current = true;
    }, [test, lessonId, canAttempt]); // eslint-disable-line react-hooks/exhaustive-deps

    // ---------------------------------------------------------------------------
    // Cooldown countdown timer
    // ---------------------------------------------------------------------------
    useEffect(() => {
        const minutes = status?.cooldownRemainingMinutes;

        if (cooldownIntervalRef.current) {
            clearInterval(cooldownIntervalRef.current);
            cooldownIntervalRef.current = null;
        }

        if (!minutes) {
            setCooldownSeconds(null);
            return;
        }

        setCooldownSeconds(minutes * 60);

        cooldownIntervalRef.current = setInterval(() => {
            setCooldownSeconds((prev) => {
                if (prev === null || prev <= 1) {
                    clearInterval(cooldownIntervalRef.current!);
                    cooldownIntervalRef.current = null;
                    // Refetch test to update canAttempt after cooldown expires
                    queryClient.invalidateQueries({
                        queryKey: queryKeys.tests.lesson(courseId!, lessonId!),
                    });
                    return null;
                }
                return prev - 1;
            });
        }, 1000);

        return () => {
            if (cooldownIntervalRef.current) {
                clearInterval(cooldownIntervalRef.current);
            }
        };
    }, [status?.cooldownRemainingMinutes]); // eslint-disable-line react-hooks/exhaustive-deps

    // ---------------------------------------------------------------------------
    // Answer handlers — save to sessionStorage on every change
    // ---------------------------------------------------------------------------
    const handleOptionToggle = useCallback(
        (questionOrder: number, optionOrder: number, type: string) => {
            setAnswers((prev) => {
                const current = prev[questionOrder] ?? { selectedOptions: [], textValue: '' };
                let nextSelected: number[];

                if (type === 'SingleChoice') {
                    nextSelected = [optionOrder];
                } else {
                    const already = current.selectedOptions.includes(optionOrder);
                    nextSelected = already
                        ? current.selectedOptions.filter((o) => o !== optionOrder)
                        : [...current.selectedOptions, optionOrder];
                }

                const next = {
                    ...prev,
                    [questionOrder]: { ...current, selectedOptions: nextSelected },
                };
                if (lessonId && attemptId) {
                    saveDraft(lessonId, { attemptId, answers: next });
                }
                return next;
            });
        },
        [lessonId, attemptId],
    );

    const handleTextChange = useCallback(
        (questionOrder: number, value: string) => {
            setAnswers((prev) => {
                const next = {
                    ...prev,
                    [questionOrder]: {
                        ...(prev[questionOrder] ?? { selectedOptions: [] }),
                        textValue: value,
                    },
                };
                if (lessonId && attemptId) {
                    saveDraft(lessonId, { attemptId, answers: next });
                }
                return next;
            });
        },
        [lessonId, attemptId],
    );

    // ---------------------------------------------------------------------------
    // Retry starting the attempt (if startAttempt failed)
    // ---------------------------------------------------------------------------
    function retryStart() {
        if (!test || !lessonId) return;
        startAttempt.mutate(undefined, {
            onSuccess: (res) => {
                setAttemptId(res.attemptId);
                saveDraft(lessonId, { attemptId: res.attemptId, answers });
            },
        });
    }

    // ---------------------------------------------------------------------------
    // Submit
    // ---------------------------------------------------------------------------
    const handleSubmit = () => {
        if (!test || !attemptId) return;

        const submittedAnswersList: SubmittedAnswerDto[] = test.questions.map((q) => {
            const ans = answers[q.order] ?? { selectedOptions: [], textValue: '' };
            return {
                questionOrder: q.order,
                selectedOptionOrders: ans.selectedOptions,
                textValue: q.type === 'TextInput' ? ans.textValue || null : null,
            };
        });

        // Snapshot answers BEFORE sending so we can show them in review even if answers state changes
        const snapshot = { ...answers };

        submit.mutate(
            { attemptId, data: { answers: submittedAnswersList } },
            {
                onSuccess: (result) => {
                    if (lessonId) clearDraft(lessonId);
                    setSubmitResult(result);
                    setSubmittedAnswers(snapshot);
                    setPageState('submitted');
                },
                onError: () => {
                    // If the attempt was already submitted (double-tab race), clear draft
                    // and let the refetch show the updated state
                    if (lessonId) clearDraft(lessonId);
                },
            },
        );
    };

    // ---------------------------------------------------------------------------
    // Retake
    // ---------------------------------------------------------------------------
    const handleRetake = () => {
        setPageState('testing');
        setSubmitResult(null);
        setSubmittedAnswers({});
        setDraftRestored(false);
        didInitRef.current = false; // allow re-init to call startAttempt again
        setAttemptId(null);
        setAnswers({});
        // The next render will trigger the init effect again
    };

    // ---------------------------------------------------------------------------
    // Progress indicator
    // ---------------------------------------------------------------------------
    const answeredCount =
        test?.questions.filter((q) => {
            const ans = answers[q.order];
            if (!ans) return false;
            if (q.type === 'TextInput') return ans.textValue.trim().length > 0;
            return ans.selectedOptions.length > 0;
        }).length ?? 0;

    const totalQuestions = test?.questions.length ?? 0;

    // canRetake based on freshly-refetched status (after invalidation from submit)
    const canRetake = test?.studentStatus?.canAttempt === true;

    // ---------------------------------------------------------------------------
    // Render
    // ---------------------------------------------------------------------------
    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <header className="sticky top-0 z-10 border-b border-border bg-card">
                <div className="mx-auto flex h-14 max-w-4xl items-center justify-between px-6">
                    <Link
                        to={`/courses/${courseId}/learn/${lessonId}`}
                        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
                    >
                        <ChevronLeft className="h-4 w-4" />
                        {TEST_LESSON.HEADER.backToLesson}
                    </Link>
                    <div className="flex items-center gap-2 text-sm font-medium">
                        <ClipboardList className="h-4 w-4 text-primary" />
                        {TEST_LESSON.HEADER.testLabel}
                    </div>
                    <div className="w-24" />
                </div>
            </header>

            <div className="mx-auto max-w-4xl px-6 py-10">
                {/* Loading */}
                {isLoading && (
                    <div className="flex items-center justify-center py-20">
                        <div className="space-y-3 text-center">
                            <div className="mx-auto h-10 w-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                            <p className="text-sm text-muted-foreground">{TEST_LESSON.LOADING}</p>
                        </div>
                    </div>
                )}

                {/* Error */}
                {isError && (
                    <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/10 p-6 text-destructive">
                        <AlertCircle className="h-5 w-5 shrink-0" />
                        <p>{TEST_LESSON.ERROR}</p>
                    </div>
                )}

                {/* Content */}
                {!isLoading && !isError && test && (
                    <div className="space-y-8">
                        {/* Test header */}
                        <div>
                            <h1 className="mb-2 font-heading text-2xl font-bold">{test.title}</h1>
                            {test.description && (
                                <p className="text-muted-foreground">{test.description}</p>
                            )}
                            <div className="mt-3 flex flex-wrap gap-4 text-sm text-muted-foreground">
                                <span>
                                    {TEST_LESSON.STATUS.attemptsUsed(
                                        status?.attemptsUsed ?? 0,
                                        test.attemptLimit,
                                    )}
                                </span>
                                <span>
                                    {TEST_LESSON.STATUS.passingScore(test.passingThreshold)}
                                </span>
                                {test.cooldownMinutes && (
                                    <span>{TEST_LESSON.STATUS.cooldown(test.cooldownMinutes)}</span>
                                )}
                            </div>
                        </div>

                        {/* Draft restored notice */}
                        {draftRestored && pageState === 'testing' && (
                            <div className="flex items-center gap-3 rounded-xl border border-primary/30 bg-primary/5 px-5 py-3 text-sm">
                                <Info className="h-4 w-4 shrink-0 text-primary" />
                                <span>{TEST_LESSON.DRAFT_RESTORED}</span>
                            </div>
                        )}

                        {/* Cooldown notice with live countdown */}
                        {cooldownSeconds !== null && !canAttempt && (
                            <div className="flex items-center gap-3 rounded-xl border border-warning/30 bg-warning/10 px-5 py-4 text-sm">
                                <Clock className="h-5 w-5 shrink-0 text-warning" />
                                <span>
                                    {TEST_LESSON.STATUS.cooldownTimer(
                                        String(Math.floor(cooldownSeconds / 60)).padStart(2, '0'),
                                        String(cooldownSeconds % 60).padStart(2, '0'),
                                    )}
                                </span>
                            </div>
                        )}

                        {/* No attempts left */}
                        {!canAttempt && cooldownSeconds === null && pageState !== 'submitted' && (
                            <div className="flex items-center gap-3 rounded-xl border border-destructive/30 bg-destructive/10 px-5 py-4 text-sm text-destructive">
                                <AlertCircle className="h-5 w-5 shrink-0" />
                                {TEST_LESSON.STATUS.noAttemptsLeft}
                            </div>
                        )}

                        {/* Results view */}
                        {pageState === 'submitted' && submitResult && (
                            <TestResults
                                result={submitResult}
                                test={test}
                                courseId={courseId!}
                                lessonId={lessonId!}
                                submittedAnswers={submittedAnswers}
                                onRetake={handleRetake}
                                canRetake={canRetake}
                            />
                        )}

                        {/* Quiz form */}
                        {pageState === 'testing' && canAttempt && (
                            <div className="space-y-4">
                                {/* Progress indicator */}
                                {totalQuestions > 0 && (
                                    <div className="flex items-center justify-between text-xs text-muted-foreground">
                                        <span>
                                            {TEST_LESSON.FORM.answeredOf(
                                                answeredCount,
                                                totalQuestions,
                                            )}
                                        </span>
                                        <div className="h-1.5 w-48 overflow-hidden rounded-full bg-secondary">
                                            <div
                                                className="h-full rounded-full bg-primary transition-all"
                                                style={{
                                                    width: `${totalQuestions > 0 ? (answeredCount / totalQuestions) * 100 : 0}%`,
                                                }}
                                            />
                                        </div>
                                    </div>
                                )}

                                {test.questions
                                    .slice()
                                    .sort((a, b) => a.order - b.order)
                                    .map((question, idx) => (
                                        <QuestionCard
                                            key={question.order}
                                            question={question}
                                            index={idx}
                                            total={test.questions.length}
                                            selectedOptions={
                                                answers[question.order]?.selectedOptions ?? []
                                            }
                                            textValue={answers[question.order]?.textValue ?? ''}
                                            onOptionToggle={(optOrder) =>
                                                handleOptionToggle(
                                                    question.order,
                                                    optOrder,
                                                    question.type,
                                                )
                                            }
                                            onTextChange={(val) =>
                                                handleTextChange(question.order, val)
                                            }
                                        />
                                    ))}

                                <div className="flex justify-end pt-4">
                                    {startAttempt.isError ? (
                                        <div className="flex flex-col items-end gap-2">
                                            <p className="text-sm text-destructive">
                                                {TEST_LESSON.FORM.startError}
                                            </p>
                                            <button
                                                type="button"
                                                onClick={retryStart}
                                                disabled={startAttempt.isPending}
                                                className="rounded-lg bg-secondary px-6 py-2.5 text-sm font-medium transition-colors hover:bg-secondary/80 disabled:opacity-50"
                                            >
                                                {startAttempt.isPending
                                                    ? TEST_LESSON.FORM.starting
                                                    : TEST_LESSON.FORM.retry}
                                            </button>
                                        </div>
                                    ) : (
                                        <button
                                            type="button"
                                            onClick={handleSubmit}
                                            disabled={submit.isPending || !attemptId}
                                            className={cn(
                                                'rounded-lg px-8 py-3 text-sm font-medium text-primary-foreground transition-colors',
                                                submit.isPending || !attemptId
                                                    ? 'cursor-not-allowed bg-primary/60'
                                                    : 'bg-primary hover:bg-primary/90',
                                            )}
                                        >
                                            {submit.isPending
                                                ? TEST_LESSON.FORM.submitting
                                                : !attemptId
                                                  ? TEST_LESSON.FORM.starting
                                                  : TEST_LESSON.FORM.submitButton}
                                        </button>
                                    )}
                                </div>
                            </div>
                        )}

                        {/* Attempt history — only after submitting */}
                        {pageState === 'submitted' && <TestAttemptHistory attempts={attempts} />}
                    </div>
                )}
            </div>
        </div>
    );
}
