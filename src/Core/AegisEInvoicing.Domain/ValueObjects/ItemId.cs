using System.Security.Cryptography;
using System.Text;

namespace AegisEInvoicing.Domain.ValueObjects;

public record ItemId
{
    public string Segment1 { get; init; }
    public string Segment2 { get; init; }
    public string Segment3 { get; init; }
    public string Segment4 { get; init; }
    public string FullId { get; init; }

    public ItemId()
    {
        Segment1 = "ITM" + RandomDigits(2);
        Segment2 = RandomDigits(3);
        Segment3 = RandomDigits(9);
        Segment4 = RandomUpperLetters(5) + RandomDigits(1);

        FullId = $"{Segment1}-{Segment2}-{Segment3}-{Segment4}";
    }

    public override string ToString() => FullId;

    #region Random helpers (cryptographically strong)
    private static readonly char[] Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] Digits = "0123456789".ToCharArray();

    private static string RandomUpperLetters(int count)
    {
        if (count <= 0) return string.Empty;
        var sb = new StringBuilder(count);
        for (int i = 0; i < count; i++)
        {
            int idx = RandomInt(0, Letters.Length);
            sb.Append(Letters[idx]);
        }
        return sb.ToString();
    }

    private static string RandomDigits(int count)
    {
        if (count <= 0) return string.Empty;
        var sb = new StringBuilder(count);
        for (int i = 0; i < count; i++)
        {
            int idx = RandomInt(0, Digits.Length);
            sb.Append(Digits[idx]);
        }
        return sb.ToString();
    }

    private static int RandomInt(int minInclusive, int maxExclusive)
    {
        return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
    }
    #endregion
}