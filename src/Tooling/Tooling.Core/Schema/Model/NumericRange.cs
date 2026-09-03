namespace Tooling.Schema.Model
{
    /// <summary>The numbers a field accepts, ends included.</summary>
    public readonly record struct NumericRange(double Min, double Max)
    {
        public double Max { get; init; } = SchemaGuard.Ordered(Min, Max);

        public bool Contains(double value) => value >= Min && value <= Max;
    }
}
