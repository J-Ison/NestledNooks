using System.Globalization;

namespace NestledNooks.Services;

public static class MoneyFormat
{
    public static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public static string Usd(decimal amount) => amount.ToString("C", UsCulture);

    public static string UsdWhole(decimal amount) => amount.ToString("C0", UsCulture);
}
