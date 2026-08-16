using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Features.Auth;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.SignUp;

/// <summary>
/// Validates a Platform license, creates the shop + owner, and issues an OrderFlow JWT.
/// </summary>
/// <exception cref="UnauthorizedAppException">License is invalid.</exception>
/// <exception cref="ConflictAppException">License hash or email already registered.</exception>
public sealed class SignUpCommandHandler(
    IPlatformLicenseClient platform,
    IShopRepository shops,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILicenseKeyProtector licenseKeyProtector,
    IJwtTokenService jwt) : IRequestHandler<SignUpCommand, AuthResponse>
{
    /// <summary>Creates shop + owner in one save, then issues a JWT. License plaintext is never persisted.</summary>
    public async Task<AuthResponse> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        var validation = await platform.ValidateAsync(request.LicenseKey.Trim(), cancellationToken);
        if (!validation.IsValid)
            throw new UnauthorizedAppException(validation.Message ?? "License is not valid.");

        // Lookup by hash so plaintext license keys are never stored or compared in SQL.
        var lookupHash = LicenseLookupHasher.Compute(request.LicenseKey.Trim());
        if (await shops.GetByLicenseLookupHashAsync(lookupHash, cancellationToken) is not null)
            throw new ConflictAppException("This shop is already registered. Please sign in.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
            throw new ConflictAppException("An account with this email already exists.");

        var plan = PlanQuota.FromPlanName(validation.PlanName);
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? request.ShopName.Trim()
            : request.DisplayName.Trim();

        var shop = Shop.Create(
            request.ShopName.Trim(),
            request.Phone,
            lookupHash,
            licenseKeyProtector.Protect(request.LicenseKey.Trim()),
            validation.PlanName ?? plan.Name,
            validation.ExpiresAt,
            plan.IsUnrecognized);

        var owner = User.CreateOwner(shop.Id, email, displayName, passwordHasher.Hash(request.Password));

        shops.Add(shop);
        users.Add(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = jwt.Create(owner, shop, plan);
        return AuthMapping.ToAuthResponse(token, owner, shop, plan);
    }
}
