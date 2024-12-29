namespace Playground.Script.Helpers
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Godot;

    public partial class ObservableCharacterBody2D : CharacterBody2D, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if(Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
