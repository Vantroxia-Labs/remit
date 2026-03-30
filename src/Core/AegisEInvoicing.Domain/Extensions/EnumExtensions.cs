namespace AegisEInvoicing.Domain.Extensions;

public static class EnumExtensions
{
    public static int ToInt<T>(this T enumValue) where T : Enum
    {
        return Convert.ToInt32(enumValue);
    }

    // Or more generic for different underlying types
    public static TResult ToNumeric<TResult>(this Enum enumValue)
        where TResult : struct, IConvertible
    {
        return (TResult)Convert.ChangeType(enumValue, typeof(TResult));
    }
}
