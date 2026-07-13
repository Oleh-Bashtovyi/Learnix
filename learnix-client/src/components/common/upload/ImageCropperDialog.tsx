import { useState } from 'react';
import Cropper from 'react-easy-crop';
import { useTranslation } from 'react-i18next';
import { ZoomIn, ZoomOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { CROP_OUTPUT } from '@/const/upload.constants';
import type { CropArea } from '@/utils/cropImage';

interface ImageCropperDialogProps {
    /** Object URL of the picked file. The dialog is open whenever this is set. */
    imageUrl: string | null;
    aspect: number;
    isSaving: boolean;
    onCancel: () => void;
    onSave: (area: CropArea) => void;
}

export function ImageCropperDialog({
    imageUrl,
    aspect,
    isSaving,
    onCancel,
    onSave,
}: ImageCropperDialogProps) {
    const { t } = useTranslation('common');
    const [crop, setCrop] = useState({ x: 0, y: 0 });
    const [zoom, setZoom] = useState<number>(CROP_OUTPUT.minZoom);
    const [area, setArea] = useState<CropArea | null>(null);

    // A fresh pick must not inherit the previous image's pan/zoom.
    function handleCancel() {
        setCrop({ x: 0, y: 0 });
        setZoom(CROP_OUTPUT.minZoom);
        setArea(null);
        onCancel();
    }

    return (
        <Dialog open={imageUrl !== null} onOpenChange={(open) => !open && handleCancel()}>
            <DialogContent className="max-w-lg">
                <DialogHeader>
                    <DialogTitle>{t('upload.cropTitle')}</DialogTitle>
                </DialogHeader>

                {imageUrl && (
                    <>
                        <div className="relative h-72 w-full overflow-hidden rounded-lg bg-muted sm:h-80">
                            <Cropper
                                image={imageUrl}
                                crop={crop}
                                zoom={zoom}
                                aspect={aspect}
                                minZoom={CROP_OUTPUT.minZoom}
                                maxZoom={CROP_OUTPUT.maxZoom}
                                onCropChange={setCrop}
                                onZoomChange={setZoom}
                                onCropComplete={(_, areaPixels) => setArea(areaPixels)}
                                showGrid={false}
                            />
                        </div>

                        <div className="flex items-center gap-3">
                            <ZoomOut className="size-4 shrink-0 text-muted-foreground" />
                            <input
                                type="range"
                                aria-label={t('upload.zoom')}
                                min={CROP_OUTPUT.minZoom}
                                max={CROP_OUTPUT.maxZoom}
                                step={CROP_OUTPUT.zoomStep}
                                value={zoom}
                                onChange={(e) => setZoom(Number(e.target.value))}
                                className="h-1.5 w-full cursor-pointer appearance-none rounded-full bg-secondary accent-primary"
                            />
                            <ZoomIn className="size-4 shrink-0 text-muted-foreground" />
                        </div>

                        <p className="text-xs text-muted-foreground">{t('upload.cropHint')}</p>

                        <div className="flex justify-end gap-2">
                            <Button variant="outline" onClick={handleCancel} disabled={isSaving}>
                                {t('actions.cancel')}
                            </Button>
                            <Button
                                onClick={() => area && onSave(area)}
                                disabled={isSaving || !area}
                            >
                                {isSaving ? t('actions.uploading') : t('actions.save')}
                            </Button>
                        </div>
                    </>
                )}
            </DialogContent>
        </Dialog>
    );
}
