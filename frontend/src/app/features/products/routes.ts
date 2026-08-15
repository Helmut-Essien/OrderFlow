import { Routes } from '@angular/router';

/** Inventory: list, `/new`, and `/:id` edit (same form component). */
export const PRODUCT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/product-list/product-list.component').then(
        (m) => m.ProductListComponent
      )
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/product-form/product-form.component').then(
        (m) => m.ProductFormComponent
      )
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/product-form/product-form.component').then(
        (m) => m.ProductFormComponent
      )
  }
];
