namespace Cinema.Domain.Entities;

public class CustomerSegment
{
    public int SegmentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ConditionJson { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
