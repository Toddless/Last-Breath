namespace LastBreath.World
{
    using System;
    using System.Threading.Tasks;
    using Core;
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

        public override void _Ready()
        {
            _saveGame = GameServiceProvider.Instance.GetService<ISaveGameService>();
            if (_saveGame is { HasPendingLoad: true }) _ = ApplyWhenSettledAsync();
        }

        private async Task ApplyWhenSettledAsync()
        {
            try
            {
                // One frame: every node's _Ready and the deferred spawn fills have run by then.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                _saveGame!.ApplyPendingLoad();
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to apply the pending save load", e, this);
            }
        }
    }
}
