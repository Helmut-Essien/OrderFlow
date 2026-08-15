import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { AUTH_FIELD_LIMITS } from '../../../../core/auth/auth.models';
import {
  passwordsMatchValidator,
  requiredTrimmed
} from '../../../../shared/validators/auth.validators';

interface ApiValidationError {
  propertyName?: string;
  errorMessage?: string;
}

/** Auth Gateway: login and signup tabs. License key is collected on signup only. */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block h-full' }
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly mode = signal<'login' | 'signup'>('login');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly passwordVisible = signal(false);
  readonly confirmPasswordVisible = signal(false);

  /** Backend: SignUpCommandValidator + LoginCommandValidator + DTO StringLength */
  readonly limits = AUTH_FIELD_LIMITS;

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  toggleConfirmPasswordVisibility(): void {
    this.confirmPasswordVisible.update((visible) => !visible);
  }

  readonly form = this.fb.nonNullable.group({
    licenseKey: [''],
    shopName: [''],
    displayName: ['', [Validators.maxLength(AUTH_FIELD_LIMITS.displayName)]],
    phone: ['', [Validators.maxLength(AUTH_FIELD_LIMITS.phone)]],
    email: [
      '',
      [
        requiredTrimmed,
        Validators.email,
        Validators.maxLength(AUTH_FIELD_LIMITS.email)
      ]
    ],
    password: [
      '',
      [requiredTrimmed, Validators.maxLength(AUTH_FIELD_LIMITS.password)]
    ],
    confirmPassword: ['']
  });

  constructor() {
    const initialMode =
      this.route.snapshot.queryParamMap.get('mode') === 'signup' ? 'signup' : 'login';
    this.applyModeValidators(initialMode);
    this.mode.set(initialMode);
  }

  /** Switches tabs and re-applies validators (license/shop required only on signup). */
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

  /** Login or signup. Email is lowercased on submit to match server storage. */
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
          email: value.email.trim().toLowerCase(),
          password: value.password
        })
        .pipe(takeUntilDestroyed(this.destroyRef))
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
    const displayName = value.displayName.trim();
    const phone = value.phone.trim();

    this.submitting.set(true);
    this.auth
      .signUp({
        licenseKey: value.licenseKey.trim(),
        shopName: value.shopName.trim(),
        displayName: displayName || undefined,
        phone: phone || undefined,
        email: value.email.trim().toLowerCase(),
        password: value.password
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
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
        Validators.maxLength(AUTH_FIELD_LIMITS.licenseKey)
      ]);
      shopName.setValidators([
        requiredTrimmed,
        Validators.maxLength(AUTH_FIELD_LIMITS.shopName)
      ]);
      displayName.setValidators([Validators.maxLength(AUTH_FIELD_LIMITS.displayName)]);
      phone.setValidators([Validators.maxLength(AUTH_FIELD_LIMITS.phone)]);
      password.setValidators([
        requiredTrimmed,
        Validators.minLength(AUTH_FIELD_LIMITS.passwordMin),
        Validators.maxLength(AUTH_FIELD_LIMITS.password)
      ]);
      confirmPassword.setValidators([
        requiredTrimmed,
        Validators.maxLength(AUTH_FIELD_LIMITS.password)
      ]);
      this.form.setValidators(passwordsMatchValidator);
    } else {
      licenseKey.clearValidators();
      shopName.clearValidators();
      displayName.setValidators([Validators.maxLength(AUTH_FIELD_LIMITS.displayName)]);
      phone.setValidators([Validators.maxLength(AUTH_FIELD_LIMITS.phone)]);
      password.setValidators([
        requiredTrimmed,
        Validators.maxLength(AUTH_FIELD_LIMITS.password)
      ]);
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
