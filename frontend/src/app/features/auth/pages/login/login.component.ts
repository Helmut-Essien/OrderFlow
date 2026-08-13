import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';

/** Matches FluentValidation NotEmpty for strings (rejects whitespace-only). */
const requiredTrimmed: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (typeof value !== 'string' || value.trim().length === 0) {
    return { required: true };
  }
  return null;
};

const passwordsMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value as string | undefined;
  const confirmPassword = control.get('confirmPassword')?.value as string | undefined;
  if (!confirmPassword?.trim()) {
    return null;
  }
  return password === confirmPassword ? null : { passwordsMismatch: true };
};

interface ApiValidationError {
  propertyName?: string;
  errorMessage?: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  host: { class: 'block h-full' }
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly mode = signal<'login' | 'signup'>('login');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly passwordVisible = signal(false);
  readonly confirmPasswordVisible = signal(false);

  /** Backend: SignUpCommandValidator + LoginCommandValidator */
  private static readonly limits = {
    licenseKey: 100,
    email: 320,
    password: 128,
    passwordMin: 8,
    shopName: 200,
    displayName: 200,
    phone: 50
  } as const;

  readonly limits = LoginComponent.limits;

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible.update((visible) => !visible);
  }

  readonly form = this.fb.nonNullable.group({
    licenseKey: [''],
    shopName: [''],
    displayName: ['', [Validators.maxLength(LoginComponent.limits.displayName)]],
    phone: ['', [Validators.maxLength(LoginComponent.limits.phone)]],
    email: [
      '',
      [
        requiredTrimmed,
        Validators.email,
        Validators.maxLength(LoginComponent.limits.email)
      ]
    ],
    // Login rules by default (password: NotEmpty only)
    password: ['', [requiredTrimmed]],
    confirmPassword: ['']
  });

  constructor() {
    this.applyModeValidators('login');
  }

  setMode(mode: 'login' | 'signup'): void {
    this.mode.set(mode);
    this.error.set(null);
    this.passwordVisible.set(false);
    this.confirmPasswordVisible.set(false);
    if (mode === 'login') {
      this.form.controls.confirmPassword.setValue('');
    }
    this.applyModeValidators(mode);
  }

  showError(
    controlName: keyof LoginComponent['form']['controls'],
    errorCode: string
  ): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorCode);
  }

  showPasswordsMismatch(): boolean {
    return (
      this.mode() === 'signup' &&
      (this.form.controls.confirmPassword.touched || this.form.controls.password.touched) &&
      this.form.hasError('passwordsMismatch')
    );
  }

  submit(): void {
    this.error.set(null);
    this.form.markAllAsTouched();
    this.form.updateValueAndValidity();

    if (this.mode() === 'login') {
      if (
        this.form.controls.email.invalid ||
        this.form.controls.password.invalid
      ) {
        return;
      }

      const value = this.form.getRawValue();
      this.submitting.set(true);
      this.auth
        .login({
          email: value.email.trim(),
          password: value.password
        })
        .subscribe({
          next: () => {
            this.submitting.set(false);
            void this.router.navigateByUrl('/app');
          },
          error: (err) => this.handleError(err)
        });
      return;
    }

    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.auth
      .signUp({
        licenseKey: value.licenseKey.trim(),
        shopName: value.shopName.trim(),
        displayName: value.displayName.trim() || undefined,
        phone: value.phone.trim() || undefined,
        email: value.email.trim(),
        password: value.password
      })
      .subscribe({
        next: () => {
          this.submitting.set(false);
          void this.router.navigateByUrl('/app');
        },
        error: (err) => this.handleError(err)
      });
  }

  private applyModeValidators(mode: 'login' | 'signup'): void {
    const { licenseKey, shopName, displayName, phone, password, confirmPassword } =
      this.form.controls;

    if (mode === 'signup') {
      licenseKey.setValidators([
        requiredTrimmed,
        Validators.maxLength(LoginComponent.limits.licenseKey)
      ]);
      shopName.setValidators([
        requiredTrimmed,
        Validators.maxLength(LoginComponent.limits.shopName)
      ]);
      displayName.setValidators([Validators.maxLength(LoginComponent.limits.displayName)]);
      phone.setValidators([Validators.maxLength(LoginComponent.limits.phone)]);
      password.setValidators([
        requiredTrimmed,
        Validators.minLength(LoginComponent.limits.passwordMin),
        Validators.maxLength(LoginComponent.limits.password)
      ]);
      confirmPassword.setValidators([requiredTrimmed]);
      this.form.setValidators(passwordsMatchValidator);
    } else {
      licenseKey.clearValidators();
      shopName.clearValidators();
      displayName.setValidators([Validators.maxLength(LoginComponent.limits.displayName)]);
      phone.setValidators([Validators.maxLength(LoginComponent.limits.phone)]);
      password.setValidators([requiredTrimmed]);
      confirmPassword.clearValidators();
      this.form.clearValidators();
    }

    licenseKey.updateValueAndValidity({ emitEvent: false });
    shopName.updateValueAndValidity({ emitEvent: false });
    displayName.updateValueAndValidity({ emitEvent: false });
    phone.updateValueAndValidity({ emitEvent: false });
    password.updateValueAndValidity({ emitEvent: false });
    confirmPassword.updateValueAndValidity({ emitEvent: false });
    this.form.updateValueAndValidity({ emitEvent: false });
  }

  private handleError(err: HttpErrorResponse): void {
    this.submitting.set(false);
    const body = err.error as
      | { message?: string; errors?: ApiValidationError[] }
      | null
      | undefined;

    const validationMessages = body?.errors
      ?.map((e) => e.errorMessage?.trim())
      .filter((msg): msg is string => !!msg);

    if (validationMessages && validationMessages.length > 0) {
      this.error.set(validationMessages.join(' '));
      return;
    }

    this.error.set(body?.message ?? 'Something went wrong. Please try again.');
  }
}
