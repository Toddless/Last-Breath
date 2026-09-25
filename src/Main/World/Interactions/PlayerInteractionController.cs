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
        /// <summary>Whether discovery ran at the last refresh; a refresh that finds it running again drops every candidate's cached offer.</summary>
        private bool _discovering;
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
            if (area is InteractionTarget target) { _candidates.Add(target); target.InvalidateOffer(); _dirty = true; }
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

        /// <summary>Picks the target from the candidates' cached offers and their reach: an enabled one first, then the nearest, then the
        /// stable tie-break.</summary>
        private void RefreshSelection()
        {
            EvaluationCount++;
            if (!TrackDiscovery()) { _service.Select(null); return; }
            InteractionTarget? best = null;
            float bestDistance = float.PositiveInfinity;
            bool bestEnabled = false;
            foreach (var target in _candidates.ToArray())
            {
                if (!GodotObject.IsInstanceValid(target) || !target.IsInsideTree()) { _candidates.Remove(target); continue; }
                // Before reach: the reach check skips an unavailable target, so only this read drops its stale cached offer.
                var actions = target.CachedOffer;
                if (actions.Count == 0) continue;
                float distance = InteractionReach.DistanceSquared(Player, target);
                if (!float.IsFinite(distance)) continue;
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

        /// <summary>Whether discovery runs now; when it runs again after a pause, every candidate's cached offer is dropped.</summary>
        private bool TrackDiscovery()
        {
            bool discovering = _service.CanDiscover();
            if (discovering && !_discovering) InvalidateOffers();
            _discovering = discovering;
            return discovering;
        }

        /// <summary>Drops every candidate's cached offer, so the next refresh reads their sources again.</summary>
        private void InvalidateOffers()
        {
            foreach (var target in _candidates) target.InvalidateOffer();
        }

        public Task<InteractionResult> Enqueue(Func<Task<InteractionResult>> run)
        {
            if (_pending != null || _running || !IsInsideTree()) return Task.FromResult(InteractionResult.Dropped);
            var completion = new TaskCompletionSource<InteractionResult>();
            _pending = (run, completion);
            return completion.Task;
        }

        /// <summary>Runs the command, answers its request, then gives the player the feedback its result carries; whatever the command
        /// changed is read again from every candidate.</summary>
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
                InvalidateOffers();
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
