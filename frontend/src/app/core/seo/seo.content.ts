/**
 * Marketing copy for `/`. The document shell (`index.html` / `index.csr.html`) stays generic and
 * `noindex` so `/login` and `/app` cannot leak homepage snippets. Prerender of `/` writes these
 * strings into the HTML via {@link SeoService.applyMarketingHome}.
 */
export const LANDING_SEO = {
  title: 'OrderFlow | WhatsApp orders and inventory for Ghana shops',
  description:
    'Take WhatsApp orders, keep inventory you can trust, and collect Paystack payments in GHS. Built for retailers in Accra, Kumasi, and across Ghana.',
  siteName: 'OrderFlow',
  locale: 'en_GB',
  htmlLang: 'en-GH',
  imagePath: '/assets/og/og-image.jpg',
  imageWidth: '1200',
  imageHeight: '630',
  imageAlt: 'OrderFlow — WhatsApp orders and inventory for Ghana shops',
  imageType: 'image/jpeg'
} as const;

/**
 * JSON-LD graph for the marketing home. No plan prices — licenses come from Platform, not a public catalog.
 * `url` / `image` are filled in when a public origin is known.
 */
export function landingJsonLd(origin: string | null): Record<string, unknown> {
  const pageUrl = origin ? `${origin}/` : undefined;
  const imageUrl = origin ? `${origin}${LANDING_SEO.imagePath}` : undefined;

  return {
    '@context': 'https://schema.org',
    '@graph': [
      {
        '@type': 'Organization',
        '@id': pageUrl ? `${pageUrl}#organization` : undefined,
        name: LANDING_SEO.siteName,
        url: pageUrl,
        description: LANDING_SEO.description,
        areaServed: { '@type': 'Country', name: 'Ghana' }
      },
      {
        '@type': 'WebSite',
        '@id': pageUrl ? `${pageUrl}#website` : undefined,
        name: LANDING_SEO.siteName,
        url: pageUrl,
        inLanguage: LANDING_SEO.htmlLang,
        description: LANDING_SEO.description,
        publisher: pageUrl ? { '@id': `${pageUrl}#organization` } : undefined
      },
      {
        '@type': 'SoftwareApplication',
        '@id': pageUrl ? `${pageUrl}#app` : undefined,
        name: LANDING_SEO.siteName,
        applicationCategory: 'BusinessApplication',
        operatingSystem: 'Web',
        inLanguage: LANDING_SEO.htmlLang,
        description: LANDING_SEO.description,
        url: pageUrl,
        image: imageUrl,
        featureList: [
          'WhatsApp ordering',
          'Inventory management',
          'Paystack payments in GHS'
        ],
        areaServed: { '@type': 'Country', name: 'Ghana' }
      }
    ]
  };
}
