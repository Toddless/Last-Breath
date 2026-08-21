namespace Core.Battle.Abilities
{
    /// <summary>
    /// What an augment lays when its own record does not say so — the records built by a factory, where
    /// the effect is named in the code that builds them. Asked by the card and by nothing else: WHAT the
    /// augment does is the factory's business, and this only tells the card which canon its figures are
    /// balanced in.
    /// <para>An augment names its effect in ONE of the two places, never in both: a record naming one in
    /// data is built from that word, and a second word beside it would be free to disagree with it.</para>
    /// </summary>
    public interface IAugmentLaidEffects
    {
        /// <summary>The effect the named augment lays from code, or empty — which is the answer for every
        /// record that names its effect in data and for every augment that lays none.</summary>
        string LaidEffectOf(string augmentId);
    }
}
