namespace tg_metro_ekb_bot;

public static class TimetableExtensions
{
    public enum DayType
    {
        Weekday,
        Weekend,
    }
    
    public static DayType GetDayType(bool isWeekend)
    {
        return isWeekend ? DayType.Weekend : DayType.Weekday;
    }
    
    public enum WayType
    {
        Up,
        Down,
    }
}