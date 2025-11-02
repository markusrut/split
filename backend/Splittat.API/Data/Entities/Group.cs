namespace Splittat.API.Data.Entities;

public class Group
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
