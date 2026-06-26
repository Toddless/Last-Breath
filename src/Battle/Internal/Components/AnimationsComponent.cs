namespace Battle.Internal.Components
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Components;
    using Godot;

    [GlobalClass]
    public partial class AnimationsComponent : Node, IAnimationsComponent
    {
        [Export] private AnimatedSprite2D? _animatedSprite2D;
        private string _previousAnimation = "Idle_Down";

        public async Task PlayAnimationAsync(string animation)
        {
            if (_animatedSprite2D == null) return;
           _previousAnimation = _animatedSprite2D.GetAnimation();
            try
            {
                if (animation.StartsWith("Ability"))
                {
                    await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
                    GD.Print("Ability animation finished");
                }
                else
                {
                    _animatedSprite2D.Play(animation);
                    await ToSignal(_animatedSprite2D, "animation_finished");
                    _animatedSprite2D.Play(_previousAnimation);
                }
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
            }
        }

        public void PlayAnimation(string animation) => _animatedSprite2D?.Play(animation);
    }
}
