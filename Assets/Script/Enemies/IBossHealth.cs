/// <summary>Anything whose health can drive the boss bar.</summary>
public interface IBossHealth
{
    float HealthNormalized { get; }
    bool IsDead { get; }
}
