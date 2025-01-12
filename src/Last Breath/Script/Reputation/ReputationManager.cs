namespace Playground.Script.Reputation
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class ReputationManager
    {
        private ElfsReputation? _elfsReputation = new(0);
        private DwarfsReputation? _dwarfsReputation = new(0);
        private HumansReputation? _hamansReputation = new(0);

        public ReputationManager(int elfstRep, int dwarfsRep, int humanReps)
        {
            _elfsReputation.ChangeReputationPoints(elfstRep);
            _dwarfsReputation.ChangeReputationPoints(dwarfsRep);
            _hamansReputation.ChangeReputationPoints(humanReps);
        }

        public ElfsReputation? ElfsReputation
        {
            get => _elfsReputation;
            private set => _elfsReputation = value;
        }

        public DwarfsReputation? DwarfsReputation
        {
            get => _dwarfsReputation;
            private set => _dwarfsReputation = value;
        }

        public HumansReputation? HumansReputation
        {
            get => _hamansReputation;
            private set => _hamansReputation = value;
        }

        public void ChangeReputationForMultipleFractions(List<Fractions> fraction, int amount)
        {
            fraction.ForEach(fraction => GetReputationByFraction(fraction)?.ChangeReputationPoints(amount));
        }

        public void ChangeReputationConflictFractions(Fractions decreaseReputationPoints, Fractions increaseReputationPoints, int amount)
        {
           GetReputationByFraction(decreaseReputationPoints)?.ChangeReputationPoints(-amount);
           GetReputationByFraction(increaseReputationPoints)?.ChangeReputationPoints(amount);
        }

        private BaseReputation? GetReputationByFraction(Fractions fractions) => fractions switch
        {
            Fractions.Elf => ElfsReputation,
            Fractions.Dwarf => DwarfsReputation,
            Fractions.Human => HumansReputation,
            _ => null
        };
    }
}
