namespace QuickJS.Core
{
    public interface ITimer
    {
        float AnimationTime { get; }
        float DeltaTime { get; }
        float TimeScale { get; }
        object Yield(float advanceBy);
    }
}