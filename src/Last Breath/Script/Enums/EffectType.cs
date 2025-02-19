namespace Playground.Script.Enums
{
    using System;

    [Flags]
    public enum EffectType
    {
        None = 0,
        Buff = 1,
        Debuff = 2,
        Regeneration = 4,
        Poison = 8,
        Bleeding = 16,
        Fear = 32,
        Stun = 64,
        Cleans = 128,
    }
}
