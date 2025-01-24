namespace App.Utilities;

public static class NumberFormatter
{
    public static string FormatNumber(int number)
    {
        if (number >= 1000000)
            return (number / 1000000D).ToString("0.#") + "M";
        if (number >= 1000)
            return (number / 1000D).ToString("0.#") + "K";
        return number.ToString();
    }
}
