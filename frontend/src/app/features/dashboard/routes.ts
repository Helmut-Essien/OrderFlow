import { Routes } from '@angular/router';

/** Shop dashboard at `/app` (empty child of the shell). */
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent)
  }
];
