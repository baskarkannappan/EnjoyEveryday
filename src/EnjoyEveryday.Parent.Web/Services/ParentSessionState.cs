using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Parent.Web.Services;

public class ParentSessionState
{
    public ParentAuthorizedChild? SelectedChild { get; private set; }
    public IReadOnlyList<ParentAuthorizedChild> AuthorizedChildren { get; private set; } = Array.Empty<ParentAuthorizedChild>();

    public event Action? OnChildChanged;

    public void SetAuthorizedChildren(IEnumerable<ParentAuthorizedChild> children)
    {
        AuthorizedChildren = children.ToList();
        if (SelectedChild == null || !AuthorizedChildren.Any(c => c.ChildId == SelectedChild.ChildId))
        {
            SelectedChild = AuthorizedChildren.FirstOrDefault();
            OnChildChanged?.Invoke();
        }
    }

    public void SelectChild(Guid childId)
    {
        var child = AuthorizedChildren.FirstOrDefault(c => c.ChildId == childId);
        if (child != null && child.ChildId != SelectedChild?.ChildId)
        {
            SelectedChild = child;
            OnChildChanged?.Invoke();
        }
    }
}
