import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../seo/seo.service';

/** Public 404: `noindex` so unknown URLs do not rank as a copy of the marketing home. */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './not-found.component.html',
  host: { class: 'block min-h-full' }
})
export class NotFoundComponent {
  constructor() {
    inject(SeoService).applyPrivatePage('Page not found | OrderFlow');
  }
}
