import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Attaches `Authorization: Bearer` when a non-expired JWT is present.
 * API 401s (except login/signup) clear the session so an expired token cannot linger.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const authorized = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authorized).pipe(
    catchError((err: HttpErrorResponse) => {
      const isCredentialPost =
        req.url.includes('/api/auth/login') || req.url.includes('/api/auth/signup');
      if (err.status === 401 && !isCredentialPost) {
        auth.handleUnauthorized();
      }

      return throwError(() => err);
    })
  );
};
