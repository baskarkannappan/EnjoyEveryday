using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EnjoyEveryday.Domain.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Storage;

public class ObjectStorageRegistry : IObjectStorageRegistry
{
    private readonly ConcurrentDictionary<string, IObjectStorage> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _defaultProviderId;
    private readonly ILogger<ObjectStorageRegistry> _logger;

    public ObjectStorageRegistry(
        IEnumerable<IObjectStorage> providers,
        IConfiguration configuration,
        ILogger<ObjectStorageRegistry> logger)
    {
        _logger = logger;

        foreach (var p in providers)
        {
            _providers[p.ProviderId] = p;
        }

        var configuredDefault = configuration["Providers:Storage:DefaultProvider"]
            ?? configuration["Storage:DefaultProvider"]
            ?? LocalDiskObjectStorage.ProviderIdentifier;

        if (_providers.ContainsKey(configuredDefault))
        {
            _defaultProviderId = configuredDefault;
        }
        else if (_providers.ContainsKey(LocalDiskObjectStorage.ProviderIdentifier))
        {
            _defaultProviderId = LocalDiskObjectStorage.ProviderIdentifier;
        }
        else
        {
            _defaultProviderId = _providers.Keys.FirstOrDefault() ?? LocalDiskObjectStorage.ProviderIdentifier;
        }

        _logger.LogInformation("ObjectStorageRegistry initialized with default provider: {DefaultProvider}. Registered: {List}",
            _defaultProviderId, string.Join(", ", _providers.Keys));
    }

    public IObjectStorage Default => GetProvider(_defaultProviderId);

    public IEnumerable<string> RegisteredProviders => _providers.Keys;

    public IObjectStorage GetProvider(string providerId)
    {
        if (TryGetProvider(providerId, out var provider) && provider != null)
        {
            return provider;
        }

        throw new KeyNotFoundException($"Object storage provider '{providerId}' is not registered or supported.");
    }

    public bool TryGetProvider(string providerId, out IObjectStorage? provider)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            provider = Default;
            return true;
        }

        return _providers.TryGetValue(providerId.Trim(), out provider);
    }
}
