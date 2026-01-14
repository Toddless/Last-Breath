namespace Core.Interfaces.Components
{
    using System.Threading.Tasks;

    public interface IAnimationsComponent
    {
        Task PlayAnimationAsync(string animation);
        void PlayAnimation(string animation);
    }
}
