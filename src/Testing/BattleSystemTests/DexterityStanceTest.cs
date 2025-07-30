namespace PlaygroundTest.BattleSystemTests
{
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;

    public class DexterityStanceTest : StanceBase
    {
        public DexterityStanceTest(ICharacter owner, IResource resource, IStanceActivationEffect effect, Stance stanceType) : base(owner, resource, effect, stanceType)
        {
        }
    }
}
