import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

/** Root routes: landing `/`, guest `/login`, authenticated `/app`, public 404 (never redirect unknown URLs home). */
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
      },
      {
        path: 'products',
        loadChildren: () =>
          import('./features/products/routes').then((m) => m.PRODUCT_ROUTES)
      }
    ]
  },
  {
    path: '404',
    loadComponent: () =>
      import('./core/not-found/not-found.component').then((m) => m.NotFoundComponent)
  },
  {
    path: '**',
    loadComponent: () =>
      import('./core/not-found/not-found.component').then((m) => m.NotFoundComponent)
  }
];
