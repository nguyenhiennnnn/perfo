namespace Cinema.Domain.Entities;

public class InvoiceProfile
{
    public int InvoiceProfileId { get; set; }
    public int UserId { get; set; }
    public string TaxCode { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User User { get; set; } = null!;
}
