namespace Core.Modifiers.Conditions
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// "While the fight is still short" — the owner's own turns of the current battle, counted from one.
    /// Inverted, it is the drawn-out fight the same record draws the line for: "from that turn on".
    /// <para>The record names the turn the line stops at rather than the last turn it covers, the way a
    /// threshold over a vital names the share it arms below: a record of four covers turns one to three,
    /// and inverted it covers the fourth and everything after. The catalog id says which of the two the
    /// entry is, so the boundary is written down once and never counted again at the call site.</para>
    /// </summary>
    public sealed class BattleTurnCondition(int turn) : TurnCondition
    {
        protected override bool Evaluate(bool wasMet) => Turn < turn;
    }

    public class BattleTurnConditionFactory : IConditionFactory
    {
        /// <summary>The lowest boundary that draws a line at all: everyone with a turn is on his first, so
        /// a record of one is met by nobody, and inverted by everyone who is fighting.</summary>
        private const int LowestBoundary = 2;

        public string Type => ConditionTypes.BattleTurn;

        public OwnerCondition? Create(JObject json)
        {
            int turn = json.Value<int?>(ConditionFields.Turn) ?? 0;
            if (turn >= LowestBoundary) return new BattleTurnCondition(turn);

            Tracker.TrackError($"Skipping condition '{Type}': '{ConditionFields.Turn}' is {turn}, expected the turn the line stops at — {LowestBoundary} or later");
            return null;
        }
    }
}
