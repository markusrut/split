namespace Splittat.API.Data.Entities;

public enum SplitType
{
    Equal,
    ByItem,
    Percentage,
    Custom
}

public class Split
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid CreatedBy { get; set; }
    public SplitType SplitType { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public Group? Group { get; set; }
    public User Creator { get; set; } = null!;
    public ICollection<ItemAssignment> ItemAssignments { get; set; } = new List<ItemAssignment>();
}
