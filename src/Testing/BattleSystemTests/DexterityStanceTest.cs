namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using LastBreath.Script.BattleSystem;

    public class DexterityStanceTest : StanceBase
    {
        public DexterityStanceTest(IEntity owner, IResource resource, IStanceActivationEffect effect, Stance stanceType) : base(owner, resource, effect, stanceType)
        {
        }
    }
}
