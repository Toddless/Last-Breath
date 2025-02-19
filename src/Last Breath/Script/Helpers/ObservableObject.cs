namespace Playground.Script.Helpers
{
    public class ObservableObject
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
