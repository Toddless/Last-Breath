namespace Core.Enums
{
    /// <summary>Which parameter namespace a modifier line speaks: entity stats (<see cref="EntityParameter"/>)
    /// or context-pipeline knobs (<see cref="ContextParameter"/>). The two enums number independently, so a
    /// line key carries the channel to keep their members apart.</summary>
    public enum ModifierChannel : byte
    {
        Entity = 0,
        Context
    }
}
