import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () =>
      import('./features/landing/routes').then((m) => m.LANDING_ROUTES)
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./core/layout/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/dashboard/routes').then((m) => m.DASHBOARD_ROUTES)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
