namespace Playground.Script.Helpers
{
    using Godot;

    public partial class ObservableCharacterBody2D : CharacterBody2D
    {
        protected bool SetProperty<T>(ref T field, T value)
        {
            if (Equals(field, value))
                return false;
            field = value;
            return true;
        }
    }
}
