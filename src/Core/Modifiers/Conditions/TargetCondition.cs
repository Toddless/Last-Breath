namespace Core.Modifiers.Conditions
{
    using Entity;
    using Events;

    /// <summary>
    /// Shared lifecycle of every predicate about the fighter the owner is acting upon rather than about the
    /// owner himself — "the target is under control", "the target is under a third of his health".
    /// <para>Such a predicate has no subject until a hit is being resolved: who the target is exists only
    /// for the length of one attack. The attack context is the object that names both sides, and it reaches
    /// the owner's own combat bus twice — once as the attack is joined, once as it is spent — so the family
    /// takes its subject from there and answers from the fighter it was pointed at. Between attacks there is
    /// no target and therefore no answer at all: the predicate is met by nothing, inverted or not, exactly
    /// like an unattached one. That is the whole of the guard that keeps a line written about a target from
    /// quietly applying everywhere else — a heal, an effect ticking between turns, a parameter resolved out
    /// of combat.</para>
    /// <para>The verdict is taken once, when the attack is joined, and stands until it is spent — which is
    /// the whole of the hit, damage included, so what shapes the numbers of one attack cannot read the
    /// target twice and get two answers. A series re-arms per attack, so a target driven under a threshold
    /// by one hit is answered afresh on the next.</para>
    /// </summary>
    public abstract class TargetCondition : OwnerCondition
    {
        /// <summary>The fighter the owner is hitting right now, or null while no attack of his is in flight.</summary>
        protected IFightable? Target { get; private set; }

        /// <summary>Between attacks there is nobody to read — see the class summary.</summary>
        protected override bool CanAnswer => Target != null;

        protected override void Subscribe(IFightable owner)
        {
            owner.CombatEvents.Subscribe<BeforeAttackEvent>(OnAttackJoined);
            owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAttackSpent);
        }

        protected override void Unsubscribe(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<BeforeAttackEvent>(OnAttackJoined);
            owner.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAttackSpent);
        }

        /// <summary>The subject is live state and goes wherever the base drops its own: a released or
        /// cloned predicate must not answer from the last fighter its owner swung at.</summary>
        protected override void Forget() => Target = null;

        /// <summary>Both attack events are published on the attacker's own bus, so what arrives here is an
        /// attack of the owner's and its target is the one he is acting upon.</summary>
        private void OnAttackJoined(BeforeAttackEvent joined) => Aim(joined.Context.Target);

        private void OnAttackSpent(AfterAttackEvent spent) => Aim(null);

        private void Aim(IFightable? target)
        {
            Target = target;
            Reevaluate();
        }
    }
}
