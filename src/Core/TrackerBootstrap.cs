namespace Core
{
    /// <summary>Escape hatch for hosts running without the Godot runtime (unit tests, simulations,
    /// tooling): set <see cref="LogPathOverride"/> BEFORE the first <see cref="Tracker"/> call —
    /// e.g. from a [ModuleInitializer] — and Tracker never touches native engine APIs.</summary>
    public static class TrackerBootstrap
    {
        public static string? LogPathOverride { get; set; }
    }
}
