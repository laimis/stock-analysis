import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    standalone: true,
    name: 'parsedDate'
})
export class ParsedDatePipe implements PipeTransform {
    transform(value: string): string {
        if (!value) return '';

        return value.split('T')[0];
    }
}
