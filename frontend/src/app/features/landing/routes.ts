import { Routes } from '@angular/router';

/** Marketing landing at `/`. */
export const LANDING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/landing/landing.component').then((m) => m.LandingComponent)
  }
];
