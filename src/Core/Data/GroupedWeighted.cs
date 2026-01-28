namespace Core.Data
{
    public record GroupedWeighted<T>(T Obj, float From, float To, float Weight, int Count) { }
}
