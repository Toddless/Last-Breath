namespace Playground.Script.Helpers
{
    using System.ComponentModel;

    public class PropertyChangedWithValuesEventArgs<T> : PropertyChangedEventArgs
    {
        public T OldValue { get; set; }
        public T NewValue { get; set; }

        public PropertyChangedWithValuesEventArgs(string? propertyName, T oldValue, T newValue)
            : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
