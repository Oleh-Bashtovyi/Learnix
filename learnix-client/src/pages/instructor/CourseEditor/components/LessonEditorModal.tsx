import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { ConfirmDialog } from '@/components/common/ui/ConfirmDialog';
import { LessonType } from '@/enums/lesson.enums';
import {
    useCreatePostLesson,
    useCreateTestLesson,
    useCreateVideoLesson,
    useUpdatePostLesson,
    useUpdateTestLesson,
    useUpdateVideoLesson,
} from '@/hooks/instructor/useLessonMutations';
import type {
    PostLessonFormData,
    TestLessonFormData,
    VideoLessonFormData,
} from '@/schemas/lesson.schema';
import type { CourseForEditLessonDto } from '@/types/course.types';
import { PostLessonForm } from './PostLessonForm';
import { TestLessonForm } from './TestLessonForm';
import { VideoLessonForm } from './VideoLessonForm';

interface Props {
    courseId: string;
    sectionId: string;
    lessonType: LessonType;
    lesson?: CourseForEditLessonDto;
    onClose: () => void;
}

export function LessonEditorModal({ courseId, sectionId, lessonType, lesson, onClose }: Props) {
    const { t } = useTranslation('instructor');
    const overlayRef = useRef<HTMLDivElement>(null);
    const [isDirty, setIsDirty] = useState(false);
    const [showConfirm, setShowConfirm] = useState(false);

    const handleAttemptClose = useCallback(() => {
        if (isDirty) {
            setShowConfirm(true);
        } else {
            onClose();
        }
    }, [isDirty, onClose]);

    const createVideo = useCreateVideoLesson(courseId, sectionId);
    const createPost = useCreatePostLesson(courseId, sectionId);
    const createTest = useCreateTestLesson(courseId, sectionId);
    const updateVideo = useUpdateVideoLesson(courseId);
    const updatePost = useUpdatePostLesson(courseId);
    const updateTest = useUpdateTestLesson(courseId);

    const isEditing = !!lesson;

    const TITLES: Record<LessonType, { create: string; edit: string }> = {
        Video: { create: t('modalNewVideo'), edit: t('modalEditVideo') },
        Post: { create: t('modalNewPost'), edit: t('modalEditPost') },
        Test: { create: t('modalNewTest'), edit: t('modalEditTest') },
    };

    const title = TITLES[lessonType][isEditing ? 'edit' : 'create'];

    function handleVideoSubmit(data: VideoLessonFormData) {
        if (isEditing) {
            updateVideo.mutate({ lessonId: lesson.id, data }, { onSuccess: onClose });
        } else {
            createVideo.mutate(data, { onSuccess: onClose });
        }
    }

    function handlePostSubmit(data: PostLessonFormData) {
        if (isEditing) {
            updatePost.mutate({ lessonId: lesson.id, data }, { onSuccess: onClose });
        } else {
            createPost.mutate(data, { onSuccess: onClose });
        }
    }

    function handleTestSubmit(data: TestLessonFormData) {
        if (isEditing) {
            updateTest.mutate({ lessonId: lesson!.id, data }, { onSuccess: onClose });
        } else {
            createTest.mutate(data, { onSuccess: onClose });
        }
    }

    useEffect(() => {
        function onKey(e: KeyboardEvent) {
            if (e.key === 'Escape') handleAttemptClose();
        }
        document.addEventListener('keydown', onKey);
        return () => document.removeEventListener('keydown', onKey);
    }, [handleAttemptClose]);

    const videoIsPending = createVideo.isPending || updateVideo.isPending;
    const postIsPending = createPost.isPending || updatePost.isPending;
    const testIsPending = createTest.isPending || updateTest.isPending;

    return (
        <div
            ref={overlayRef}
            className="fixed inset-0 z-50 flex items-center justify-center bg-foreground/30 p-4"
            onClick={(e) => {
                if (e.target === overlayRef.current) handleAttemptClose();
            }}
        >
            <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-xl bg-card shadow-xl">
                <div className="flex items-center justify-between border-b border-border p-5">
                    <h2 className="font-heading font-semibold text-foreground">{title}</h2>
                    <button
                        onClick={handleAttemptClose}
                        className="text-muted-foreground transition-colors hover:text-foreground"
                    >
                        <X size={18} />
                    </button>
                </div>
                <div className="p-5">
                    {lessonType === 'Video' && (
                        <VideoLessonForm
                            lesson={lesson}
                            isPending={videoIsPending}
                            onSubmit={handleVideoSubmit}
                            onCancel={handleAttemptClose}
                            onDirtyChange={setIsDirty}
                        />
                    )}
                    {lessonType === 'Post' && (
                        <PostLessonForm
                            lesson={lesson}
                            isPending={postIsPending}
                            onSubmit={handlePostSubmit}
                            onCancel={handleAttemptClose}
                            onDirtyChange={setIsDirty}
                        />
                    )}
                    {lessonType === 'Test' && (
                        <TestLessonForm
                            lesson={lesson}
                            isPending={testIsPending}
                            onSubmit={handleTestSubmit}
                            onCancel={handleAttemptClose}
                            onDirtyChange={setIsDirty}
                        />
                    )}
                </div>
            </div>

            {showConfirm && (
                <ConfirmDialog
                    title={t('confirmUnsavedTitle')}
                    description={t('confirmUnsavedDesc')}
                    confirmLabel={t('confirmUnsavedDiscard')}
                    variant="destructive"
                    onConfirm={onClose}
                    onClose={() => setShowConfirm(false)}
                />
            )}
        </div>
    );
}
