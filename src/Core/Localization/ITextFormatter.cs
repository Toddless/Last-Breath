namespace Core.Localization
{
    /// <summary>
    /// A registered "how does this object read as text" rule. The registry replaces the old
    /// switch-by-type: supporting a new domain object = one class + one DI registration.
    /// </summary>
    public interface ITextFormatter
    {
        bool CanFormat(object value);

        string Format(object value, TextFormat format);
    }
}
