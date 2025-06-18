namespace CurrencyConversion.Api.Options;

public class ScheduleOptions
{
    public const string SectionName = "Schedule";
    public string DailyTime { get; set; } = "02:00"; // HH:mm
}
