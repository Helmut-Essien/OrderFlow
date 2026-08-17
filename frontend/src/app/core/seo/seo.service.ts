import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { LANDING_SEO, landingJsonLd } from './seo.content';

const JSON_LD_ID = 'of-jsonld';
const CANONICAL_ID = 'of-canonical';
const HREFLANG_GH_ID = 'of-hreflang-gh';
const HREFLANG_DEFAULT_ID = 'of-hreflang-default';

const SOCIAL_SELECTORS = [
  'property="og:type"',
  'property="og:title"',
  'property="og:description"',
  'property="og:url"',
  'property="og:image"',
  'property="og:image:width"',
  'property="og:image:height"',
  'property="og:image:alt"',
  'property="og:image:type"',
  'property="og:site_name"',
  'property="og:locale"',
  'name="twitter:card"',
  'name="twitter:title"',
  'name="twitter:description"',
  'name="twitter:image"',
  'name="twitter:image:alt"'
] as const;

/**
 * Document title, robots, Open Graph, canonical, hreflang, and JSON-LD.
 * The HTML shell is `noindex` so `/login` and `/app` cannot rank. Only prerendered `/`
 * calls {@link applyMarketingHome} and becomes indexable.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /** Indexable marketing tags for `/`. Absolute URLs need `environment.siteUrl` (or `ORDERFLOW_SITE_URL` at build). */
  applyMarketingHome(): void {
    const origin = this.resolveOrigin();
    const pageUrl = origin ? `${origin}/` : null;
    const imageUrl = origin ? `${origin}${LANDING_SEO.imagePath}` : LANDING_SEO.imagePath;

    this.title.setTitle(LANDING_SEO.title);
    this.meta.updateTag({ name: 'description', content: LANDING_SEO.description });
    this.meta.updateTag({ name: 'robots', content: 'index, follow' });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:title', content: LANDING_SEO.title });
    this.meta.updateTag({ property: 'og:description', content: LANDING_SEO.description });
    this.meta.updateTag({ property: 'og:image', content: imageUrl });
    this.meta.updateTag({ property: 'og:image:width', content: LANDING_SEO.imageWidth });
    this.meta.updateTag({ property: 'og:image:height', content: LANDING_SEO.imageHeight });
    this.meta.updateTag({ property: 'og:image:alt', content: LANDING_SEO.imageAlt });
    this.meta.updateTag({ property: 'og:image:type', content: LANDING_SEO.imageType });
    this.meta.updateTag({ property: 'og:site_name', content: LANDING_SEO.siteName });
    this.meta.updateTag({ property: 'og:locale', content: LANDING_SEO.locale });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: LANDING_SEO.title });
    this.meta.updateTag({ name: 'twitter:description', content: LANDING_SEO.description });
    this.meta.updateTag({ name: 'twitter:image', content: imageUrl });
    this.meta.updateTag({ name: 'twitter:image:alt', content: LANDING_SEO.imageAlt });

    if (pageUrl) {
      this.meta.updateTag({ property: 'og:url', content: pageUrl });
      this.setCanonical(pageUrl);
      this.setHreflang(pageUrl);
    } else {
      this.meta.removeTag('property="og:url"');
      this.removeCanonical();
      this.removeHreflang();
    }

    this.setJsonLd(landingJsonLd(origin || null));
  }

  /**
   * Private surfaces (`/login`, `/app`, unknown URLs) must not compete with `/` in search or previews.
   * Strips marketing OG/JSON-LD so a shared login link does not look like the homepage.
   * @param pageTitle Browser tab title only — not a marketing snippet.
   */
  applyPrivatePage(pageTitle: string): void {
    this.title.setTitle(pageTitle);
    this.meta.updateTag({ name: 'robots', content: 'noindex, nofollow' });
    this.meta.removeTag('name="description"');
    this.clearSocialTags();
    this.removeCanonical();
    this.removeHreflang();
    this.removeJsonLd();
  }

  /**
   * Configured `siteUrl`, then the browser origin after hydration (never during prerender).
   * Prerendered absolute OG/canonical still require `ORDERFLOW_SITE_URL` or a committed generated origin.
   */
  private resolveOrigin(): string {
    const configured = environment.siteUrl.replace(/\/$/, '');
    if (configured) {
      return configured;
    }

    if (this.isBrowser && typeof location !== 'undefined' && location.protocol.startsWith('http')) {
      return location.origin;
    }

    return '';
  }

  private clearSocialTags(): void {
    for (const selector of SOCIAL_SELECTORS) {
      this.meta.removeTag(selector);
    }
  }

  private setCanonical(href: string): void {
    const link = this.getOrCreateLink(CANONICAL_ID);
    link.setAttribute('rel', 'canonical');
    link.setAttribute('href', href);
  }

  private setHreflang(href: string): void {
    const gh = this.getOrCreateLink(HREFLANG_GH_ID);
    gh.setAttribute('rel', 'alternate');
    gh.setAttribute('hreflang', LANDING_SEO.htmlLang);
    gh.setAttribute('href', href);

    const fallback = this.getOrCreateLink(HREFLANG_DEFAULT_ID);
    fallback.setAttribute('rel', 'alternate');
    fallback.setAttribute('hreflang', 'x-default');
    fallback.setAttribute('href', href);
  }

  private removeCanonical(): void {
    this.document.getElementById(CANONICAL_ID)?.remove();
  }

  private removeHreflang(): void {
    this.document.getElementById(HREFLANG_GH_ID)?.remove();
    this.document.getElementById(HREFLANG_DEFAULT_ID)?.remove();
  }

  private setJsonLd(payload: Record<string, unknown>): void {
    let script = this.document.getElementById(JSON_LD_ID) as HTMLScriptElement | null;
    if (!script) {
      script = this.document.createElement('script');
      script.id = JSON_LD_ID;
      script.type = 'application/ld+json';
      this.document.head.appendChild(script);
    }

    script.textContent = JSON.stringify(payload);
  }

  private removeJsonLd(): void {
    this.document.getElementById(JSON_LD_ID)?.remove();
  }

  private getOrCreateLink(id: string): HTMLLinkElement {
    let link = this.document.getElementById(id) as HTMLLinkElement | null;
    if (!link) {
      link = this.document.createElement('link');
      link.id = id;
      this.document.head.appendChild(link);
    }

    return link;
  }
}
