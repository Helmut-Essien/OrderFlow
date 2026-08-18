import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ShopStateService } from '../shop/shop-state.service';
import { AuthResponse, LoginRequest, MeResponse, SignUpRequest } from './auth.models';
import { isAccessTokenExpired, readJwtExpiryMs } from './jwt';

const TOKEN_KEY = 'orderflow.token';

/**
 * App-wide session: JWT in `localStorage`, `currentUser` Signal, and shop/plan sync via {@link ShopStateService}.
 * Expired tokens are treated as signed out; there is no refresh-token grant in MVP.
 * `localStorage` is browser-only so prerender of `/` does not crash.
 * Core must not import feature modules.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly shopState = inject(ShopStateService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private expiryTimer: ReturnType<typeof setTimeout> | null = null;
  private handlingUnauthorized = false;
  private cachedToken: string | null = null;

  /** Latest `/me` or login/signup payload. Null after logout or a failed token refresh. */
  readonly currentUser = signal<MeResponse | AuthResponse | null>(null);

  constructor() {
    if (!this.isBrowser) {
      return;
    }

    this.cachedToken = this.readStoredToken();
    if (!this.cachedToken || isAccessTokenExpired(this.cachedToken)) {
      this.clearSession();
      return;
    }

    this.scheduleExpiryLogout(readJwtExpiryMs(this.cachedToken));
    this.refreshMe().subscribe({
      error: () => this.clearSession()
    });
  }

  /** OrderFlow JWT, or null when signed out or expired. */
  get token(): string | null {
    if (!this.cachedToken || isAccessTokenExpired(this.cachedToken)) {
      return null;
    }

    return this.cachedToken;
  }

  get isAuthenticated(): boolean {
    return this.token !== null;
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

  /**
   * Drops an expired or rejected session. Navigates to login only from `/app` so landing/login 401s stay put.
   * The flag stays set permanently — once a 401 clears the session, subsequent 401s from in-flight
   * requests are harmless and must not trigger duplicate navigations.
   */
  handleUnauthorized(): void {
    if (this.handlingUnauthorized) {
      return;
    }

    this.handlingUnauthorized = true;
    const onApp = this.router.url.startsWith('/app');
    this.clearSession();
    if (onApp) {
      void this.router.navigateByUrl('/login');
    }
  }

  private storeSession(response: AuthResponse): void {
    this.handlingUnauthorized = false;
    this.cachedToken = response.token;
    if (this.isBrowser) {
      localStorage.setItem(TOKEN_KEY, response.token);
    }
    this.currentUser.set(response);
    this.shopState.setFromSession(response);
    this.scheduleExpiryLogout(Date.parse(response.expiresAt) || readJwtExpiryMs(response.token));
  }

  private clearSession(): void {
    this.clearExpiryTimer();
    this.cachedToken = null;
    if (this.isBrowser) {
      localStorage.removeItem(TOKEN_KEY);
    }
    this.currentUser.set(null);
    this.shopState.clear();
  }

  private readStoredToken(): string | null {
    if (!this.isBrowser) {
      return null;
    }

    return localStorage.getItem(TOKEN_KEY);
  }

  private scheduleExpiryLogout(expiryMs: number | null): void {
    this.clearExpiryTimer();
    if (expiryMs == null || Number.isNaN(expiryMs)) {
      return;
    }

    const delay = expiryMs - Date.now();
    if (delay <= 0) {
      this.clearSession();
      return;
    }

    this.expiryTimer = setTimeout(() => this.logout(), delay);
  }

  private clearExpiryTimer(): void {
    if (this.expiryTimer != null) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }
  }
}
