namespace LastBreath.Npc
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Entity;
    using Core.Enums;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Narrative.Dialogues;
    using Core.Services;
    using Core.World.Interactions;
    using Godot;
    using World.Interactions;
    using System.Collections.Generic;

    /// <summary>Supplies talk under a shared target; authored identity overrides preserve static talkers.</summary>
    [GlobalClass]
    public partial class DialogueActor : Node, IInteractionSource
    {
        private const double ValidationIntervalSeconds = 0.1;
        [Export] private string _npcId = string.Empty;
        [Export] private Fractions _faction = Fractions.Human;
        private IDialogueService _dialogue = null!;
        private IPlayerAccessor _players = null!;
        private IGameMessageBus _messages = null!;
        private bool _ownsConversation;
        private double _elapsed;
        private InteractionTarget Target => (InteractionTarget)GetParent();
        private INpc? Npc => Target.GetParent() as INpc;
        private string NpcId => _npcId.Length > 0 ? _npcId : Npc?.Id ?? string.Empty;

        public override void _EnterTree()
        {
            var provider = Services.GameServiceProvider.Instance;
            _dialogue = provider.GetService<IDialogueService>();
            _players = provider.GetService<IPlayerAccessor>();
            _messages = provider.GetService<IGameMessageBus>();
            _dialogue.Ended += ConversationEnded;
        }

        public override void _ExitTree()
        {
            EndOwnedConversation();
            _dialogue.Ended -= ConversationEnded;
        }

        public IEnumerable<InteractionAction> Actions()
        {
            if (!Target.IsAvailable || Npc is { CanTalk: false }) return [];
            bool available = NpcId.Length > 0 && _dialogue.CanStart(NpcId, Npc?.InstanceId, Npc?.Fraction ?? _faction);
            return [new(InteractionActions.Talk, "UI_Interaction_Talk", available,
                available ? null : "UI_Interaction_NoDialogue")];
        }

        public async Task<InteractionResult> Execute(string actionId)
        {
            if (actionId != InteractionActions.Talk || !Actions().Any(action => action.Enabled))
                return InteractionResult.Unavailable;
            await _messages.PublishMessageAsync(new OpenDialogueMessage(NpcId, Npc?.InstanceId, Npc?.Fraction ?? _faction));
            _ownsConversation = _dialogue.IsActive;
            return _ownsConversation ? InteractionResult.Started : InteractionResult.Unavailable;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_ownsConversation) return;
            _elapsed += delta;
            if (_elapsed < ValidationIntervalSeconds) return;
            _elapsed = 0;
            if (_players.Player is not IPlayer { IsAlive: true, IsFighting: false } player
                || player is not Node2D body || !float.IsFinite(InteractionReach.DistanceSquared(body, Target)))
                EndOwnedConversation();
        }

        private void ConversationEnded() => _ownsConversation = false;
        private void EndOwnedConversation()
        {
            if (_ownsConversation) _dialogue.End();
            _ownsConversation = false;
        }
    }
}
