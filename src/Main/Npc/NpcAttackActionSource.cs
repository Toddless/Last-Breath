namespace LastBreath.Npc
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Services;
    using Core.World.Interactions;
    using Godot;
    using World.Interactions;

    public partial class NpcAttackActionSource : Node, IInteractionSource
    {
        private BaseNpc? Npc => GetParent().GetParent() as BaseNpc;
        public IEnumerable<InteractionAction> Actions()
        {
            var player = GameServiceProvider.Instance.GetService<IPlayerAccessor>().Player;
            return Npc is { } npc && player != null && npc.CanStartBattleWith(player)
                ? [new(InteractionActions.Attack, "UI_Interaction_Attack", ExplicitChoice: true)] : [];
        }
        public Task<InteractionResult> Execute(string actionId)
        {
            var player = GameServiceProvider.Instance.GetService<IPlayerAccessor>().Player;
            bool started = actionId == InteractionActions.Attack && player != null && Npc?.TryStartBattleWith(player) == true;
            return Task.FromResult(started ? InteractionResult.Started : InteractionResult.Unavailable);
        }
    }
}
