using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class JourneyService
{
    private readonly IChildStoryRepository _storyRepository;
    private readonly ITenantContext _tenantContext;

    public JourneyService(IChildStoryRepository storyRepository, ITenantContext tenantContext)
    {
        _storyRepository = storyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<ChildStory>> GetChildStoriesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return await _storyRepository.GetByChildIdAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<IEnumerable<ChildStory>> GetPublishedChildStoriesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var stories = await _storyRepository.GetByChildIdAsync(_tenantContext.TenantId, childId, cancellationToken);
        return stories.Where(s => s.Status == "Published" || s.Status == "Approved");
    }

    public async Task<IEnumerable<ChildInterest>> GetChildInterestsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        return await _storyRepository.GetInterestsByChildIdAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<ChildStory> CreateStoryAsync(Guid childId, Guid teacherId, Guid? scheduleId, string content, CancellationToken cancellationToken = default)
    {
        var story = new ChildStory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ChildId = childId,
            TeacherId = teacherId,
            ExperienceScheduleId = scheduleId,
            Content = content,
            Status = "Draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return await _storyRepository.AddStoryAsync(story, cancellationToken);
    }

    public async Task<ChildInterest> AddInterestAsync(Guid childId, string name, CancellationToken cancellationToken = default)
    {
        var interest = new ChildInterest
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ChildId = childId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        return await _storyRepository.AddInterestAsync(interest, cancellationToken);
    }

    public async Task ApproveStoryAsync(Guid storyId, CancellationToken cancellationToken = default)
    {
        await _storyRepository.UpdateStoryStatusAsync(_tenantContext.TenantId, storyId, "Approved", cancellationToken);
    }
}
