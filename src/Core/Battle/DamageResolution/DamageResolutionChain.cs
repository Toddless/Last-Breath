namespace Core.Battle.DamageResolution
{
    using System.Collections.Generic;
    using Context;
    using Entity;

    /// <summary>
    /// A single absorption step between mitigation and health: eats part of the damage, writes its
    /// bite into the context (honest log numbers) and returns what is left for the layers below.
    /// </summary>
    public interface IDamageAbsorptionLayer
    {
        float Absorb(IDamageContext context, IFightable target, float remaining);
    }

    /// <summary>
    /// The ordered absorption layers every entity runs post-mitigation damage through
    /// (shield → barrier → stage guard); what survives the chain hits health. One shared
    /// sequence — a new defense mechanic is a new layer here, not an entity edit.
    /// </summary>
    public class DamageResolutionChain(IReadOnlyList<IDamageAbsorptionLayer> layers)
    {
        public static DamageResolutionChain CreateDefault() =>
            new([new ShieldAbsorptionLayer(), new BarrierAbsorptionLayer(), new StageGuardLayer()]);

        public float Apply(IDamageContext context, IFightable target, float damage)
        {
            float remaining = damage;
            foreach (var layer in layers)
            {
                if (remaining <= 0) return 0;
                remaining = layer.Absorb(context, target, remaining);
            }

            return remaining;
        }
    }
}
