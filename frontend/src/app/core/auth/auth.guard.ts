import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Sends unauthenticated visitors to `/login`. Used on `/app`. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated) {
    return true;
  }
  return router.createUrlTree(['/login']);
};

/** Sends already-authenticated users to `/app`. Used on `/login`. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated) {
    return true;
  }
  return router.createUrlTree(['/app']);
};
