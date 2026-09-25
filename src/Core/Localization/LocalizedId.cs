namespace Core.Localization
{
    /// <summary>
    /// A description value that is an ID rather than a number — printed as whatever the catalog calls
    /// that id. Localized when the line is rendered and not when the value is put in place: values are
    /// composed where there is no locale yet (and, in a sandbox, no localization service at all), and a
    /// name resolved early would also survive a language change unchanged.
    /// </summary>
    public sealed record LocalizedId(string Id)
    {
        public override string ToString() => Localization.Localize(Id);
    }
}
