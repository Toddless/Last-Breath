namespace Battle.Internal.Components
{
    using System;
    using System.Threading.Tasks;
    using Core.Entity.Components;
    using Godot;

    /// <summary>
    /// The animator for art that is one static frame per facing: breathing, blows, the stagger and
    /// the fall are tweens on the child sprite. The body itself is never moved — in battle its
    /// position belongs to the director's approach tweens.
    /// </summary>
    [GlobalClass]
    public partial class TweenAnimationsComponent : AnimationsComponentBase
    {
        /// <summary>How far out of its rest position the sprite lunges when it strikes.</summary>
        private const float LungeDistance = 34f;

        private const float RecoilDistance = 18f;

        /// <summary>Share of a blow spent reaching out; the rest is the recovery back to rest.</summary>
        private const float StrikeShare = 0.35f;

        private const float SquashX = 1.12f;
        private const float SquashY = 0.88f;
        private const float CastPulse = 1.08f;
        private const float FallDegrees = 82f;
        private const float FallDrop = 12f;
        private const float StunDegrees = 9f;
        private const float HurtDegrees = 6f;
        private const int StunSwings = 4;

        /// <summary>Idle breathing: the slow squash and rise that keep a static frame alive.</summary>
        private const float BreathSeconds = 0.9f;

        private const float BreathSquash = 0.035f;
        private const float BreathRise = 3f;

        // Tween targets are node paths; the engine's own property names spell them without magic strings.
        private static readonly NodePath PositionPath = new(Node2D.PropertyName.Position);
        private static readonly NodePath ScalePath = new(Node2D.PropertyName.Scale);
        private static readonly NodePath RotationPath = new(Node2D.PropertyName.Rotation);

        private readonly TweenAnimationState _state = new();
        private Vector2 _restPosition;
        private Vector2 _restScale = Vector2.One;
        private float _restRotation;
        private Tween? _breath;
        private Tween? _motion;

        /// <summary>Which motion is current. A wait that outlived its own motion (a new one started,
        /// or a real clip took the sprite) must not settle the sprite under whoever owns it now.</summary>
        private int _generation;

        public override void _Ready() => ResetBaseline();

        /// <summary>
        /// Leaving the tree ends everything this node still owns: both tweens are killed and any
        /// wait still counting down is disowned. Without it a motion outlives its animator — the
        /// body of a summon is torn down one turn after its death beat, and a tween or a resumed
        /// wait that still wanted to settle the sprite would be writing through freed nodes.
        /// </summary>
        public override void _ExitTree()
        {
            _generation++;
            Stop(ref _motion);
            Stop(ref _breath);
        }

        /// <summary>
        /// Back in a tree — carried into the arena, or carried home after the battle — the figure
        /// starts breathing again, the symmetric half of what <see cref="_ExitTree"/> put out.
        /// Deferred because entering walks top-down: the sprite is a sibling of this animator's
        /// owner and is not in the tree yet when this fires.
        /// </summary>
        public override void _EnterTree() => CallDeferred(MethodName.ResumeBreathing);

        /// <summary>A body that already fell keeps lying, and a motion in flight settles itself when
        /// it ends — only an idle, standing figure is picked back up.</summary>
        private void ResumeBreathing()
        {
            if (!IsInsideTree() || _state.IsFallen || IsLive(_motion)) return;

            Settle();
        }

        public override void PlayAnimation(string animation) => StartMotion(_state.Next(animation), speedScale: 1f);

        /// <summary>
        /// Plays the motion and waits out its own length — a timer, not the tween's signal, so an
        /// interrupted motion can never hang the beat queue. The sprite returns to its rest transform
        /// afterwards, which is what the director means by "the previous clip is restored".
        /// </summary>
        public override async Task PlayAnimationAsync(string animation, float speedScale = 1f)
        {
            var step = _state.Next(animation);
            float seconds = StartMotion(step, speedScale);
            if (seconds <= 0f || !IsInsideTree()) return;

            int generation = _generation;
            try
            {
                await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
            }
            finally
            {
                // _ExitTree bumps the generation too, so a body torn down mid-motion settles nothing.
                if (generation == _generation && IsInsideTree() && step.Kind != TweenAnimationKind.Death) Settle();
            }
        }

        public override bool HasClip(string animation) => TweenAnimationRules.Handles(animation);

        public override float GetClipSeconds(string animation) => TweenAnimationRules.Seconds(animation);

        public override void ApplyVisual(SpriteFrames frames, float scale = 1f)
        {
            base.ApplyVisual(frames, scale);
            ResetBaseline();
        }

        /// <summary>
        /// Hands the sprite over to an authored clip: every tween stops, the transform and the mirror
        /// go back to rest and any wait still running is disowned. Without it the endless breathing
        /// would keep squashing a corpse playing its Dead clip, and a mirrored facing would flip it.
        /// </summary>
        public void Suspend()
        {
            _generation++;
            Stop(ref _motion);
            Stop(ref _breath);
            RestTransform();
            if (Sprite is { } sprite) sprite.FlipH = false;
        }

        /// <summary>Re-reads the sprite's rest transform: dressing an NPC in library art scales it.</summary>
        public void ResetBaseline()
        {
            if (Sprite is not { } sprite) return;

            _restPosition = sprite.Position;
            _restScale = sprite.Scale;
            _restRotation = sprite.Rotation;
        }

        /// <summary>Starts the step's motion and returns the scaled seconds it runs for; zero means
        /// there was nothing to play (a facing swap, or a corpse asked to fall twice).</summary>
        private float StartMotion(TweenAnimationStep step, float speedScale)
        {
            if (Sprite is not { } sprite || !IsInsideTree() || !sprite.IsInsideTree()) return 0f;

            var aimed = step.Kind is TweenAnimationKind.Attack or TweenAnimationKind.Cast ? AimAtTarget(step) : step;
            ShowFacing(aimed);

            if (aimed.Kind == TweenAnimationKind.Facing)
            {
                Settle();
                return 0f;
            }

            if (aimed.IsRepeat) return 0f;

            _generation++;
            Stop(ref _motion);
            Stop(ref _breath);
            float seconds = TweenAnimationRules.Scaled(aimed.Seconds, speedScale);
            // Bound to THIS node, not to the sprite: the motion ends with the animator that owns the
            // Settle callback and the rest transform, instead of outliving it on a sibling.
            var tween = CreateTween().SetParallel();
            BuildMotion(tween, sprite, aimed, seconds);
            if (aimed.Kind != TweenAnimationKind.Death) tween.Chain().TweenCallback(Callable.From(Settle));
            _motion = tween;
            return seconds;
        }

        /// <summary>
        /// In battle the fighter's body is the arena spot's child at local zero, so its offset is the
        /// approach the director already tweened toward the target: the horizontal side of that
        /// offset is the side the blow is aimed at. Blows are never played outside a battle.
        /// </summary>
        private TweenAnimationStep AimAtTarget(TweenAnimationStep step)
        {
            if (Sprite?.GetParent() is not Node2D body || body.Position == Vector2.Zero) return step;

            var offset = body.Position;
            // TODO: turn up/down as well once an arena puts spots above one another.
            if (Mathf.Abs(offset.X) <= Mathf.Abs(offset.Y)) return step;
            return _state.Turn(offset.X >= 0 ? TweenFacing.Right : TweenFacing.Left, step);
        }

        private void ShowFacing(TweenAnimationStep step)
        {
            if (Sprite is not { } sprite) return;

            sprite.FlipH = step.FlipHorizontally;
            if (sprite.SpriteFrames?.HasAnimation(step.Clip) != true || sprite.GetAnimation() == step.Clip) return;
            sprite.Play(step.Clip);
        }

        private void BuildMotion(Tween tween, Node2D sprite, TweenAnimationStep step, float seconds)
        {
            var direction = FacingDirection(step.Facing);
            switch (step.Kind)
            {
                case TweenAnimationKind.Attack:
                    BuildStrike(tween, sprite, direction, seconds);
                    break;
                case TweenAnimationKind.Hurt:
                    BuildRecoil(tween, sprite, direction, seconds);
                    break;
                case TweenAnimationKind.Death:
                    BuildFall(tween, sprite, step.Facing, seconds);
                    break;
                case TweenAnimationKind.Stun:
                    BuildWobble(tween, sprite, seconds);
                    break;
                default:
                    BuildCast(tween, sprite, direction, seconds);
                    break;
            }
        }

        /// <summary>The blow: a squashed lunge along the facing and a softer recovery.</summary>
        private void BuildStrike(Tween tween, Node2D sprite, Vector2 direction, float seconds)
        {
            float reach = seconds * StrikeShare;
            float recover = seconds - reach;

            tween.TweenProperty(sprite, PositionPath, _restPosition + (direction * LungeDistance), reach)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(sprite, ScalePath, Squashed(), reach)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.Chain().TweenProperty(sprite, PositionPath, _restPosition, recover)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(sprite, ScalePath, _restScale, recover)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        }

        /// <summary>Taking the hit: shoved back off the facing, then an elastic shiver home.</summary>
        private void BuildRecoil(Tween tween, Node2D sprite, Vector2 direction, float seconds)
        {
            float shove = seconds * StrikeShare;
            float settle = seconds - shove;

            tween.TweenProperty(sprite, PositionPath, _restPosition - (direction * RecoilDistance), shove)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(sprite, RotationPath, _restRotation + Mathf.DegToRad(HurtDegrees), shove)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.Chain().TweenProperty(sprite, PositionPath, _restPosition, settle)
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(sprite, RotationPath, _restRotation, settle)
                .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        }

        /// <summary>The fall: topples toward the side it faces and settles into the ground.</summary>
        private void BuildFall(Tween tween, Node2D sprite, TweenFacing facing, float seconds)
        {
            float side = facing == TweenFacing.Left ? -1f : 1f;

            tween.TweenProperty(sprite, RotationPath, _restRotation + Mathf.DegToRad(FallDegrees * side), seconds)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tween.TweenProperty(sprite, PositionPath, _restPosition + new Vector2(0f, FallDrop), seconds)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tween.TweenProperty(sprite, ScalePath, Squashed(), seconds)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        }

        /// <summary>The skipped turn: a finite sway that ends upright, never an endless loop.</summary>
        private void BuildWobble(Tween tween, Node2D sprite, float seconds)
        {
            float swing = seconds / StunSwings;
            float amplitude = Mathf.DegToRad(StunDegrees);

            for (int i = 0; i < StunSwings; i++)
            {
                float target = i == StunSwings - 1 ? _restRotation : _restRotation + (amplitude * (i % 2 == 0 ? 1f : -1f));
                tween.Chain().TweenProperty(sprite, RotationPath, target, swing)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            }
        }

        /// <summary>The cast pose: a backswing away from the target and a swell of the whole figure.</summary>
        private void BuildCast(Tween tween, Node2D sprite, Vector2 direction, float seconds)
        {
            float wind = seconds * StrikeShare;
            float release = seconds - wind;

            tween.TweenProperty(sprite, PositionPath, _restPosition - (direction * RecoilDistance), wind)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(sprite, ScalePath, _restScale * CastPulse, wind)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            tween.Chain().TweenProperty(sprite, PositionPath, _restPosition, release)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(sprite, ScalePath, _restScale, release)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        /// <summary>Back to the rest transform and back to breathing — the state every motion ends in.</summary>
        private void Settle()
        {
            RestTransform();
            StartBreathing();
        }

        private void RestTransform()
        {
            if (Sprite is not { } sprite) return;

            sprite.Position = _restPosition;
            sprite.Scale = _restScale;
            sprite.Rotation = _restRotation;
        }

        private void StartBreathing()
        {
            if (Sprite is not { } sprite || !IsInsideTree() || !sprite.IsInsideTree()) return;
            if (IsLive(_breath)) return;

            // Endless by design, so the binding matters most here: on this node it dies with the
            // animator, on the sprite it would keep squashing a body nobody owns any more.
            var breath = CreateTween().SetLoops().SetParallel();
            breath.TweenProperty(sprite, ScalePath, new Vector2(_restScale.X, _restScale.Y * (1f - BreathSquash)), BreathSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            breath.TweenProperty(sprite, PositionPath, _restPosition - new Vector2(0f, BreathRise), BreathSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            breath.Chain().TweenProperty(sprite, ScalePath, _restScale, BreathSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            breath.TweenProperty(sprite, PositionPath, _restPosition, BreathSeconds)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            _breath = breath;
        }

        private Vector2 Squashed() => new(_restScale.X * SquashX, _restScale.Y * SquashY);

        private static Vector2 FacingDirection(TweenFacing facing) => facing switch
        {
            TweenFacing.Up => Vector2.Up,
            TweenFacing.Left => Vector2.Left,
            TweenFacing.Right => Vector2.Right,
            _ => Vector2.Down,
        };

        private static void Stop(ref Tween? tween)
        {
            if (tween is { } running && IsInstanceValid(running) && running.IsValid()) running.Kill();
            tween = null;
        }

        /// <summary>A tween that is still playing: alive on both sides and not yet spent.</summary>
        private static bool IsLive(Tween? tween) =>
            tween is { } running && IsInstanceValid(running) && running.IsValid() && running.IsRunning();
    }
}
