namespace Splittat.API.Data.Entities;

public enum GroupRole
{
    Owner,
    Member
}

public class GroupMember
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupRole Role { get; set; } = GroupRole.Member;
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
