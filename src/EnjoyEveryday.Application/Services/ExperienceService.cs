using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExperienceService
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly ITenantContext _tenantContext;

    public ExperienceService(IExperienceRepository experienceRepository, ITenantContext tenantContext)
    {
        _experienceRepository = experienceRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<Experience>> GetExperiencesAsync(CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetAllAsync(_tenantContext.TenantId, cancellationToken);
    }

    public async Task<Experience?> GetExperienceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<Experience> CreateExperienceAsync(string title, string description, string dnaPayload, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Title = title,
            Description = description,
            Status = ExperienceStatus.Idea.ToString(),
            DnaPayload = dnaPayload,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _experienceRepository.AddAsync(experience, cancellationToken);
    }

    public async Task UpdateExperienceAsync(Guid id, string title, string description, string dnaPayload, string status, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
    {
        var experience = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (experience == null) throw new InvalidOperationException("Experience not found.");

        experience.Title = title;
        experience.Description = description;
        experience.DnaPayload = dnaPayload;
        experience.Status = status;
        experience.UpdatedAt = DateTimeOffset.UtcNow;

        await _experienceRepository.UpdateAsync(experience, changeReason, modifiedByUserId, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceVersion>> GetExperienceHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetVersionsAsync(id, cancellationToken);
    }
}
