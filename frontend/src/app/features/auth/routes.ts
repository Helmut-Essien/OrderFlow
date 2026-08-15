import { Routes } from '@angular/router';

/** Auth Gateway at `/login` (guest-only via parent `guestGuard`). */
export const AUTH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent)
  }
];
