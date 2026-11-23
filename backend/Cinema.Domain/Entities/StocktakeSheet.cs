namespace Cinema.Domain.Entities;

public class StocktakeSheet
{
    public int StocktakeSheetId { get; set; }
    public DateOnly StocktakeDate { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<StocktakeSheetLine> Lines { get; set; } = [];
}
