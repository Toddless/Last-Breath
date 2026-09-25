namespace LastBreath.World.Interactions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.MessageBus;
    using Core.World.Interactions;
    using Godot;

    public partial class PlayerInteractionController : Area2D
    {
        private const string FailureMessage = "Interaction failed";
        private const string MissingRefusalSoundFormat =
            "Interaction controller '{0}' plays no refusal sound: it needs its refusal sound player set in the scene";
        [Export] public double RefreshSeconds { get; set; } = 0.1;

        /// <summary>Sound of a refused player command.</summary>
        [Export] private AudioStreamPlayer? RefusalSound { get; set; }
        private readonly HashSet<InteractionTarget> _candidates = [];
        private InteractionService _service = null!;
        private double _elapsed;
        private bool _dirty = true;
        private (Func<Task<InteractionResult>> Run, TaskCompletionSource<InteractionResult> Completion)? _pending;
        private bool _running;
        public Node2D Player => (Node2D)GetParent();
        public int CandidateCount => _candidates.Count;
        public int EvaluationCount { get; private set; }

        public override void _EnterTree()
        {
            _service = Services.GameServiceProvider.Instance.GetService<InteractionService>();
            _service.Controller = this;
        }

        public override void _Ready()
        {
            CollisionLayer = 0;
            CollisionMask = InteractionTarget.DetectionLayer;
            Monitorable = false;
            InputPickable = false;
            AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 1 } });
            AreaEntered += Entered;
            AreaExited += Exited;
            if (RefusalSound == null) ReportMissingRefusalSound();
        }

        private void Entered(Area2D area)
        {
            if (area is InteractionTarget target) { _candidates.Add(target); _dirty = true; }
        }
        private void Exited(Area2D area)
        {
            if (area is InteractionTarget target) { _candidates.Remove(target); _dirty = true; }
        }

        public void CancelPending()
        {
            _pending?.Completion.TrySetResult(InteractionResult.Dropped);
            _pending = null;
        }

        /// <summary>Plays the refusal sound; without one set in the scene a refusal stays silent.</summary>
        public void PlayRefusal() => RefusalSound?.Play();

        /// <summary>Writes the missing refusal sound to the log and the Godot console.</summary>
        private void ReportMissingRefusalSound()
        {
            string message = string.Format(MissingRefusalSoundFormat, GetPath());
            Tracker.TrackError(message, this);
            GD.PrintErr(message);
        }

        public override void _ExitTree()
        {
            _candidates.Clear();
            CancelPending();
            _service.CancelSession(InteractionSessionEndCause.ActorUnavailable);
            if (_service.Controller == this) _service.Controller = null;
            _service.Select(null);
            _dirty = true;
        }

        public override void _PhysicsProcess(double delta)
        {
            _elapsed += delta;
            if (_dirty || _elapsed >= Math.Max(0.02, RefreshSeconds))
            {
                _dirty = false;
                _elapsed = 0;
                _service.ValidateSession();
                RefreshSelection();
            }
            if (!_running && _pending is { } pending)
            {
                _pending = null;
                Run(pending);
            }
        }

        private void RefreshSelection()
        {
            EvaluationCount++;
            if (!_service.CanDiscover()) { _service.Select(null); return; }
            InteractionTarget? best = null;
            float bestDistance = float.PositiveInfinity;
            bool bestEnabled = false;
            foreach (var target in _candidates.ToArray())
            {
                if (!GodotObject.IsInstanceValid(target) || !target.IsInsideTree()) { _candidates.Remove(target); continue; }
                float distance = InteractionReach.DistanceSquared(Player, target);
                if (!float.IsFinite(distance)) continue;
                var actions = target.ReadActions();
                if (actions.Count == 0) continue;
                bool enabled = actions.Any(x => x.Enabled);
                if (best != null && (bestEnabled && !enabled || bestEnabled == enabled && distance > bestDistance)) continue;
                if (best != null && bestEnabled == enabled && distance == bestDistance)
                {
                    if (best == _service.Selected || target != _service.Selected && string.CompareOrdinal(best.Handle.ObjectId, target.Handle.ObjectId) < 0) continue;
                }
                best = target;
                bestEnabled = enabled;
                bestDistance = distance;
            }
            _service.Select(best);
        }

        public Task<InteractionResult> Enqueue(Func<Task<InteractionResult>> run)
        {
            if (_pending != null || _running || !IsInsideTree()) return Task.FromResult(InteractionResult.Dropped);
            var completion = new TaskCompletionSource<InteractionResult>();
            _pending = (run, completion);
            return completion.Task;
        }

        /// <summary>Runs the command, answers its request, then gives the player the feedback its result carries.</summary>
        private async void Run((Func<Task<InteractionResult>> Run, TaskCompletionSource<InteractionResult> Completion) pending)
        {
            _running = true;
            var result = InteractionResult.Unavailable;
            try { result = await pending.Run(); }
            catch (Exception error) { Tracker.TrackException(FailureMessage, error); }
            finally
            {
                pending.Completion.TrySetResult(result);
                _running = false;
                _dirty = true;
            }
            _service.GiveFeedback(result);
        }

        public override void _UnhandledInput(InputEvent e)
        {
            if (!e.IsActionPressed(InteractionActions.Interact) || e.IsEcho() || !_service.CanDiscover() || _service.Selected is not { } target) return;
            GetViewport().SetInputAsHandled();
            var handle = target.Handle;
            _ = Enqueue(() => _service.Activate(handle));
        }
    }
}
