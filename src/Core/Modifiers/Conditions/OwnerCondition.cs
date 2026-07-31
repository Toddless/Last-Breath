namespace Core.Modifiers.Conditions
{
    using System;
    using Entity;
    using Interfaces;

    /// <summary>
    /// Shared lifecycle of every predicate over the owner's own state: one subscription set taken in
    /// <see cref="Attach"/> and released in <see cref="Detach"/>, a cached answer so <see cref="IsMet"/>
    /// never walks the owner, and the record-level inversion flag. A derived class only says WHAT it
    /// watches and HOW it reads the owner.
    /// </summary>
    public abstract class OwnerCondition : ICondition
    {
        private bool _state;
        private bool _met;

        protected IFightable? Owner { get; private set; }

        /// <summary>Whether the owner gives the predicate anything to read at all. A line over something
        /// the fighter does not have has no answer, and no answer is not met: the inversion flag turns a
        /// verdict around, it must not turn silence into one.</summary>
        protected virtual bool CanAnswer => true;

        /// <summary>Inverts the predicate ("not in the strength stance", "no debuff on me"). Set once when
        /// the condition is built from its record — inversion is a flag on the entry, not a wrapper class.</summary>
        public bool Negate { get; set; }

        /// <summary>An unattached condition — or one whose subject the owner has nothing to answer with —
        /// is met by nothing: a line that cannot be answered must not keep counting, inverted or not.</summary>
        public bool IsMet => _met;

        public event Action<bool>? StateChanged;

        /// <summary>The published answer: the raw verdict turned around by the record's flag, and nothing
        /// at all while there is no owner or nothing to read.</summary>
        private bool Answer() => Owner != null && CanAnswer && (Negate ^ _state);

        /// <summary>Reads the owner and answers the predicate. <paramref name="wasMet"/> is the raw
        /// (pre-inversion) answer being replaced, so band-based predicates can keep their gap; it is
        /// false while the condition attaches, which yields the plain unbanded answer.</summary>
        protected abstract bool Evaluate(bool wasMet);

        protected abstract void Subscribe(IFightable owner);

        protected abstract void Unsubscribe(IFightable owner);

        /// <summary>Re-reads the owner and publishes only real flips — a signal that does not move the
        /// predicate must not refresh the parameter. What is compared is the published answer, not the
        /// raw verdict: a subject appearing or vanishing moves the line without moving the verdict.</summary>
        protected void Reevaluate()
        {
            if (Owner == null) return;

            _state = Evaluate(_state);
            bool met = Answer();
            if (met == _met) return;

            _met = met;
            StateChanged?.Invoke(met);
        }

        public void Attach(IFightable owner)
        {
            if (Owner != null) Detach();

            Owner = owner;
            Subscribe(owner);
            // The answer must be right before the first signal arrives: a line landing on an already
            // wounded fighter counts from the moment it lands, not from the next hit.
            _state = Evaluate(wasMet: false);
            _met = Answer();
        }

        /// <summary>Releases the fighter and drops the cached answer. Silent by design: the consumer that
        /// detaches a condition is the one letting the line go, and it stops counting it in the same
        /// breath — a flip announced here would refresh a parameter for a line that no longer exists.</summary>
        public void Detach()
        {
            if (Owner == null) return;

            Unsubscribe(Owner);
            Owner = null;
            _state = false;
            _met = false;
        }

        /// <summary>A field-by-field clone with the live part wiped: no owner, no subscribers, no cached
        /// answer. Every predicate here is described by immutable parameters, so the clone needs no
        /// per-class copy code.</summary>
        public ICondition Copy()
        {
            var copy = (OwnerCondition)MemberwiseClone();
            copy.StateChanged = null;
            copy.Owner = null;
            copy._state = false;
            copy._met = false;
            return copy;
        }
    }
}
