using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Behaviors;

namespace OrderFlow.Application;

/// <summary>Registers MediatR handlers, FluentValidation validators, and the validation pipeline behavior.</summary>
public static class DependencyInjection
{
    /// <summary>Adds MediatR, validators from this assembly, and <see cref="ValidationBehavior{TRequest,TResponse}"/>.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
