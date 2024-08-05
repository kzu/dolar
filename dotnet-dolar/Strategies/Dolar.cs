using System;

namespace Devlooped;

public record Rate(DateOnly Date, double Buy, double Sell);

public interface IDolarStrategy
{
    Rate? GetRate(DateOnly date);
}

public static class DolarStrategy
{
    public static IDolarStrategy Create(DolarType type) =>
        type switch
        {
            DolarType.CCL => new DolarAmbito(type),
            DolarType.MEP => new DolarAmbito(type),
            DolarType.Turista => new DolarAmbito(type),
            DolarType.Blue => new DolarAmbito(type),
            DolarType.Divisa => new DolarBcra(true),
            DolarType.Billete => new DolarBcra(false),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
}