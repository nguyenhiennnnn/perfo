namespace Cinema.Application.Events;

public static class BusinessEventCatalog
{
    private static readonly Dictionary<Type, IBusinessEventDescriptor> ByType = [];
    private static readonly Dictionary<string, IBusinessEventDescriptor> ByName = [];

    static BusinessEventCatalog()
    {
        Register<OrderConfirmed>();
        Register<OrderCancelled>();
        Register<OrderRefunded>();
        Register<OrderExchanged>();
        Register<DailyClosed>();
        Register<ShowtimeEnded>();
        Register<ShowtimeOpened>();
        Register<ShowtimeSeatsReleased>();
        Register<ShowtimeRoomChanged>();
        Register<TicketCheckedIn>();
        Register<ReviewChanged>();
        Register<GiftCardSent>();
    }

    public static IBusinessEventDescriptor For<TEvent>()
        where TEvent : IBusinessEvent =>
        ByType.TryGetValue(typeof(TEvent), out var descriptor)
            ? descriptor
            : throw new InvalidOperationException(typeof(TEvent).Name);

    public static IBusinessEventDescriptor? Find(string eventName) =>
        ByName.GetValueOrDefault(eventName);

    private static void Register<TEvent>()
        where TEvent : IBusinessEvent
    {
        var descriptor = new BusinessEventDescriptor<TEvent>();
        ByType[typeof(TEvent)] = descriptor;
        ByName[descriptor.Name] = descriptor;
    }
}
