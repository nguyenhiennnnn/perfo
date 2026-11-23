namespace Cinema.Domain.Entities;

public class ComboRecipe
{
    public int ComboRecipeId { get; set; }
    public int ComboId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Combo Combo { get; set; } = null!;
    public ICollection<ComboRecipeLine> Lines { get; set; } = [];
}
