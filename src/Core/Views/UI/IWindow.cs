namespace Core.Views.UI
{
    using Data;

    public interface IWindow : IInitializable, IRequireServices
    {

        /// <summary>False for dead-end screens Esc must not dismiss (game over).</summary>
        bool IsDismissable => true;

        /// <summary>True for windows that freeze player walking while open (dialogue, trade).
        /// The player polls <see cref="IUiElementsManager.HasMovementBlockingWindow"/> — a new
        /// blocking scenario is one flag here, not a new gate in the player.</summary>
        bool BlocksMovement => false;

        /// <summary>Closing means dying: the manager frees the node after this call.</summary>
        void Close();
    }
}
