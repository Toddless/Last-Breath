namespace LastBreath.Components
{
    using System;
    using System.Threading.Tasks;
    using Core.Components;
    using Godot;

    [GlobalClass]
    public partial class AnimationsComponent : Node, IAnimationsComponent
    {
        private const float MissingClipSeconds = 0.5f;

        [Export] private AnimatedSprite2D? _animatedSprite2D;
        private string _previousAnimation = "Idle_Down";

        public async Task PlayAnimationAsync(string animation)
        {
            if (_animatedSprite2D == null) return;
            _previousAnimation = _animatedSprite2D.GetAnimation();
            try
            {
                if (_animatedSprite2D.SpriteFrames is { } sf && sf.HasAnimation(animation))
                {
                    _animatedSprite2D.Play(animation);
                    // Looping clips never emit animation_finished — awaiting it would hang the director
                    // forever; they play for their computed length instead.
                    if (sf.GetAnimationLoop(animation))
                        await ToSignal(GetTree().CreateTimer(GetClipDuration(sf, animation)), "timeout");
                    else
                        await ToSignal(_animatedSprite2D, "animation_finished");
                    _animatedSprite2D.Play(_previousAnimation);
                }
                else
                    await ToSignal(GetTree().CreateTimer(MissingClipSeconds), "timeout");
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
            }
        }

        public void PlayAnimation(string animation) => _animatedSprite2D?.Play(animation);

        private static float GetClipDuration(SpriteFrames frames, string animation)
        {
            float speed = (float)frames.GetAnimationSpeed(animation);
            if (speed <= 0) return MissingClipSeconds;

            float totalFrames = 0;
            for (int i = 0; i < frames.GetFrameCount(animation); i++)
                totalFrames += (float)frames.GetFrameDuration(animation, i);
            return totalFrames / speed;
        }
    }
}
