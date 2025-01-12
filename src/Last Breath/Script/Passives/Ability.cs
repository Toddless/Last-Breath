namespace Playground.Script.Passives.Attacks
{
    using System;
    using Godot;
    using Playground.Components;

    public abstract partial class Ability : Node
    {
        public bool HaveISomethinToApplyAfterAttack { get; set; } = false;

        public abstract Type TargetTypeComponent
        {
            get;
        }

        public int BuffLasts { get; set; } = 1;

        public int Cooldown { get; set; } = 4;

        public abstract void AfterBuffEnds(IGameComponent? component);

        public abstract void ActivateAbility(IGameComponent? component);

        public abstract void EffectAfterAttack(IGameComponent? component);
    }
}
