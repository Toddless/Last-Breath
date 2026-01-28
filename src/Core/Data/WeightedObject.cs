namespace Core.Data
{
    public record WeightedObject<T>(T Obj, float From, float To, float Weight);
}
