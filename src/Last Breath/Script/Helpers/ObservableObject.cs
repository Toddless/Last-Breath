namespace Playground.Script.Helpers
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            var oldValue = field;
            field = value;
            OnPropertyChanged(propertyName, oldValue, value);
            return true;
        }

        protected virtual void OnPropertyChanged<T>(string? propertyName, T oldValue, T newValue)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedWithValuesEventArgs<T>(propertyName, oldValue, newValue));
        }
    }
}
