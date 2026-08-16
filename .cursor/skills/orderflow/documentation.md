# Documentation conventions

Read from [SKILL.md](SKILL.md). Generated and edited code must be **documented and commented to best standards**. Comments explain **why** and **invariants**. They never restate the next line of code.

Apply this on every new or changed public type in the **same slice** as the feature.

## Shared rules (C# and TypeScript)

**Do**

- Document public types and members with a one-sentence summary that states purpose or invariant
- Document non-obvious business rules: Shop tenancy, plan quotas, optimistic concurrency, secret handling, SKU/email normalization
- Document thrown exceptions / error paths that callers must handle
- Keep comments accurate when the code changes; delete comments that no longer apply
- Prefer a precise type/method name over a comment that only repeats the name

**Do not**

- Narrate obvious code (`i++`, `return result`, `inject(HttpClient)`)
- Leave placeholders (`TODO`, `FIXME`, `HACK`, `add logic here`) or commented-out dead code
- Write summaries that only echo the identifier (`The Product class represents a product.`)
- Duplicate XML/JSDoc on private locals or `CancellationToken` unless behavior is unusual. Public properties still need a one-line `///` (CS1591).

## Backend (C# XML)

Use `///` XML documentation on **public** types and members. Private helpers get an inline `//` only when the why is not obvious.

| Kind | Required docs |
|------|----------------|
| Domain entity / factory / mutator | Type summary **and every public property**. Factory/mutator summaries. Note invariants (max lengths, uniqueness, concurrency `Version`, computed vs stored). |
| Enum | Type summary **and every member** (including obvious names — CS1591 does not skip them). |
| `const` / static quota | One-line summary on each public constant or static (e.g. `ProductConstraints.MaxStock`, `PlanQuota.Starter`). |
| Application command/query | Type summary of the use case. |
| Handler | Type summary **and** `Handle`: side effects, tenancy (`ShopId` from JWT), plan limits, exceptions (`ForbiddenAppException`, `ConflictAppException`, `ConcurrencyAppException`). |
| Validator | Type summary **and** a one-line ctor summary (`Binds field limits from ProductConstraints.`). Extra comments only when a rule is non-obvious (e.g. max length on login password for DoS). |
| Shared DTO / request | Type summary **and every public property** (units, GHS, version, optional vs required). DataAnnotations do not satisfy CS1591. |
| Controller action | Summary of the HTTP operation. `<response>` codes that are not the happy path when they matter (409 concurrency, 403 plan cap). |
| Application interface | Port summary **and every member**. Document thread/tenancy expectations if relevant. |
| Infrastructure adapter | Type summary of the external system and failure behavior. Public members that implement an Application port use `/// <inheritdoc />` — do not restate the port. Never document secrets. |
| EF configuration | Type summary **and** `Configure`. Inline comments on CHECK constraints and global query filters. |
| Test | **No XML.** Method names document behavior (`CreateProduct_WhenSkuExists_ThrowsConflict`). Comment only a non-obvious arrange/assert. Do not set `GenerateDocumentationFile` on test projects. |

XML tags to use: `<summary>` always; `<param>` / `<returns>` when they add meaning beyond the name; `<exception cref="...">` for AppExceptions callers should expect; `<remarks>` for concurrency SQL, plan mapping, or Platform license rules.

```csharp
/// <summary>
/// Creates a product in the authenticated shop, enforcing plan product caps and SKU uniqueness.
/// </summary>
/// <exception cref="ForbiddenAppException">Shop is at <c>PlanQuota.MaxProducts</c>.</exception>
/// <exception cref="ConflictAppException">SKU already exists in the shop.</exception>
public sealed class CreateProductCommandHandler(...) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <summary>
    /// Persists the product and an opening <see cref="StockMovement"/> when initial stock is non-zero.
    /// </summary>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Plan caps are enforced here (not in Domain) so quota can change with Platform planName.
        ...
    }
}
```

Controller XML feeds OpenAPI (`AddOpenApi` in `Program.cs`). Keep action summaries accurate; do not invent undocumented routes.

**CS1591 is a build error** on every `backend/src` project (`Directory.Build.props`: `GenerateDocumentationFile=true` + `WarningsAsErrors` includes `CS1591`). Missing `///` on a public type, property, method, ctor, const, enum member, or **protected override** (`OnModelCreating`) fails the build. One-sentence summaries are enough; do not skip `Handle`, validator ctors, or DTO properties. Overrides of framework members use `/// <inheritdoc />` plus `<remarks>` when OrderFlow-specific behavior matters.

**Do not XML-document:**

- Test projects (`backend/tests/Directory.Build.props` sets `GenerateDocumentationFile=false` and `NoWarn` CS1591)
- EF migrations and snapshots (`Persistence/Migrations/**/*.cs` — never hand-edit generated files)

Private nested types (JSON payloads, SQL row shapes) do not need XML.

## Frontend (TypeScript / Angular JSDoc)

Use `/** */` JSDoc on **exported** APIs. Components: document the class when the selector/name is not enough; document public methods and non-obvious Signals.

| Kind | Required docs |
|------|----------------|
| `*.api.ts` service | Class summary (which controller it mirrors). Method summaries for **every** HTTP method (`list`/`get`/`create`/`update`/extras) — do not leave `create` undocumented. |
| `*.models.ts` | File or type summary. `*_FIELD_LIMITS` must note they mirror Shared DTO `[StringLength]` / Domain constraints. Document helpers (`generateSku`). |
| Validators / pipes | Export summary + `@param` / `@returns` when the contract is not obvious. |
| Core services (`AuthService`, `ShopStateService`) | Class summary; document Signals (what they hold, who updates them); document side effects (token storage, logout navigation). |
| Guards / interceptor | Why they exist and what they read (JWT, guest vs auth). |
| Standalone component | Class summary (the job of the view). Public methods used from the template. Signals that encode workflow (loading, submitting, concurrency retry). |
| `routes.ts` | File-level note only if the route tree is non-obvious (guards, lazy load). |
| Template (`.html`) | Comments only for layout or a11y intent that Tailwind classes do not make obvious (e.g. mobile vs desktop split, skip-link, live region). |
| Specs | Test names document behavior; comments only for non-obvious setup. |

```typescript
/**
 * HTTP client for shop product catalog endpoints.
 * Mirrors `ProductsController`; DTOs match `OrderFlow.Shared/DTOs/Products`.
 */
@Injectable({ providedIn: 'root' })
export class ProductApi {
  /**
   * Lists products for the current shop.
   * @param options `pageSize` is 1–100 (API default 20).
   */
  list(options: { search?: string; category?: string; page?: number; pageSize?: number } = {}) { ... }
}

/** Client field limits; must stay in sync with `ProductConstraints` and Shared DTO `[StringLength]`. */
export const PRODUCT_FIELD_LIMITS = { ... } as const;
```

```html
<!-- Mobile: stacked fields. Desktop (lg+): upload column + form column. -->
```

Do not JSDoc every `inject()`, every template binding, or every Tailwind class.

## Inline comments (both stacks)

Use a short `//` (C# / TypeScript) or `<!-- -->` (HTML) when:

- A branch exists because of a product rule (plan cap, WhatsApp pending, GHS rounding)
- A query filter is tenant-scoped (`ShopId` from JWT / global query filter)
- Stock updates must be atomic with `Version` (do not “fix” to read-modify-write)
- A value is normalized (SKU uppercase, email lowercase) and callers might skip it
- Security: never log license keys / integration keys; redact in Serilog
- Performance: `AsNoTracking` on reads; why search uses `ILike` instead of `ToLower()`
- Production: why a setting is rejected at startup (Dev key, localhost Platform URL)

Place the comment **above** the non-obvious block, not at the end of a long line.
