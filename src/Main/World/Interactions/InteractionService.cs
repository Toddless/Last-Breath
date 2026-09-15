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
        private InteractionTarget? _sessionTarget;
        private InteractionWindow? _window;
        public PlayerInteractionController? Controller { get; set; }
        public InteractionTarget? Selected { get; private set; }
        public IReadOnlyList<InteractionAction> SelectedActions { get; private set; } = [];
        public InteractionTarget? SessionTarget => _sessionTarget;
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
            if (_sessionTarget == target) CancelSession();
            if (Selected == target) Select(null);
        }
        public void Select(InteractionTarget? target)
        {
            Selected = target;
            SelectedActions = target?.ReadActions() ?? [];
        }

        public bool CanDiscover() => ActorAvailable() && _window == null && !Ui.HasMovementBlockingWindow;
        private bool ActorAvailable() => Controller is { } controller && GodotObject.IsInstanceValid(controller) && controller.IsInsideTree()
            && provider.GetService<IPlayerAccessor>().Player is { IsAlive: true, IsFighting: false } player
            && ReferenceEquals(player, controller.Player)
            && Core.World.Spaces.SpatialAccess.CanReceiveInput(controller)
            && !Core.World.Spaces.SpatialAccess.HasTextFocus(controller)
            && !provider.GetService<ILocationTravelService>().IsTransitioning
            && !provider.GetService<ILoadScope>().IsLoading
            && provider.GetService<IUiContextService>().Current == UiContext.World;

        public bool CanReach(InteractionTarget target) => ActorAvailable()
            && !Ui.HasMovementBlockingWindowExcept(_window)
            && float.IsFinite(InteractionReach.DistanceSquared(Controller!.Player, target));

        public Task<InteractionResult> Activate(InteractionHandle handle)
        {
            if (!CanDiscover() || !TryResolve(handle, out var target) || !CanReach(target)) return Task.FromResult(InteractionResult.Unavailable);
            var actions = target.ReadActions();
            var enabled = actions.Where(x => x.Enabled).ToList();
            if (enabled.Count == 0) return Task.FromResult(InteractionResult.Unavailable);
            if (enabled.Count == 1 && !enabled[0].ExplicitChoice) return Execute(handle, enabled[0].Id);
            ShowWindow<InteractionMenuWindow>(target);
            return Task.FromResult(InteractionResult.Completed);
        }

        public async Task<InteractionResult> Execute(InteractionHandle handle, string actionId)
        {
            if (!TryResolve(handle, out var target) || !CanReach(target)
                || _sessionTarget != null && _sessionTarget != target) return InteractionResult.Unavailable;
            if (target.FindEnabledSource(actionId) is not { } source) return InteractionResult.Unavailable;
            // Release a menu before travel/dialogue enters its own UI/transition gate.
            CancelSession();
            return await source.Execute(actionId);
        }

        public InteractionResult OpenChest(ChestComponent chest)
        {
            if (!CanReach(chest.Target)) return InteractionResult.Unavailable;
            chest.Open();
            ShowWindow<ChestContentsWindow>(chest.Target);
            return InteractionResult.Completed;
        }

        private void ShowWindow<T>(InteractionTarget target) where T : InteractionWindow
        {
            CancelSession();
            if (Ui.OpenWindow(typeof(T)) is not T window) return;
            _sessionTarget = target;
            _window = window;
            window.Bind(this, target);
            Select(null);
        }

        public InteractionResult Transfer(InteractionHandle handle, string? slotId)
        {
            if (_window is not ChestContentsWindow || _sessionTarget is not { } target
                || target.Handle != handle || !CanReach(target) || target.GetParent() is not ChestComponent chest)
                return InteractionResult.Unavailable;
            if (slotId != null && !chest.Contents.Slots.Any(x => x.Id == slotId)) return InteractionResult.Unavailable;
            var transfer = chest.Transfer(slotId, () => _sessionTarget == target && CanReach(target));
            if (transfer.CapacityLimited) _window?.CapacityRefused();
            _window?.Refresh();
            return new(transfer.Accepted > 0, transfer.CapacityLimited ? "UI_Inventory_Full" : null);
        }

        public void ValidateSession()
        {
            if (_sessionTarget == null) return;
            if (_window == null || !GodotObject.IsInstanceValid(_window) || _window.IsQueuedForDeletion() || !CanReach(_sessionTarget))
                CancelSession();
        }
        public void WindowClosed(InteractionWindow window)
        {
            if (_window != window) return;
            _window = null;
            _sessionTarget = null;
            Controller?.CancelPending();
        }
        public void CancelSession()
        {
            var window = _window;
            Controller?.CancelPending();
            _window = null;
            _sessionTarget = null;
            if (window != null && GodotObject.IsInstanceValid(window) && !window.IsQueuedForDeletion()) window.Close();
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
