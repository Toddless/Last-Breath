namespace Core.Views.UI
{
    using Data;

    public interface IWindow : IInitializable, IRequireServices
    {
        bool IsAlreadyVisible { get; }

        /// <summary>False for dead-end screens Esc must not dismiss (game over).</summary>
        bool IsDismissable => true;

        /// <summary>Closing means dying: the manager frees the node after this call.</summary>
        void Close();
    }
}
