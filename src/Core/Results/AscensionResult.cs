namespace Core.Results
{
    using System.Collections.Generic;

    /// <summary>Gift lines are reported by InstanceId (a composite pool entry lands as several
    /// atomic lines on the item); empty when no gift was rolled.</summary>
    public record AscensionResult(bool Succeeded, IReadOnlyList<string> GiftedModifierIds);
}
