import { Pipe, PipeTransform } from '@angular/core';
import { formatDistanceToNow } from 'date-fns';

@Pipe({
    name: 'timeAgo',
    standalone: true
})
export class TimeAgoPipe implements PipeTransform {
    transform(value: string | Date | null | undefined): string {
        if (!value) return 'N/A';

        try {
            const date = typeof value === 'string' ? new Date(value) : value;
            return formatDistanceToNow(date, { addSuffix: true });
        } catch (error) {
            return 'Invalid date';
        }
    }
}
