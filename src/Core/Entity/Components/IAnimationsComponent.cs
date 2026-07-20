namespace Core.Entity.Components
{
    using System.Threading.Tasks;

    public interface IAnimationsComponent
    {
        Task PlayAnimationAsync(string animation, float speedScale = 1f);
        void PlayAnimation(string animation);
        float GetClipSeconds(string animation);

        /// <summary>Whether the sprite's clip set contains the animation (activity poses degrade
        /// to Idle on a miss). Default false keeps sprite-less fakes honest.</summary>
        bool HasClip(string animation) => false;
    }
}
