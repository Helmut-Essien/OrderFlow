import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly mode = signal<'login' | 'signup'>('login');
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    licenseKey: [''],
    shopName: [''],
    displayName: [''],
    phone: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  setMode(mode: 'login' | 'signup'): void {
    this.mode.set(mode);
    this.error.set(null);
    if (mode === 'signup') {
      this.form.controls.licenseKey.addValidators(Validators.required);
      this.form.controls.shopName.addValidators(Validators.required);
    } else {
      this.form.controls.licenseKey.clearValidators();
      this.form.controls.shopName.clearValidators();
    }
    this.form.controls.licenseKey.updateValueAndValidity();
    this.form.controls.shopName.updateValueAndValidity();
  }

  submit(): void {
    this.error.set(null);
    const value = this.form.getRawValue();

    if (this.mode() === 'login') {
      if (this.form.controls.email.invalid || this.form.controls.password.invalid) {
        this.form.markAllAsTouched();
        return;
      }
      this.submitting.set(true);
      this.auth.login({ email: value.email, password: value.password }).subscribe({
        next: () => void this.router.navigateByUrl('/'),
        error: (err) => this.handleError(err)
      });
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

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
        next: () => void this.router.navigateByUrl('/'),
        error: (err) => this.handleError(err)
      });
  }

  private handleError(err: HttpErrorResponse): void {
    this.submitting.set(false);
    const message = err.error?.message ?? 'Something went wrong. Please try again.';
    this.error.set(message);
  }
}
