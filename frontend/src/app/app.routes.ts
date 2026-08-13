import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
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
  { path: '', pathMatch: 'full', redirectTo: 'app' },
  { path: '**', redirectTo: 'app' }
];
