import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Matches FluentValidation NotEmpty for strings (rejects whitespace-only). */
export const requiredTrimmed: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const value = control.value;
  if (typeof value !== 'string' || value.trim().length === 0) {
    return { required: true };
  }
  return null;
};

/** Accepts empty (optional) values; otherwise requires an integer. Used for stock fields. */
export const integerNumber: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const value = control.value;
  if (value === '' || value === null || value === undefined) {
    return null;
  }
  return Number.isInteger(Number(value)) ? null : { integer: true };
};

/** Rejects a numeric zero. Used for stock adjustment delta. */
export const nonZeroNumber: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const value = control.value;
  if (value === '' || value === null || value === undefined) {
    return null;
  }
  return Number(value) === 0 ? { nonZero: true } : null;
};

/** Group validator: `password` and `confirmPassword` must match when confirm is filled. */
export const passwordsMatchValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const password = control.get('password')?.value as string | undefined;
  const confirmPassword = control.get('confirmPassword')?.value as string | undefined;
  if (!confirmPassword?.trim()) {
    return null;
  }
  return password === confirmPassword ? null : { passwordsMismatch: true };
};
