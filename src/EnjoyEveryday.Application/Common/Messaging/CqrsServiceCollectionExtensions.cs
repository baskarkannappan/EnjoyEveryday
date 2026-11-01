using System;
using System.Linq;
using System.Reflection;
using EnjoyEveryday.Application.Common.Messaging;
using EnjoyEveryday.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace EnjoyEveryday.Application.Common.Messaging;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the native Microsoft-backed CQRS dispatcher and automatically discovers handlers in assemblies.
    /// </summary>
    public static IServiceCollection AddMicrosoftCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IDispatcher, MicrosoftServiceProviderDispatcher>();

        var targetAssemblies = assemblies.Length > 0 ? assemblies : new[] { Assembly.GetExecutingAssembly() };

        foreach (var assembly in targetAssemblies)
        {
            var types = assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false });

            foreach (var type in types)
            {
                // Register IQueryHandler<,>
                var queryInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
                foreach (var iface in queryInterfaces)
                {
                    services.AddScoped(iface, type);
                }

                // Register ICommandHandler<>
                var commandVoidInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
                foreach (var iface in commandVoidInterfaces)
                {
                    services.AddScoped(iface, type);
                }

                // Register ICommandHandler<,>
                var commandResultInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
                foreach (var iface in commandResultInterfaces)
                {
                    services.AddScoped(iface, type);
                }
            }
        }

        return services;
    }
}
