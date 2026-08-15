import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  computed,
  inject,
  signal
} from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';

interface PlanCard {
  name: string;
  blurb: string;
  products: string;
  orders: string;
  users: string;
  ai: string;
  highlighted: boolean;
}

/** Marketing home at `/`. Plan copy matches `PlanQuota`. Motion respects `prefers-reduced-motion`. */
@Component({
  selector: 'app-landing',
  imports: [RouterLink, NgClass],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css',
  host: {
    class: 'block min-h-full',
    '[class.of-lp-reduced]': 'reducedMotion()'
  }
})
export class LandingComponent implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly auth = inject(AuthService);

  readonly menuOpen = signal(false);
  readonly year = new Date().getFullYear();
  /** When true, illustration loops stay static and reveal classes apply immediately. */
  readonly reducedMotion = signal(false);
  readonly tiltX = signal(0);
  readonly tiltY = signal(0);

  readonly sceneTransform = computed(
    () =>
      `rotateX(${-12 + this.tiltX()}deg) rotateY(${-18 + this.tiltY()}deg)`
  );

  readonly cubeFaces = ['front', 'back', 'right', 'left', 'top', 'bottom'] as const;
  readonly coinEdges = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

  readonly plans: PlanCard[] = [
    {
      name: 'Starter',
      blurb: 'One owner, clear stock, WhatsApp-ready.',
      products: '50 products',
      orders: '300 orders / month',
      users: '1 user',
      ai: 'No AI features',
      highlighted: false
    },
    {
      name: 'Growth',
      blurb: 'For shops scaling Accra & Kumasi demand.',
      products: '300 products',
      orders: 'Unlimited orders',
      users: '3 users',
      ai: 'No AI features',
      highlighted: true
    },
    {
      name: 'Business',
      blurb: 'Room to grow with your whole team.',
      products: 'Unlimited products',
      orders: 'Unlimited orders',
      users: '10 users',
      ai: 'AI-ready (later)',
      highlighted: false
    }
  ];

  private observer: IntersectionObserver | null = null;

  ngAfterViewInit(): void {
    const reduced =
      typeof matchMedia !== 'undefined' &&
      matchMedia('(prefers-reduced-motion: reduce)').matches;

    this.reducedMotion.set(reduced);

    const nodes = Array.from(
      this.host.nativeElement.querySelectorAll('.of-reveal, .of-reveal-left')
    ) as HTMLElement[];

    if (reduced) {
      nodes.forEach((el: HTMLElement) => el.classList.add('of-reveal-visible'));
      return;
    }

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('of-reveal-visible');
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { rootMargin: '0px 0px -8% 0px', threshold: 0.12 }
    );

    nodes.forEach((el: HTMLElement) => this.observer?.observe(el));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  /** Pointer tilt for the hero scene. Disabled for touch and reduced motion. */
  onStageMove(event: PointerEvent): void {
    if (this.reducedMotion() || event.pointerType === 'touch') {
      return;
    }
    const el = event.currentTarget as HTMLElement;
    const rect = el.getBoundingClientRect();
    const nx = (event.clientX - rect.left) / rect.width - 0.5;
    const ny = (event.clientY - rect.top) / rect.height - 0.5;
    this.tiltX.set(ny * -10);
    this.tiltY.set(nx * 14);
  }

  resetTilt(): void {
    this.tiltX.set(0);
    this.tiltY.set(0);
  }

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
