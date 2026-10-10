using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ExperienceService
{
    private readonly IExperienceRepository _experienceRepository;
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;

    public ExperienceService(IExperienceRepository experienceRepository, IExperienceFeedbackRepository feedbackRepository, ITenantContext tenantContext, IUserContext userContext)
    {
        _experienceRepository = experienceRepository;
        _feedbackRepository = feedbackRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<IEnumerable<Experience>> GetExperiencesAsync(CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetAllAsync(_tenantContext.TenantId, cancellationToken);
    }

    public async Task<Experience?> GetExperienceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<Experience> CreateExperienceAsync(string title, string description, string dnaPayload, Guid createdByUserId, string? status = null, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.create")) throw new UnauthorizedAccessException("Requires experience.create permission.");
        var experience = new Experience
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Title = title,
            Description = description,
            Status = status ?? ExperienceStatus.Idea.ToString(),
            DnaPayload = dnaPayload,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _experienceRepository.AddAsync(experience, cancellationToken);
    }

    public async Task UpdateExperienceAsync(Guid id, string title, string description, string dnaPayload, string status, string changeReason, Guid modifiedByUserId, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.create")) throw new UnauthorizedAccessException("Requires experience.create permission.");
        var experience = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (experience == null) throw new InvalidOperationException("Experience not found.");

        experience.Title = title;
        experience.Description = description;
        experience.DnaPayload = dnaPayload;
        experience.Status = status;
        experience.UpdatedAt = DateTimeOffset.UtcNow;

        await _experienceRepository.UpdateAsync(experience, changeReason, modifiedByUserId, cancellationToken);
    }

    public async Task DeleteExperienceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_userContext.HasPermission("experience.create")) throw new UnauthorizedAccessException("Requires experience.create permission.");
        await _experienceRepository.DeleteAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceVersion>> GetExperienceHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _experienceRepository.GetVersionsAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetFeedbackForExperienceAsync(Guid experienceId, CancellationToken cancellationToken = default)
    {
        return await _feedbackRepository.GetByExperienceIdAsync(_tenantContext.TenantId, experienceId, cancellationToken);
    }

    public async Task<ExperienceFeedback> LeaveFeedbackAsync(Guid experienceId, Guid teacherId, int stars, string notes, string rating = "", CancellationToken cancellationToken = default)
    {
        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ExperienceId = experienceId,
            TeacherId = teacherId,
            Stars = stars,
            Notes = notes,
            Rating = rating,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await _feedbackRepository.AddAsync(feedback, cancellationToken);
    }
}
