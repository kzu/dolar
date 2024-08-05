using Devlooped;

namespace Tests;

public class Strategies
{
    [Theory]
    [InlineData(DolarType.CCL, "2024-01-02", 997.45, 997.45)]
    [InlineData(DolarType.MEP, "2024-03-01", 1053.46, 1053.46)]
    [InlineData(DolarType.Turista, "2024-06-06", 1468.80, 1468.80)]
    [InlineData(DolarType.Blue, "2023-08-10", 597, 602)]
    [InlineData(DolarType.Divisa, "2024-05-15", 882.5, 885.5)]
    [InlineData(DolarType.Billete, "2024-05-14", 864, 904)]
    public void Dolar(DolarType type, string date, double buy, double sell)
    {
        var strategy = DolarStrategy.Create(type, new Progress<string>());
        var rate = strategy.GetRate(DateOnly.ParseExact(date, "yyyy-MM-dd"));
        Assert.NotNull(rate);
        Assert.Equal(buy, rate!.Buy);
        Assert.Equal(sell, rate!.Sell);
    }
}