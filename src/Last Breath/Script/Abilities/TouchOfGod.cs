namespace Playground.Script.Abilities
{
    using Godot;
    using Playground.Script.Abilities.Effects;

    public class TouchOfGod : AbilityBase
    {
        private const string TextureUID = "uid://drp7ikoc6v028";

        public TouchOfGod(ICharacter owner)
            : base(owner,
                cooldown: 6,
                cost: 5,
                type: Enums.ResourceType.Fury,
                activateOnlyOnCaster: true)
        {
            LoadData();
        }

        protected override AbilityEffectConfig ConfigureEffects() => new()
        {
            SelfTarget = [new RegenerationEffect(), new GoliathEffect(stacks: 5)]
        };

        protected override void LoadData() => Icon = ResourceLoader.Load<Texture2D>(TextureUID);
    }
}
