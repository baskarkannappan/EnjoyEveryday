using System;
using System.Collections.Generic;

namespace EnjoyEveryday.Domain.Entities;

public class ClassroomConversationsFeedDto
{
    public int TotalUnreadCount { get; set; }
    public List<ClassroomConversationItemDto> Conversations { get; set; } = new();
}

public class ClassroomConversationItemDto
{
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
    public string ClassroomName { get; set; } = string.Empty;
    public string ParentName { get; set; } = string.Empty;
    public Guid? ParentUserId { get; set; }
    public string LastMessageSnippet { get; set; } = string.Empty;
    public DateTimeOffset LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}

public class SendMessageRequest
{
    public Guid ChildId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ClassroomId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string? RecipientName { get; set; }
    public string Channel { get; set; } = "InApp";
}
