using EnjoyEveryday.Application.Services;
using Microsoft.AspNetCore.DataProtection;

namespace EnjoyEveryday.Infrastructure.Services;

public sealed class DataProtectionCredentialProtector : ICredentialProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("EnjoyEveryday.OrgRazorpaySecrets.v1");
    }

    public string Protect(string plaintext)
        => string.IsNullOrEmpty(plaintext) ? string.Empty : _protector.Protect(plaintext);

    public string Unprotect(string protectedText)
        => string.IsNullOrEmpty(protectedText) ? string.Empty : _protector.Unprotect(protectedText);
}

