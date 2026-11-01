using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public interface IClassroomCommunicationService
{
    Task<ClassroomConversationsFeedDto> GetConversationsFeedAsync(
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? classroomId = null,
        CancellationToken ct = default);

    Task<IEnumerable<ParentChatMessage>> GetChildMessagesForEducatorAsync(
        Guid childId,
        int take = 50,
        CancellationToken ct = default);

    Task<ParentChatMessage> SendEducatorMessageAsync(
        Guid childId,
        string messageText,
        Guid? classroomId = null,
        CancellationToken ct = default);

    Task MarkConversationAsReadByEducatorAsync(Guid childId, CancellationToken ct = default);

    Task<bool> ApproveAgentDraftAsync(Guid messageId, CancellationToken ct = default);

    Task<bool> DiscardAgentDraftAsync(Guid messageId, CancellationToken ct = default);
}

public class ClassroomCommunicationService : IClassroomCommunicationService
{
    private readonly IParentCommunicationRepository _communicationRepo;
    private readonly IChildRepository _childRepo;
    private readonly IClassroomRepository _classroomRepo;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IRealtimeNotificationService? _realtimeNotificationService;

    public ClassroomCommunicationService(
        IParentCommunicationRepository communicationRepo,
        IChildRepository childRepo,
        IClassroomRepository classroomRepo,
        ITenantContext tenantContext,
        IUserContext userContext,
        IRealtimeNotificationService? realtimeNotificationService = null)
    {
        _communicationRepo = communicationRepo;
        _childRepo = childRepo;
        _classroomRepo = classroomRepo;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _realtimeNotificationService = realtimeNotificationService;
    }

    public async Task<ClassroomConversationsFeedDto> GetConversationsFeedAsync(
        Guid? organizationId = null,
        Guid? branchId = null,
        Guid? classroomId = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Classrooms in scope
        var allClassrooms = (await _classroomRepo.GetAllAsync(tenantId, ct)).Where(c => c.IsActive).ToList();
        if (classroomId.HasValue && classroomId.Value != Guid.Empty)
        {
            allClassrooms = allClassrooms.Where(c => c.Id == classroomId.Value).ToList();
        }
        else if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            allClassrooms = allClassrooms.Where(c => c.BranchId == branchId.Value).ToList();
        }
        var classroomLookup = allClassrooms.ToDictionary(c => c.Id);

        // Recent communications
        var comms = (await _communicationRepo.GetRecentCommunicationsAsync(
            tenantId,
            organizationId,
            branchId,
            classroomId,
            limit: 100,
            cancellationToken: ct)).ToList();

        // Children in tenant
        var children = (await _childRepo.GetAllAsync(tenantId, ct)).ToList();
        if (classroomId.HasValue && classroomId.Value != Guid.Empty)
        {
            children = children.Where(c => c.ClassroomId == classroomId.Value).ToList();
        }
        var childrenLookup = children.ToDictionary(c => c.Id);

        var convoList = new List<ClassroomConversationItemDto>();
        var childIds = comms.Select(c => c.ChildId).Distinct().ToList();

        foreach (var cId in childIds)
        {
            var childComms = comms.Where(c => c.ChildId == cId).OrderByDescending(c => c.CreatedAt).ToList();
            var latest = childComms.FirstOrDefault();
            if (latest == null) continue;

            childrenLookup.TryGetValue(cId, out var ch);
            var childName = ch != null ? $"{ch.FirstName} {ch.LastName}".Trim() : (!string.IsNullOrWhiteSpace(latest.ChildName) ? latest.ChildName : "Student");
            var cRoomId = ch?.ClassroomId ?? latest.ClassroomId;
            Classroom? cls = null;
            if (cRoomId.HasValue) classroomLookup.TryGetValue(cRoomId.Value, out cls);
            var cRoomName = cls?.Name ?? latest.ClassroomName ?? "Classroom";
            var unreadCount = childComms.Count(c => c.TeacherId == Guid.Empty && c.DeliveryStatus != "Read");

            convoList.Add(new ClassroomConversationItemDto
            {
                ChildId = cId,
                ChildName = childName,
                ClassroomId = cRoomId,
                ClassroomName = cRoomName,
                ParentName = latest.RecipientName ?? "Parent",
                ParentUserId = latest.RecipientUserId,
                LastMessageSnippet = !string.IsNullOrWhiteSpace(latest.ContentFinal) ? latest.ContentFinal : (latest.ContentDraft ?? ""),
                LastMessageTime = latest.SentAt ?? latest.CreatedAt,
                UnreadCount = unreadCount
            });
        }

        // Include enrolled children with no prior communications
        foreach (var ch in childrenLookup.Values)
        {
            if (convoList.Any(c => c.ChildId == ch.Id)) continue;
            Classroom? cls = null;
            if (ch.ClassroomId.HasValue) classroomLookup.TryGetValue(ch.ClassroomId.Value, out cls);

            convoList.Add(new ClassroomConversationItemDto
            {
                ChildId = ch.Id,
                ChildName = $"{ch.FirstName} {ch.LastName}".Trim(),
                ClassroomId = ch.ClassroomId,
                ClassroomName = cls?.Name ?? "Classroom",
                ParentName = "Family & Guardians",
                ParentUserId = null,
                LastMessageSnippet = "No messages yet · Click to start chat",
                LastMessageTime = ch.CreatedAt,
                UnreadCount = 0
            });
        }

        var sorted = convoList.OrderByDescending(c => c.UnreadCount > 0).ThenByDescending(c => c.LastMessageTime).ToList();

        return new ClassroomConversationsFeedDto
        {
            TotalUnreadCount = sorted.Sum(c => c.UnreadCount),
            Conversations = sorted
        };
    }

    public async Task<IEnumerable<ParentChatMessage>> GetChildMessagesForEducatorAsync(
        Guid childId,
        int take = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var comms = (await _communicationRepo.GetByChildIdAsync(tenantId, childId, ct)).ToList();

        var messages = new List<ParentChatMessage>();
        foreach (var c in comms)
        {
            var isFromParent = (c.TeacherId == Guid.Empty);
            var content = !string.IsNullOrWhiteSpace(c.ContentFinal) ? c.ContentFinal : c.ContentDraft;

            if (!string.IsNullOrWhiteSpace(content))
            {
                messages.Add(new ParentChatMessage
                {
                    Id = c.Id,
                    ChildId = c.ChildId,
                    SenderName = isFromParent ? (c.RecipientName ?? "Parent") : (!string.IsNullOrWhiteSpace(c.TeacherName) ? c.TeacherName : "Educator"),
                    Content = content,
                    Timestamp = c.SentAt ?? c.CreatedAt,
                    IsFromParent = isFromParent,
                    DeliveryStatus = c.DeliveryStatus ?? "Delivered",
                    IsAgentDraft = c.IsAgentDraft,
                    AgentTokenId = c.AgentTokenId
                });
            }
        }

        return messages.OrderBy(m => m.Timestamp).TakeLast(take);
    }

    public async Task<ParentChatMessage> SendEducatorMessageAsync(
        Guid childId,
        string messageText,
        Guid? classroomId = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;
        var sanitized = messageText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Message content cannot be empty.", nameof(messageText));

        var child = await _childRepo.GetByIdAsync(tenantId, childId, ct);
        var classroom = child?.ClassroomId.HasValue == true
            ? await _classroomRepo.GetByIdAsync(tenantId, child.ClassroomId.Value, ct)
            : null;

        var teacherName = _userContext.Email?.Split('@')[0] ?? "Educator";

        var comm = new ParentCommunication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChildId = childId,
            ChildName = child != null ? $"{child.FirstName} {child.LastName}".Trim() : "Child",
            ClassroomId = classroom?.Id ?? classroomId,
            ClassroomName = classroom?.Name ?? "Classroom",
            TeacherId = userId != Guid.Empty ? userId : Guid.NewGuid(),
            TeacherName = teacherName,
            CommunicationType = "EducatorMessage",
            ContentDraft = sanitized,
            ContentFinal = sanitized,
            DeliveryChannel = "InApp",
            DeliveryStatus = "Sent",
            Status = ParentCommunicationStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow,
            SentAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var created = await _communicationRepo.CreateAsync(comm, ct);

        if (_realtimeNotificationService != null)
        {
            try
            {
                await _realtimeNotificationService.NotifyEducatorMessageSentAsync(new ParentMessageNotification
                {
                    MessageId = created.Id,
                    TenantId = tenantId,
                    ChildId = created.ChildId,
                    ChildName = created.ChildName ?? string.Empty,
                    ClassroomId = created.ClassroomId,
                    ClassroomName = created.ClassroomName ?? string.Empty,
                    SenderName = teacherName,
                    Content = sanitized,
                    Timestamp = created.SentAt ?? created.CreatedAt,
                    IsFromParent = false
                }, ct);
            }
            catch { }
        }

        return new ParentChatMessage
        {
            Id = created.Id,
            ChildId = created.ChildId,
            SenderName = teacherName,
            Content = sanitized,
            Timestamp = created.SentAt ?? created.CreatedAt,
            IsFromParent = false,
            DeliveryStatus = created.DeliveryStatus
        };
    }

    public async Task MarkConversationAsReadByEducatorAsync(Guid childId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        await _communicationRepo.MarkAsReadByEducatorAsync(tenantId, childId, ct);
    }

    public async Task<bool> ApproveAgentDraftAsync(Guid messageId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        var comm = await _communicationRepo.GetByIdAsync(tenantId, messageId, ct);
        if (comm == null) return false;

        var approved = await _communicationRepo.ApproveDraftAsync(tenantId, messageId, userId, ct);
        if (approved && _realtimeNotificationService != null)
        {
            try
            {
                var content = !string.IsNullOrWhiteSpace(comm.ContentFinal) ? comm.ContentFinal : comm.ContentDraft;
                await _realtimeNotificationService.NotifyEducatorMessageSentAsync(new ParentMessageNotification
                {
                    MessageId = comm.Id,
                    TenantId = tenantId,
                    ChildId = comm.ChildId,
                    ChildName = comm.ChildName ?? string.Empty,
                    ClassroomId = comm.ClassroomId,
                    ClassroomName = comm.ClassroomName ?? string.Empty,
                    SenderName = comm.TeacherName ?? "Educator",
                    Content = content,
                    Timestamp = DateTimeOffset.UtcNow,
                    IsFromParent = false
                }, ct);
            }
            catch { }
        }

        return approved;
    }

    public async Task<bool> DiscardAgentDraftAsync(Guid messageId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _communicationRepo.DiscardDraftAsync(tenantId, messageId, ct);
    }
}
