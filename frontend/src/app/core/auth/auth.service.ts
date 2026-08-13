import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ShopStateService } from '../shop/shop-state.service';
import { AuthResponse, LoginRequest, MeResponse, SignUpRequest } from './auth.models';

const TOKEN_KEY = 'orderflow.token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly shopState = inject(ShopStateService);

  readonly currentUser = signal<MeResponse | AuthResponse | null>(null);

  constructor() {
    if (this.token) {
      this.refreshMe().subscribe({
        error: () => this.clearSession()
      });
    }
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }

  signUp(request: SignUpRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/signup`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  refreshMe() {
    return this.http.get<MeResponse>(`${environment.apiUrl}/api/auth/me`).pipe(
      tap((profile) => {
        this.currentUser.set(profile);
        this.shopState.setFromSession(profile);
      })
    );
  }

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
