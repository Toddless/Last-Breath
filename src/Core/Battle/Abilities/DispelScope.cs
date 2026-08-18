namespace Core.Battle.Abilities
{
    /// <summary>Which effects a dispel reaches for. The split follows what a fighter wants gone: off
    /// himself the damaging effects and debuffs, off a target the buffs.</summary>
    public enum DispelScope : byte
    {
        Self = 0,
        Target
    }
}
