namespace AppaRently.Infrastructure.Services;

internal static class ReservationDateRules
{
    public const int CheckInHour = 14;
    public const int CheckOutHour = 12;

    public static DateTime NormalizeCheckIn(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date.AddHours(CheckInHour), DateTimeKind.Utc);
    }

    public static DateTime NormalizeCheckOut(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date.AddHours(CheckOutHour), DateTimeKind.Utc);
    }

    public static (DateTime CheckIn, DateTime CheckOut) NormalizeStay(DateTime checkIn, DateTime checkOut)
    {
        return (NormalizeCheckIn(checkIn), NormalizeCheckOut(checkOut));
    }
}
