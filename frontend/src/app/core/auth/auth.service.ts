import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ShopStateService } from '../shop/shop-state.service';
import { AuthResponse, LoginRequest, MeResponse, SignUpRequest } from './auth.models';

const TOKEN_KEY = 'orderflow.token';

/**
 * App-wide session: JWT in `localStorage`, `currentUser` Signal, and shop/plan sync via {@link ShopStateService}.
 * Core must not import feature modules.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly shopState = inject(ShopStateService);

  /** Latest `/me` or login/signup payload. Null after logout or a failed token refresh. */
  readonly currentUser = signal<MeResponse | AuthResponse | null>(null);

  constructor() {
    if (this.token) {
      this.refreshMe().subscribe({
        error: () => this.clearSession()
      });
    }
  }

  /** OrderFlow JWT, or null when signed out. */
  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }

  /** Registers a shop with a Platform license key, then stores the issued JWT. */
  signUp(request: SignUpRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/signup`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  /** Email/password login. License keys are not sent. */
  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  /** Reloads shop/plan from `GET /api/auth/me` without issuing a new token. */
  refreshMe() {
    return this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`).pipe(
      tap((profile) => {
        this.currentUser.set(profile);
        this.shopState.setFromSession(profile);
      })
    );
  }

  /** Clears the JWT and shop Signals, then navigates to `/login`. */
  logout(): void {
    this.clearSession();
    void this.router.navigateByUrl('/login');
  }

  private storeSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    this.currentUser.set(response);
    this.shopState.setFromSession(response);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.currentUser.set(null);
    this.shopState.clear();
  }
}
