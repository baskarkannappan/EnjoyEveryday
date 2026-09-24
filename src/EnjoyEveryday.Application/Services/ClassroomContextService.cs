using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class ClassroomContextData
{
    public Guid ClassroomId { get; set; }
    public int ChildrenCount { get; set; }
    public string AgeGroup { get; set; } = "3–4";
    public string Weather { get; set; } = "Rainy";
    public string Energy { get; set; } = "Active today";
    public List<string> Materials { get; set; } = new();
    public List<string> RecentInterests { get; set; } = new();
    public List<ChildStory> LittleMoments { get; set; } = new();
}

public class ClassroomContextService
{
    private readonly IClassroomRepository _classroomRepository;
    private readonly IChildRepository _childRepository;
    private readonly IChildStoryRepository _storyRepository;
    private readonly ITenantContext _tenantContext;

    public ClassroomContextService(
        IClassroomRepository classroomRepository,
        IChildRepository childRepository,
        IChildStoryRepository storyRepository,
        ITenantContext tenantContext)
    {
        _classroomRepository = classroomRepository;
        _childRepository = childRepository;
        _storyRepository = storyRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ClassroomContextData> GetClassroomContextAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var children = (await _childRepository.GetByClassroomIdAsync(tenantId, classroomId, cancellationToken)).ToList();
        
        var interests = new HashSet<string>();
        var moments = new List<ChildStory>();

        foreach (var child in children)
        {
            var childInterests = await _storyRepository.GetInterestsByChildIdAsync(tenantId, child.Id, cancellationToken);
            foreach (var interest in childInterests)
            {
                interests.Add(interest.Name);
            }

            var childStories = await _storyRepository.GetByChildIdAsync(tenantId, child.Id, cancellationToken);
            moments.AddRange(childStories);
        }

        // Hardcode mock data for now for the UI structure
        return new ClassroomContextData
        {
            ClassroomId = classroomId,
            ChildrenCount = children.Count,
            AgeGroup = "3–4",
            Weather = "Rainy",
            Energy = "Active today",
            Materials = new List<string> { "Blocks", "Paper", "Crayons", "Cardboard" },
            RecentInterests = interests.Take(4).ToList(),
            LittleMoments = moments.OrderByDescending(m => m.CreatedAt).Take(3).ToList()
        };
    }
}
