import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
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

@Component({
  selector: 'app-landing',
  imports: [RouterLink, NgClass],
  templateUrl: './landing.component.html',
  host: { class: 'block min-h-full' }
})
export class LandingComponent implements AfterViewInit, OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  readonly auth = inject(AuthService);

  readonly menuOpen = signal(false);
  readonly year = new Date().getFullYear();

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

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }
}
