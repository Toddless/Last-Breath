namespace LastBreath.Npc
{
    using Core.Ai.World.Skirmish;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.MessageBus;
    using Core.Services;
    using Godot;

    /// <summary>
    /// The talk interaction: an Area2D dropped into an NPC scene (or placed standalone for a
    /// static talker). Left click with the player in reach opens the conversation. Under a
    /// world NPC the identity comes from the parent; the exports are the standalone fallback.
    /// </summary>
    [GlobalClass]
    public partial class DialogueActor : Area2D
    {
        private const float InteractDistance = 150f;

        [Export] private string _npcId = string.Empty;
        [Export] private Fractions _faction = Fractions.Human;
        private IGameMessageBus? _messages;
        private IPlayerAccessor? _playerAccessor;

        public override void _Ready()
        {
            _messages = Services.GameServiceProvider.Instance.GetService<IGameMessageBus>();
            _playerAccessor = Services.GameServiceProvider.Instance.GetService<IPlayerAccessor>();
            InputEvent += OnInputEvent;
        }

        private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (!PlayerInReach()) return;

            // A lying body or a fighter mid-battle doesn't talk.
            if (GetParent() is ISkirmishParticipant { IsAlive: false } or ISkirmishParticipant { IsFighting: true }) return;
            // A filled export is the author's override (a spawned parent may carry ANY definition);
            // the parent identity is the fallback, but its instance/faction stay authoritative.
            var parent = GetParent() as INpc;
            string npcId = _npcId.Length > 0 ? _npcId : parent?.Id ?? string.Empty;
            var message = new OpenDialogueMessage(npcId, parent?.InstanceId, parent?.Fraction ?? _faction);
            if (message.NpcId.Length == 0)
            {
                Core.Tracker.TrackError("DialogueActor has no npc id: neither the parent INpc nor the _npcId export provides one");
                return;
            }

            Publish(message);
        }

        /// <summary>Awaited so a handler exception lands in the log instead of vanishing with the task.</summary>
        private async void Publish(OpenDialogueMessage message)
        {
            try
            {
                await _messages!.PublishMessageAsync(message);
            }
            catch (System.Exception exception)
            {
                Core.Tracker.TrackError($"Opening dialogue for '{message.NpcId}' failed: {exception}");
            }
        }

        private bool PlayerInReach() =>
            _playerAccessor?.Player is Node2D player
            && player.GlobalPosition.DistanceTo(GlobalPosition) <= InteractDistance;
    }
}
