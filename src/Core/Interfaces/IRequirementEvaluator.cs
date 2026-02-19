namespace Core.Interfaces
{
    using Data;
    using Enums;

    public interface IRequirementEvaluator
    {
        RequirementType Type { get; }

        bool Evaluate<T>(T requirement, IGameServiceProvider provider)
            where T : IRequirement;
        
    }
}
