namespace EnjoyEveryday.Domain.Entities;

public class OrganizationCurrencyHistory
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string PreviousCurrency { get; set; } = string.Empty;
    public string NewCurrency { get; set; } = string.Empty;
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

