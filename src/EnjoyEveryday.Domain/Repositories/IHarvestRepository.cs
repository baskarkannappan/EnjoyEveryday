namespace EnjoyEveryday.Domain.Repositories;

public class HarvestMetricsDto
{
    public int TotalExperiences { get; set; }
    public int TotalFeedbacks { get; set; }
    public int PatternsDiscovered { get; set; }
}

public class HarvestPerformanceItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public int Uses { get; set; }
    public string Response { get; set; } = string.Empty;
}

public interface IHarvestRepository
{
    Task<HarvestMetricsDto> GetMonthlyMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<HarvestPerformanceItemDto>> GetPerformanceItemsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
