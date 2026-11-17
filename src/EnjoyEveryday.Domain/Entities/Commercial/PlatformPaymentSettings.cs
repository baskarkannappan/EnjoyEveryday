using System;

namespace EnjoyEveryday.Domain.Entities;

public class PlatformPaymentSettings
{
    public Guid Id { get; set; }
    public string UpiId { get; set; } = "billing@enjoyeveryday";
    public string PayeeName { get; set; } = "EnjoyEveryday Platform";
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public bool UseCustomQrImage { get; set; }
    public string? CustomQrDataUri { get; set; }
    public string? UpdatedByEmail { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

