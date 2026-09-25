namespace Core.Battle.DamageResolution
{
    using System.Collections.Generic;
    using Context;
    using Entity;

    /// <summary>
    /// A single absorption step between mitigation and health: eats part of the blow, writes its
    /// bite into the context (honest log numbers) and returns what is left for the layers below.
    /// A layer that guards health itself (the stage floor) reads the whole blow; one that eats damage
    /// on its way to health takes it out of <see cref="AbsorptionSplit.Absorbable"/> only.
    /// </summary>
    public interface IDamageAbsorptionLayer
    {
        AbsorptionSplit Absorb(IDamageContext context, IFightable target, AbsorptionSplit damage);
    }

    /// <summary>
    /// The ordered absorption layers every entity runs post-mitigation damage through
    /// (shield → barrier → stage guard); what survives the chain hits health. One shared
    /// sequence — a new defense mechanic is a new layer here, not an entity edit.
    /// The blow arrives as the context carries it: the chain splits it itself, so no caller can
    /// hand the layers a number that disagrees with the components.
    /// </summary>
    public class DamageResolutionChain(IReadOnlyList<IDamageAbsorptionLayer> layers)
    {
        public static DamageResolutionChain CreateDefault() =>
            new([new ShieldAbsorptionLayer(), new BarrierAbsorptionLayer(), new StageGuardLayer()]);

        public float Apply(IDamageContext context, IFightable target)
        {
            var damage = AbsorptionSplit.Of(context);
            foreach (var layer in layers)
            {
                if (damage.Total <= 0) return 0;
                damage = layer.Absorb(context, target, damage);
            }

            return damage.Total;
        }
    }
}
