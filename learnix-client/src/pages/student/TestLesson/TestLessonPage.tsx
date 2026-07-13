import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '@/api/queryKeys';
import { QueryError } from '@/components/common/system/QueryError';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { TestReviewMode } from '@/enums/lesson.enums';
import { useMyTestAttempts } from '@/hooks/lesson/useMyTestAttempts';
import { useStartTestAttempt } from '@/hooks/lesson/useStartTestAttempt';
import { useSubmitTestAttempt } from '@/hooks/lesson/useSubmitTestAttempt';
import { useTestLesson } from '@/hooks/lesson/useTestLesson';
import type { SubmitAttemptResponse, SubmittedAnswerDto } from '@/types/lesson.types';
import { QuizForm } from './components/QuizForm';
import { TestAttemptHistory } from './components/TestAttemptHistory';
import { TestAttemptReview } from './components/TestAttemptReview';
import { TestHeader } from './components/TestHeader';
import { TestNotices } from './components/TestNotices';
import { TestResults } from './components/TestResults';
import { type AnswerState, clearDraft, getDraft, saveDraft } from './hooks/useTestDraft';

export type TestPageState = 'testing' | 'submitted';

export default function TestLessonPage() {
    const { courseId, lessonId } = useParams<{ courseId: string; lessonId: string }>();
    const queryClient = useQueryClient();
    const { t } = useTranslation('testLesson');

    const {
        data: test,
        isLoading,
        isError,
        refetch: refetchTest,
    } = useTestLesson(courseId!, lessonId!);
    const { data: attempts = [] } = useMyTestAttempts(courseId!, lessonId!);
    const startAttempt = useStartTestAttempt(courseId!, lessonId!);
    const submit = useSubmitTestAttempt(courseId!, lessonId!);

    // Core page state
    const [pageState, setPageState] = useState<TestPageState>('testing');
    const [attemptId, setAttemptId] = useState<string | null>(null);
    const [answers, setAnswers] = useState<Record<number, AnswerState>>({});
    const [submitResult, setSubmitResult] = useState<SubmitAttemptResponse | null>(null);
    const [submittedAnswers, setSubmittedAnswers] = useState<Record<number, AnswerState>>({});
    const [draftRestored, setDraftRestored] = useState(false);
    const [reviewAttemptId, setReviewAttemptId] = useState<string | null>(null);
    const [showConfirmSubmit, setShowConfirmSubmit] = useState(false);

    // Cooldown countdown (seconds)
    const [cooldownSeconds, setCooldownSeconds] = useState<number | null>(null);
    const cooldownIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    const status = test?.studentStatus;
    const canAttempt = status?.canAttempt ?? false;

    // ---------------------------------------------------------------------------
    // On test load: restore from sessionStorage or server in-progress attempt.
    //
    // Why cleanup resets didInitRef:
    // React 18 Strict Mode preserves refs (but resets state) between its
    // double-mount. Without the cleanup, mount #2 sees didInitRef.current=true
    // (from mount #1) but attemptId=null (state reset) → "Starting…" forever.
    // The cleanup fires between mounts, resetting the ref so mount #2 re-runs
    // the init logic correctly.
    // ---------------------------------------------------------------------------
    const didInitRef = useRef(false);

    useEffect(() => {
        if (!test || !lessonId || didInitRef.current) return;
        if (!canAttempt) return;
        if (pageState !== 'testing') return;

        const draft = getDraft(lessonId);
        let initialized = false;

        if (draft) {
            if (draft.attemptId) {
                // eslint-disable-next-line react-hooks/set-state-in-effect
                setAttemptId(draft.attemptId);

                setAnswers(draft.answers);

                setDraftRestored(true);
                didInitRef.current = true;
                initialized = true;
            } else {
                // Corrupted draft (no attemptId) — discard and start fresh
                clearDraft(lessonId);
            }
        }

        if (!initialized && status?.inProgressAttemptId) {
            setAttemptId(status.inProgressAttemptId);

            setAnswers(
                Object.fromEntries(
                    test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
                ),
            );
            didInitRef.current = true;
            initialized = true;
        }

        if (!initialized) {
            startAttempt.mutate(undefined, {
                onSuccess: (res) => {
                    setAttemptId(res.attemptId);
                    const emptyAnswers = Object.fromEntries(
                        test.questions.map((q) => [
                            q.order,
                            { selectedOptions: [], textValue: '' },
                        ]),
                    );
                    setAnswers(emptyAnswers);
                    saveDraft(lessonId, { attemptId: res.attemptId, answers: emptyAnswers });
                },
            });
            didInitRef.current = true;
        }

        return () => {
            didInitRef.current = false;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [test, lessonId, canAttempt, pageState]);

    // Cooldown countdown timer
    useEffect(() => {
        const minutes = status?.cooldownRemainingMinutes;

        if (cooldownIntervalRef.current) {
            clearInterval(cooldownIntervalRef.current);
            cooldownIntervalRef.current = null;
        }

        if (!minutes) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
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

    // Answer handlers — save to sessionStorage on every change
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

    // Retry starting the attempt (if startAttempt failed)
    function retryStart() {
        if (!test || !lessonId) return;
        startAttempt.mutate(undefined, {
            onSuccess: (res) => {
                setAttemptId(res.attemptId);
                saveDraft(lessonId, { attemptId: res.attemptId, answers });
            },
        });
    }

    // Progress indicator
    const answeredCount =
        test?.questions.filter((q) => {
            const ans = answers[q.order];
            if (!ans) return false;
            if (q.type === 'TextInput') return ans.textValue.trim().length > 0;
            return ans.selectedOptions.length > 0;
        }).length ?? 0;

    const totalQuestions = test?.questions.length ?? 0;

    // Submit
    function handleSubmitClick() {
        if (!test || !attemptId) return;
        if (answeredCount < totalQuestions) {
            setShowConfirmSubmit(true);
        } else {
            executeSubmit();
        }
    }

    function executeSubmit() {
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
    }

    // Retake
    function handleRetake() {
        if (!test || !lessonId) return;

        setPageState('testing');
        setSubmitResult(null);
        setSubmittedAnswers({});
        setDraftRestored(false);
        setAttemptId(null);
        setAnswers({});
        // Mark as initialized so the effect doesn't race with us
        didInitRef.current = true;

        // Call directly — the effect won't re-run if test/canAttempt haven't changed
        const emptyAnswers = Object.fromEntries(
            test.questions.map((q) => [q.order, { selectedOptions: [], textValue: '' }]),
        );
        startAttempt.mutate(undefined, {
            onSuccess: (res) => {
                setAttemptId(res.attemptId);
                setAnswers(emptyAnswers);
                saveDraft(lessonId, { attemptId: res.attemptId, answers: emptyAnswers });
            },
        });
    }

    // canRetake based on freshly-refetched status (after invalidation from submit)
    const canRetake = test?.studentStatus?.canAttempt === true;

    return (
        <div className="min-h-screen bg-background">
            {/* Header */}
            <TestHeader courseId={courseId!} lessonId={lessonId!} />

            <div className="mx-auto max-w-4xl px-6 py-10">
                {/* Loading */}
                {isLoading && (
                    <div className="flex items-center justify-center py-20">
                        <div className="space-y-3 text-center">
                            <div className="mx-auto size-10 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                            <p className="text-sm text-muted-foreground">{t('loading')}</p>
                        </div>
                    </div>
                )}

                {/* Error */}
                {isError && (
                    <QueryError
                        message={t('error')}
                        onRetry={refetchTest}
                        retryLabel={t('common:actions.tryAgain')}
                    />
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
                                    {test.attemptLimit
                                        ? t('status.attemptsUsedLimited', {
                                              used: status?.attemptsUsed ?? 0,
                                              limit: test.attemptLimit,
                                          })
                                        : t('status.attemptsUsedUnlimited', {
                                              used: status?.attemptsUsed ?? 0,
                                          })}
                                </span>
                                <span>
                                    {t('status.passingScore', { pct: test.passingThreshold })}
                                </span>
                                {test.cooldownMinutes && (
                                    <span>
                                        {t('status.cooldown', { minutes: test.cooldownMinutes })}
                                    </span>
                                )}
                            </div>
                        </div>

                        <TestNotices
                            draftRestored={draftRestored}
                            pageState={pageState}
                            canAttempt={canAttempt}
                            cooldownSeconds={cooldownSeconds}
                        />

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
                            <QuizForm
                                test={test}
                                answeredCount={answeredCount}
                                totalQuestions={totalQuestions}
                                answers={answers}
                                attemptId={attemptId}
                                isStartError={startAttempt.isError}
                                isStartPending={startAttempt.isPending}
                                isSubmitPending={submit.isPending}
                                onOptionToggle={handleOptionToggle}
                                onTextChange={handleTextChange}
                                onRetryStart={retryStart}
                                onSubmitClick={handleSubmitClick}
                            />
                        )}

                        {/* Attempt history — only after submitting. A ScoreOnly test has nothing to
                            replay, so it gets no review affordance rather than one that opens onto
                            an explanation of why it is empty. */}
                        {pageState === 'submitted' && (
                            <TestAttemptHistory
                                attempts={attempts}
                                onReview={
                                    test && test.reviewMode !== TestReviewMode.ScoreOnly
                                        ? setReviewAttemptId
                                        : undefined
                                }
                            />
                        )}

                        {pageState === 'submitted' && reviewAttemptId && (
                            <TestAttemptReview
                                courseId={courseId!}
                                lessonId={lessonId!}
                                attemptId={reviewAttemptId}
                                onClose={() => setReviewAttemptId(null)}
                            />
                        )}
                    </div>
                )}
            </div>

            {/* Unanswered questions confirm */}
            {showConfirmSubmit && (
                <ConfirmDialog
                    title={t('form.incompleteTitle')}
                    description={t('form.incompleteDesc')}
                    confirmLabel={t('form.submitButton')}
                    variant="warning"
                    isPending={submit.isPending}
                    onConfirm={() => {
                        setShowConfirmSubmit(false);
                        executeSubmit();
                    }}
                    onClose={() => setShowConfirmSubmit(false)}
                />
            )}
        </div>
    );
}
