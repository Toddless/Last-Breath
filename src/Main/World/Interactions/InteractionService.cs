namespace LastBreath.World.Interactions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Data;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;
    using Core.Views.UI;
    using Core.World.Interactions;
    using Core.World.Locations;
    using Godot;
    using Containers;
    using UI;

    public sealed class InteractionService(IGameServiceProvider provider)
    {
        private const string DuplicateTargetFormat =
            "Interaction target '{0}' is disabled: object ID '{1}' in location '{2}' is already registered by '{3}'";
        private readonly Dictionary<InteractionHandle, InteractionTarget> _targets = [];
        private IInteractionSession? _session;
        public PlayerInteractionController? Controller { get; set; }
        public InteractionTarget? Selected { get; private set; }
        public IReadOnlyList<InteractionAction> SelectedActions { get; private set; } = [];
        public InteractionTarget? SessionTarget => _session?.Target;
        private IUiElementsManager Ui => provider.GetService<IUiElementsManager>();

        /// <summary>Adds a live target; a second target with the same location and object ID is reported and stays unregistered.</summary>
        public bool TryRegister(InteractionTarget target)
        {
            if (FindRegistered(target.Handle) is not { } holder) return _targets.TryAdd(target.Handle, target);
            Tracker.TrackError(string.Format(DuplicateTargetFormat, target.GetPath(), target.Handle.ObjectId,
                target.Handle.LocationId, holder.GetPath()), this);
            return false;
        }
        /// <summary>Drops the target's own registration and its session/selection; a never-registered target leaves the registry intact.</summary>
        public void Unregister(InteractionTarget target)
        {
            if (_targets.TryGetValue(target.Handle, out var registered) && registered == target) _targets.Remove(target.Handle);
            if (_session?.Target == target) CancelSession(InteractionSessionEndCause.TargetLost);
            if (Selected == target) Select(null);
        }
        public void Select(InteractionTarget? target)
        {
            Selected = target;
            SelectedActions = target?.ReadActions() ?? [];
        }

        /// <summary>Target selection and E input; text focus suppresses them without closing an open session.</summary>
        public bool CanDiscover() => ActorAvailable() && !Core.World.Spaces.SpatialAccess.HasTextFocus(Controller!)
            && _session == null && !Ui.HasMovementBlockingWindow;
        /// <summary>Actor and space state shared by discovery and open sessions; discovery-only gates stay in <see cref="CanDiscover"/>.</summary>
        private bool ActorAvailable() => Controller is { } controller && GodotObject.IsInstanceValid(controller) && controller.IsInsideTree()
            && provider.GetService<IPlayerAccessor>().Player is { IsAlive: true, IsFighting: false } player
            && ReferenceEquals(player, controller.Player)
            && Core.World.Spaces.SpatialAccess.CanReceiveInput(controller)
            && !provider.GetService<ILocationTravelService>().IsTransitioning
            && !provider.GetService<ILoadScope>().IsLoading
            && provider.GetService<IUiContextService>().Current == UiContext.World;
        /// <summary>The actor is available and held by no blocking window other than the open session's own.</summary>
        private bool ActorFree() => ActorAvailable() && !Ui.HasMovementBlockingWindowExcept(_session?.Window);

        /// <summary>The actor is free to act and reaches the target.</summary>
        public bool CanReach(InteractionTarget target) => ActorFree() && InReach(target);

        public Task<InteractionResult> Activate(InteractionHandle handle)
        {
            if (!CanDiscover() || !TryResolve(handle, out var target) || !CanReach(target)) return Task.FromResult(InteractionResult.Unavailable);
            var actions = target.ReadActions();
            var enabled = actions.Where(x => x.Enabled).ToList();
            if (enabled.Count == 0) return Task.FromResult(InteractionResult.Unavailable);
            if (enabled.Count == 1 && !enabled[0].ExplicitChoice) return Execute(handle, enabled[0].Id);
            ShowWindow<InteractionMenuWindow>(target);
            return Task.FromResult(InteractionResult.Started);
        }

        public async Task<InteractionResult> Execute(InteractionHandle handle, string actionId)
        {
            if (!TryResolve(handle, out var target) || !CanReach(target)
                || _session != null && _session.Target != target) return InteractionResult.Unavailable;
            if (target.FindEnabledSource(actionId) is not { } source) return InteractionResult.Unavailable;
            // Release a menu before travel/dialogue enters its own UI/transition gate.
            ReplaceCurrent();
            return await source.Execute(actionId);
        }

        public InteractionResult OpenChest(ChestComponent chest)
        {
            if (!CanReach(chest.Target) || !chest.TryOpen()) return InteractionResult.Unavailable;
            ShowWindow<ChestContentsWindow>(chest.Target);
            return InteractionResult.Started;
        }

        /// <summary>Makes the session the open one: a different current session ends as replaced and the selection clears.</summary>
        public void Begin(IInteractionSession session)
        {
            if (_session != session) ReplaceCurrent();
            _session = session;
            Select(null);
        }

        private void ShowWindow<T>(InteractionTarget target) where T : InteractionWindow
        {
            // Ended before opening: an open window of the same type would be handed back instead of a fresh one.
            ReplaceCurrent();
            if (Ui.OpenWindow(typeof(T)) is not T window) return;
            window.Bind(this, target);
            Begin(window);
        }

        public InteractionResult Transfer(InteractionHandle handle, string? slotId)
        {
            if (_session is not ChestContentsWindow window || window.Target is not { } target
                || target.Handle != handle || !CanReach(target) || target.GetParent() is not ChestComponent chest)
                return InteractionResult.Unavailable;
            if (slotId != null && !chest.Contents.Slots.Any(x => x.Id == slotId)) return InteractionResult.Unavailable;
            var transfer = chest.Transfer(slotId, () => _session == window && CanReach(target));
            var open = _session as InteractionWindow;
            if (transfer.CapacityLimited) open?.CapacityRefused();
            open?.Refresh();
            return new(transfer.Accepted > 0, transfer.CapacityLimited ? "UI_Inventory_Full" : null);
        }

        /// <summary>Ends the open session with the cause of its first failed check; a session no longer open is forgotten.</summary>
        public void ValidateSession()
        {
            if (_session is not { } session) return;
            if (!session.IsOpen) { Forget(); return; }
            if (FindEndCause(session.Target) is { } cause) CancelSession(cause);
        }
        /// <summary>Forgets the open session when it ended on its own terms, dropping the controller's pending command.</summary>
        public void SessionClosed(IInteractionSession session)
        {
            if (_session == session) Forget();
        }
        /// <summary>Drops the controller's pending command and ends the open session, if any, for the cause.</summary>
        public void CancelSession(InteractionSessionEndCause cause = InteractionSessionEndCause.Cancelled)
        {
            var session = _session;
            Forget();
            session?.End(cause);
        }
        /// <summary>Ends the current session, if any, as replaced; with none, no pending command belongs to a session, so it stays queued.</summary>
        private void ReplaceCurrent()
        {
            if (_session != null) CancelSession(InteractionSessionEndCause.Replaced);
        }
        /// <summary>Why a session bound to the target must end, checked as actor, target lifecycle, then reach; null while it may stay open.</summary>
        private InteractionSessionEndCause? FindEndCause(InteractionTarget target)
        {
            if (!ActorFree()) return InteractionSessionEndCause.ActorUnavailable;
            if (!IsLive(target)) return InteractionSessionEndCause.TargetLost;
            return InReach(target) ? null : InteractionSessionEndCause.OutOfReach;
        }
        /// <summary>The target is valid, in the tree, registered under its current binding and available.</summary>
        private bool IsLive(InteractionTarget target) => TryResolve(target.Handle, out var registered) && registered == target
            && target.IsInsideTree() && target.IsAvailable;
        /// <summary>The player reaches one of the target's points in its own space, without an obstacle in between.</summary>
        private bool InReach(InteractionTarget target) => float.IsFinite(InteractionReach.DistanceSquared(Controller!.Player, target));
        /// <summary>Drops the open session without ending it and cancels the controller's pending command.</summary>
        private void Forget()
        {
            _session = null;
            Controller?.CancelPending();
        }
        private bool TryResolve(InteractionHandle handle, out InteractionTarget target)
        {
            if (_targets.TryGetValue(handle, out target!) && GodotObject.IsInstanceValid(target) && !target.IsQueuedForDeletion()) return true;
            return false;
        }
        /// <summary>Target registered under the same location and object ID, whatever its binding.</summary>
        private InteractionTarget? FindRegistered(InteractionHandle identity) => _targets
            .Where(x => x.Key.LocationId == identity.LocationId && x.Key.ObjectId == identity.ObjectId)
            .Select(x => x.Value)
            .FirstOrDefault();
    }

    public sealed class ExecuteInteractionHandler(InteractionService service) : IRequestHandler<ExecuteInteractionRequest, InteractionResult>
    {
        public Task<InteractionResult> HandleRequest(ExecuteInteractionRequest request) =>
            service.Controller?.Enqueue(() => service.Execute(request.Target, request.ActionId))
            ?? Task.FromResult(InteractionResult.Unavailable);
    }
    public sealed class ContainerTransferHandler(InteractionService service) : IRequestHandler<ContainerTransferRequest, InteractionResult>
    {
        public Task<InteractionResult> HandleRequest(ContainerTransferRequest request) =>
            service.Controller?.Enqueue(() => Task.FromResult(service.Transfer(request.Target, request.SlotId)))
            ?? Task.FromResult(InteractionResult.Unavailable);
    }
}
