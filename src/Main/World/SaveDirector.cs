namespace LastBreath.World
{
    using System;
    using System.Threading.Tasks;
    using Core;
    using Core.Events;
    using Core.Save;
    using Godot;
    using Services;

    /// <summary>
    /// ONE node in the world scene (next to NpcWorldDirector). After a load request reloads the
    /// scene, this node applies the pending save file once the fresh world has settled.
    /// </summary>
    [GlobalClass]
    public partial class SaveDirector : Node
    {
        private ISaveGameService? _saveGame;
        private IGameEventBus? _gameEventBus;

        public override void _Ready()
        {
            _saveGame = GameServiceProvider.Instance.GetService<ISaveGameService>();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            if (_saveGame is { HasPendingLoad: true }) _ = ApplyWhenSettledAsync();
        }

        private async Task ApplyWhenSettledAsync()
        {
            try
            {
                // One frame: every node's _Ready and the deferred spawn fills have run by then.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await _saveGame!.ApplyPendingLoadAsync();
                // Until this line the scene ran on the state of the playthrough being left behind.
                // No section announces the load itself, so whoever read that state in _Ready is told here.
                _gameEventBus?.Publish(new GameLoadedEvent());
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to apply the pending save load", e, this);
            }
        }
    }
}
