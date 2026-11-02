using System;

namespace EnjoyEveryday.UI.Shared.Components.Communication;

public class ConversationItemDto
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
