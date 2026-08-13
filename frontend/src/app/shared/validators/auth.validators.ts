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
