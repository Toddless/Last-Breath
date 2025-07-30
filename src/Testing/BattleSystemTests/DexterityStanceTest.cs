namespace LastBreathTest.BattleSystemTests
{
    using LastBreath.Components.Interfaces;
    using LastBreath.Script;
    using LastBreath.Script.BattleSystem;
    using LastBreath.Script.Enums;

    public class DexterityStanceTest : StanceBase
    {
        public DexterityStanceTest(ICharacter owner, IResource resource, IStanceActivationEffect effect, Stance stanceType) : base(owner, resource, effect, stanceType)
        {
        }
    }
}
