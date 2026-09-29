namespace Core.Entity.Components
{
    using System.Threading.Tasks;
    using Godot;

    public interface IAnimationsComponent
    {
        AnimatedSprite2D Sprite { get; }
        Task PlayAnimationAsync(string animation, float speedScale = 1f);
        void PlayAnimation(string animation);
        float GetClipSeconds(string animation);

        /// <summary>Whether the animator shows something of its own for the animation (activity poses
        /// degrade to Idle on a miss, the fall holds the beat queue on a hit).</summary>
        bool HasClip(string animation);

        /// <summary>Dresses the entity in per-NPC art from the visual library; scale multiplies the
        /// scene's base sprite scale.</summary>
        void ApplyVisual(SpriteFrames frames, float scale = 1f);
    }
}
