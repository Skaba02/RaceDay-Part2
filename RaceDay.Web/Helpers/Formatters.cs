using System.Globalization;

namespace RaceDay.Web.Helpers;

/// <summary>Small display helpers used by the views.</summary>
public static class Formatters
{
    /// <summary>1 becomes 1st, 2 becomes 2nd, 47 becomes 47th.</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13) return $"{number}th";

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    /// <summary>"47th out of 312" (or just "47th" when the total is unknown).</summary>
    public static string PositionText(int? position, int totalFinishers)
    {
        if (position == null) return "-";
        return totalFinishers > 0 ? $"{Ordinal(position.Value)} out of {totalFinishers}" : Ordinal(position.Value);
    }

    public static string Duration(TimeSpan? time) =>
        time == null ? "-" : time.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    public static string DateText(DateTime date) => date.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);

    public static string DateTimeText(DateTime date) => date.ToString("ddd, d MMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    public static string Distance(decimal? km) => km == null ? "-" : $"{km.Value:0.##} km";

    /// <summary>Bootstrap colour name for an enrolment status: green = Confirmed, amber = Pending.</summary>
    public static string StatusColour(string? status) => status switch
    {
        "Confirmed" => "success",
        "Pending" => "warning",
        _ => "secondary"
    };
}

/// <summary>The kinds of event the website offers in its drop-down lists.</summary>
public static class EventTypes
{
    public static readonly string[] All = { "Run", "Trail Run", "Walk", "Cycle", "Triathlon" };

    /// <summary>The list, plus the current value if it is not already in it (so editing never changes a type by accident).</summary>
    public static IEnumerable<string> WithCurrent(string? current)
    {
        if (!string.IsNullOrWhiteSpace(current) && !All.Contains(current, StringComparer.OrdinalIgnoreCase))
        {
            return All.Append(current);
        }

        return All;
    }
}
