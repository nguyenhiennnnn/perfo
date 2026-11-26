namespace Cinema.Domain.Entities;

public class PublicFormAttemptLimit
{
    public string Key { get; set; } = null!;
    public int Count { get; set; }
    public DateTime WindowStartedAt { get; set; }
}
