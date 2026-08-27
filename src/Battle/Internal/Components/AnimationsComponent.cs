namespace Battle.Internal.Components
{
    using System;
    using System.Threading.Tasks;
    using Core.Entity.Components;
    using Godot;

    /// <summary>
    /// The clip animator, and the place that decides an NPC does not have one: a name the art draws
    /// as real motion (more than a single frame) plays as a clip, everything else — a lone facing
    /// frame, an unauthored blow, a cast pose — goes to the tween animator built on first need.
    /// </summary>
    [GlobalClass]
    public partial class AnimationsComponent : AnimationsComponentBase
    {
        private string _previousAnimation = TweenAnimationRules.ClipFor(TweenFacing.Down);
        private TweenAnimationsComponent? _tweens;

        /// <summary>The stand-in the body already carries, while it is still alive: a freed child
        /// leaves its wrapper behind, and suspending or measuring through it would touch dead native
        /// memory.</summary>
        private TweenAnimationsComponent? LiveTweens => IsInstanceValid(_tweens) ? _tweens : null;

        /// <summary>The stand-in for art without real clips; fully animated NPCs never build one.
        /// A method, not a property: Godot reads properties on its own (state serialization) and
        /// would spawn the node behind our back. Null when there is nothing to build it on — the
        /// stand-in hangs off THIS node and animates THAT sprite, so both have to be alive.</summary>
        private TweenAnimationsComponent? Tweens()
        {
            if (LiveTweens is { } live) return live;
            if (!IsInstanceValid(this) || Sprite is not { } sprite) return null;

            return _tweens = CreateTweens(sprite);
        }

        public override async Task PlayAnimationAsync(string animation, float speedScale = 1f)
        {
            if (Sprite is not { } sprite) return;
            if (sprite.SpriteFrames is not { } frames || !HasRealClip(animation))
            {
                if (Tweens() is { } stand) await stand.PlayAnimationAsync(animation, speedScale);
                return;
            }

            LiveTweens?.Suspend(); // an authored clip owns the sprite alone: no breathing, no mirror, no leftovers
            _previousAnimation = sprite.GetAnimation();
            sprite.SpeedScale = speedScale;
            try
            {
                sprite.Play(animation);
                if (!IsInsideTree()) return; // no tree, no timer and nobody left to show the clip to
                // Looping clips never emit animation_finished — awaiting it would hang the director
                // forever; they play for their computed length instead.
                if (frames.GetAnimationLoop(animation))
                    await ToSignal(GetTree().CreateTimer(TweenAnimationRules.Scaled(GetClipDuration(frames, animation), speedScale)), SceneTreeTimer.SignalName.Timeout);
                else
                    await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
                // The body may have been freed while the clip ran (a summon torn down after its
                // death beat): whatever is restored, it is restored on a sprite that still exists.
                Sprite?.Play(_previousAnimation);
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
            }
            finally
            {
                if (Sprite is { } alive) alive.SpeedScale = 1f;
            }
        }

        public override void PlayAnimation(string animation)
        {
            if (!HasRealClip(animation))
            {
                Tweens()?.PlayAnimation(animation);
                return;
            }

            LiveTweens?.Suspend();
            Sprite?.Play(animation);
        }

        public override bool HasClip(string animation) => HasRealClip(animation) || TweenAnimationRules.Handles(animation);

        public override float GetClipSeconds(string animation) =>
            Sprite?.SpriteFrames is { } frames && HasRealClip(animation)
                ? GetClipDuration(frames, animation)
                : TweenAnimationRules.Seconds(animation);

        public override void ApplyVisual(SpriteFrames frames, float scale = 1f)
        {
            base.ApplyVisual(frames, scale);
            LiveTweens?.ResetBaseline(); // the swap scales the sprite the tweens measure from
        }

        /// <summary>Whether the art draws this name as real motion; the rule itself is shared and pinned.
        /// The frame count is asked for only when the clip exists — the engine complains otherwise.</summary>
        private bool HasRealClip(string animation)
        {
            if (Sprite?.SpriteFrames is not { } frames) return false;

            bool authored = frames.HasAnimation(animation);
            return TweenAnimationRules.IsAuthoredMotion(authored, authored ? frames.GetFrameCount(animation) : 0);
        }

        private TweenAnimationsComponent CreateTweens(AnimatedSprite2D sprite)
        {
            var tweens = new TweenAnimationsComponent { Name = nameof(TweenAnimationsComponent) };
            tweens.UseSprite(sprite);
            AddChild(tweens);
            return tweens;
        }
    }
}
