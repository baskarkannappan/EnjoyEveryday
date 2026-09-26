using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class FeedbackService
{
    private readonly IExperienceFeedbackRepository _feedbackRepository;
    private readonly ITenantContext _tenantContext;

    public FeedbackService(IExperienceFeedbackRepository feedbackRepository, ITenantContext tenantContext)
    {
        _feedbackRepository = feedbackRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ExperienceFeedback>> GetAllFeedbackAsync(CancellationToken cancellationToken = default)
    {
        return await _feedbackRepository.GetAllAsync(_tenantContext.TenantId, cancellationToken);
    }

    public async Task<ExperienceFeedback?> GetFeedbackByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _feedbackRepository.GetByIdAsync(_tenantContext.TenantId, id, cancellationToken);
    }

    public async Task<ExperienceFeedback> CreateFeedbackAsync(Guid scheduleId, Guid teacherId, string rating, string notes, CancellationToken cancellationToken = default)
    {
        var feedback = new ExperienceFeedback
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ExperienceScheduleId = scheduleId,
            TeacherId = teacherId,
            Rating = rating,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await _feedbackRepository.AddAsync(feedback, cancellationToken);
    }

    public async Task UpdateFeedbackAsync(ExperienceFeedback feedback, CancellationToken cancellationToken = default)
    {
        await _feedbackRepository.UpdateAsync(feedback, cancellationToken);
    }

    public async Task DeleteFeedbackAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _feedbackRepository.DeleteAsync(_tenantContext.TenantId, id, cancellationToken);
    }
}
