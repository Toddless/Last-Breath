namespace Playground.Script.Abilities
{
    using Godot;
    using Playground.Script.Abilities.Effects;

    public class PrecisionStrike : AbilityBase
    {
        private const string TextureUID = "uid://du5wtjaopx2pd";
        public PrecisionStrike(ICharacter owner) : base(owner,
                cooldown: 3,
                cost: 2,
                type: Enums.ResourceType.Combopoints,
                activateOnlyOnCaster: false)
        {
            LoadTexture();
        }

        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new PrecisionEffect()],
            TargetEffects = [new ClumsinessEffect()]
        };

        protected override void LoadTexture()
        {
            Icon = ResourceLoader.Load<Texture2D>(TextureUID);
        }
    }
}
