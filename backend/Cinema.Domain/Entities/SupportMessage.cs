using Cinema.Domain.Enums;

namespace Cinema.Domain.Entities;

public class SupportMessage
{
    public int SupportMessageId { get; set; }
    public int SupportRequestId { get; set; }
    public SupportMessageAuthorType AuthorType { get; set; }
    public int? AuthorUserId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public SupportRequest SupportRequest { get; set; } = null!;
    public User? AuthorUser { get; set; }
}
