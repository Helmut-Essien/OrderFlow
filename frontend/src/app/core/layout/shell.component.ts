import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { SeoService } from '../seo/seo.service';
import { ShopStateService } from '../shop/shop-state.service';

/** Authenticated app chrome: desktop sidebar (`lg+`) and phone/tablet top + bottom nav. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly shop = inject(ShopStateService);

  constructor() {
    inject(SeoService).applyPrivatePage('OrderFlow');
  }
}
