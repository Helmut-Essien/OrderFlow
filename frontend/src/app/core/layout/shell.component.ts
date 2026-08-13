import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { ShopStateService } from '../shop/shop-state.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html'
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly shop = inject(ShopStateService);
}
