import { TestBed } from '@angular/core/testing';
import { Meta, Title } from '@angular/platform-browser';
import { LANDING_SEO } from './seo.content';
import { SeoService } from './seo.service';

describe('SeoService', () => {
  let seo: SeoService;
  let title: Title;
  let meta: Meta;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    seo = TestBed.inject(SeoService);
    title = TestBed.inject(Title);
    meta = TestBed.inject(Meta);
  });

  it('sets indexable marketing title, description, and social tags on the home', () => {
    seo.applyMarketingHome();

    expect(title.getTitle()).toBe(LANDING_SEO.title);
    expect(meta.getTag('name="description"')?.content).toBe(LANDING_SEO.description);
    expect(meta.getTag('name="robots"')?.content).toBe('index, follow');
    expect(meta.getTag('property="og:image:width"')?.content).toBe(LANDING_SEO.imageWidth);
    expect(meta.getTag('property="og:site_name"')?.content).toBe(LANDING_SEO.siteName);
  });

  it('marks private pages noindex and strips marketing Open Graph so login cannot look like home', () => {
    seo.applyMarketingHome();
    seo.applyPrivatePage('Sign in | OrderFlow');

    expect(title.getTitle()).toBe('Sign in | OrderFlow');
    expect(meta.getTag('name="robots"')?.content).toBe('noindex, nofollow');
    expect(meta.getTag('name="description"')).toBeNull();
    expect(meta.getTag('property="og:title"')).toBeNull();
    expect(meta.getTag('name="twitter:card"')).toBeNull();
  });
});
