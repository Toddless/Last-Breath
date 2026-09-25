namespace Core.Data
{
    /// <summary>
    /// One recorded battle moment: a combat event plus its position in the global order.
    /// <see cref="Event"/> is intentionally untyped — entries come from per-entity combat buses
    /// and from battle-level sources, which share no common event interface.
    /// </summary>
    public readonly record struct TimelineEntry(int Sequence, object Event);
}
