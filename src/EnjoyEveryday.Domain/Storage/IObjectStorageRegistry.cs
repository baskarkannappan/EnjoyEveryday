using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Storage;

public interface IObjectStorageRegistry
{
    IObjectStorage Default { get; }
    IObjectStorage GetProvider(string providerId);
    bool TryGetProvider(string providerId, out IObjectStorage? provider);
    IEnumerable<string> RegisteredProviders { get; }
}
