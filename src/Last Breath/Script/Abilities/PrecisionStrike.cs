namespace Playground.Script.Abilities
{
    using Godot;
    using Playground.Script.Abilities.Effects;

    public class PrecisionStrike : AbilityBase
    {
        private float _damage = 0;
        private const string TextureUID = "uid://du5wtjaopx2pd";
        public PrecisionStrike(ICharacter owner) : base(owner,
                cooldown: 3,
                cost: 2,
                type: Enums.ResourceType.Combopoints,
                activateOnlyOnCaster: false)
        {
            LoadData();
        }

        public override void Activate()
        {
            base.Activate();
            Target.Health.TakeDamage(_damage);
        }

        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new PrecisionEffect()],
            TargetEffects = [new ClumsinessEffect()]
        };

        protected override void LoadData()
        {
            Icon = ResourceLoader.Load<Texture2D>(TextureUID);
            // e.x _damage = GetDamageFromConfigFile();
        }
    }
}
