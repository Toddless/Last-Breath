namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using LastBreath.Script.BattleSystem;

    public class DexterityStanceTest : StanceBase
    {
        public DexterityStanceTest(ICharacter owner, IResource resource, IStanceActivationEffect effect, Stance stanceType) : base(owner, resource, effect, stanceType)
        {
        }
    }
}
