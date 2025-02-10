namespace Playground.Script.Reputation
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class ReputationManager
    {
        private ElfsReputation? _elfReputation = new(0);
        private DwarfsReputation? _dwarfsReputation = new(0);
        private HumansReputation? _humansReputation = new(0);

        public ReputationManager(int elfRep, int dwarfsRep, int humanReps)
        {
            _elfReputation.ChangeReputationPoints(elfRep);
            _dwarfsReputation.ChangeReputationPoints(dwarfsRep);
            _humansReputation.ChangeReputationPoints(humanReps);
        }

        public ElfsReputation? ElfReputation
        {
            get => _elfReputation;
            private set => _elfReputation = value;
        }

        public DwarfsReputation? DwarfsReputation
        {
            get => _dwarfsReputation;
            private set => _dwarfsReputation = value;
        }

        public HumansReputation? HumansReputation
        {
            get => _humansReputation;
            private set => _humansReputation = value;
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
            Fractions.Elf => ElfReputation,
            Fractions.Dwarf => DwarfsReputation,
            Fractions.Human => HumansReputation,
            _ => null
        };
    }
}
