const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

export function formatRelativeTime(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    const diffSecs = Math.round((d.getTime() - Date.now()) / 1000);
    const abs = Math.abs(diffSecs);

    if (abs < 60) return rtf.format(Math.round(diffSecs), 'second');
    if (abs < 3600) return rtf.format(Math.round(diffSecs / 60), 'minute');
    if (abs < 86400) return rtf.format(Math.round(diffSecs / 3600), 'hour');
    if (abs < 604800) return rtf.format(Math.round(diffSecs / 86400), 'day');
    if (abs < 2592000) return rtf.format(Math.round(diffSecs / 604800), 'week');
    if (abs < 31536000) return rtf.format(Math.round(diffSecs / 2592000), 'month');
    return rtf.format(Math.round(diffSecs / 31536000), 'year');
}
