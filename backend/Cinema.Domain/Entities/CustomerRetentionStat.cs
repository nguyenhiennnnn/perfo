namespace Cinema.Domain.Entities;

public class CustomerRetentionStat
{
    public DateOnly CohortMonth { get; set; }
    public int MonthOffset { get; set; }
    public int CohortSize { get; set; }
    public int ReturnedCustomers { get; set; }
    public DateTime UpdatedAt { get; set; }
}
