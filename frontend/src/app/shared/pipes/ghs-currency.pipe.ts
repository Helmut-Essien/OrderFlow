import { Pipe, PipeTransform } from '@angular/core';

/** Formats a number as `GHS 1,250.00` using `en-GH`. Null/NaN become `GHS 0.00`. */
@Pipe({
  name: 'ghsCurrency',
  standalone: true
})
export class GhsCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null || Number.isNaN(value)) {
      return 'GHS 0.00';
    }

    return `GHS ${value.toLocaleString('en-GH', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    })}`;
  }
}
