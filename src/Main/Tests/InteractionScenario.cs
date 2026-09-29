namespace LastBreath.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Inventory;
    using Core.Views.UI;
    using Godot;
    using UI;
    using World.Interactions;
    using World.Interactions.UI;

    /// <summary>Run shared by the interaction scenarios: the scenario drives a loaded Main, every passed check is counted and the result
    /// line carries the scenario's tag; afterwards the loaded Main is freed and the engine quits clean.</summary>
    public abstract partial class InteractionScenario : Node
    {
        private int _checks;

        /// <summary>Prefix of the pass and fail lines the scenario prints.</summary>
        protected abstract string ResultTag { get; }

        /// <summary>Physics ticks <see cref="Ticks()"/> waits for the world to settle.</summary>
        protected virtual int SettleTicks => 12;

        protected static IGameServiceProvider Provider => Services.GameServiceProvider.Instance;

        /// <summary>Lets the freed Main leave, hands the bag slots to a live container and collects before quitting.</summary>
        private async Task Quit()
        {
            await Ticks(2);
            var slots = new GridContainer();
            AddChild(slots);
            Provider.GetService<ISlotLender>().AttachSlots(slots);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Ticks(2);
            GetTree().Quit();
        }

        /// <summary>The scenario; its first failed check ends it.</summary>
        protected abstract Task Run();

        /// <summary>Counts a passed check; a failed one ends the scenario with its message.</summary>
        protected void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
            _checks++;
        }

        /// <summary>Loads Main without its test items under the scenario and lets it settle.</summary>
        protected async Task<Main> LoadMain()
        {
            var main = Main.Initialize().Instantiate<Main>();
            main.Set("_addTestItems", false);
            AddChild(main);
            await Ticks();
            return main;
        }

        /// <summary>Moves the player so that its interaction origin stands at the point.</summary>
        protected static void PlaceBody(Node2D player, Vector2 bodyCenter) =>
            player.GlobalPosition += bodyCenter - ((IInteractionActor)player).InteractionOrigin;

        /// <summary>Waits <see cref="SettleTicks"/> physics ticks, then one frame.</summary>
        protected Task Ticks() => Ticks(SettleTicks);

        /// <summary>Waits the physics ticks, then one frame.</summary>
        protected async Task Ticks(int count)
        {
            for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        /// <summary>Presses the key by its physical code, as the interaction keys are bound, through the root window, lets the world settle,
        /// then releases it.</summary>
        protected async Task Press(Key key)
        {
            GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = key, Pressed = true }, true);
            await Ticks();
            GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = key, Pressed = false }, true);
        }

        /// <summary>Presses and releases the key by its key code, as the menu navigation keys are bound, through the root window, then lets
        /// the world settle.</summary>
        protected async Task Tap(Key key)
        {
            GetTree().Root.PushInput(new InputEventKey { Keycode = key, Pressed = true }, true);
            await Ticks(1);
            GetTree().Root.PushInput(new InputEventKey { Keycode = key, Pressed = false }, true);
            await Ticks();
        }

        /// <summary>The root when it is a <typeparamref name="T"/>, else the first node below it that is, in tree order; null when none is.</summary>
        protected static T? FindFirst<T>(Node root) where T : class => FindAll<T>(root).FirstOrDefault();

        /// <summary>The root and every node below it that is a <typeparamref name="T"/>, in tree order.</summary>
        protected static IEnumerable<T> FindAll<T>(Node root) where T : class
        {
            if (root is T match) yield return match;
            for (int i = 0; i < root.GetChildCount(); i++)
                foreach (var found in FindAll<T>(root.GetChild(i))) yield return found;
        }

        /// <summary>Text the interaction hint shows now.</summary>
        protected string HintText() => FindFirst<Label>(FindFirst<InteractionHint>(this)!)!.Text;

        /// <summary>Texts of the notices shown now, in tree order; a notice on its way out is left out.</summary>
        protected List<string> NoticeTexts() => FindAll<NotificationPopup>(this).Where(x => !x.IsQueuedForDeletion())
            .Select(x => FindFirst<Label>(x)?.Text ?? string.Empty).ToList();

        /// <summary>Frees every overlay, the shown notices and the queued ones with them, so the notices read next are those given afterwards.</summary>
        protected async Task ClearNotices()
        {
            FindFirst<ILayerManager>(this)!.CloseOverlays();
            await Ticks(1);
        }

        /// <summary>Puts a counter of its starts in front of the player's stream, which has to be set: a player without one is silent, yet its
        /// starts would still be counted. The stream plays as before.</summary>
        protected PlayCounter CountPlays(AudioStreamPlayer player)
        {
            Check(player.Stream != null, $"The sound player {player.Name} has a sound to play.");
            var counter = new PlayCounter { Played = player.Stream };
            player.Stream = counter;
            return counter;
        }

        public override async void _Ready()
        {
            try
            {
                await Run();
                GD.Print($"{ResultTag}_PASS: {_checks} checks");
            }
            catch (Exception error)
            {
                GD.PushError($"{ResultTag}_FAIL: {error}");
                GetTree().Quit(1);
                return;
            }
            finally
            {
                foreach (var main in GetChildren().OfType<Main>()) main.QueueFree();
            }
            await Quit();
        }

        /// <summary>Stream that plays another one and counts the starts: a player asks its stream for a new playback on every start.</summary>
        protected sealed partial class PlayCounter : AudioStream
        {
            /// <summary>Stream every start plays.</summary>
            public AudioStream? Played { get; set; }

            public int Plays { get; private set; }

            public override AudioStreamPlayback? _InstantiatePlayback()
            {
                Plays++;
                return Played?.InstantiatePlayback();
            }
        }
    }
}
