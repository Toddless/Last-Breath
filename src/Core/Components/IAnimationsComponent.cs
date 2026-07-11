namespace Core.Components
{
    using System.Threading.Tasks;

    public interface IAnimationsComponent
    {
        Task PlayAnimationAsync(string animation, float speedScale = 1f);
        void PlayAnimation(string animation);
        float GetClipSeconds(string animation);
    }
}
