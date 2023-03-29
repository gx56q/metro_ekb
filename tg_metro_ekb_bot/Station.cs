namespace tg_metro_ekb_bot;

public class Station
{
    private string _stationName = null!;
    private readonly Dictionary<TimetableExtensions.WayType, Timetable> _timetable = new();

    public Station() { }

    public Station(string stationName)
    {
        _stationName = stationName;
    }
    public Station(string stationName, Timetable stationTimetableUp, Timetable stationTimetableDown)
    {
        _stationName = stationName;
        _timetable[TimetableExtensions.WayType.Up] = stationTimetableUp;
        _timetable[TimetableExtensions.WayType.Down] = stationTimetableDown;
    }

    public void AddTime(TimeOnly time, TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        _timetable[wayType].AddTime(time, dateType);
    }
    
    public void AddTime(string time, TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        _timetable[wayType].AddTime(time, dateType);
    }

    
    public void AddTime(IEnumerable<string> times, TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        if (_timetable.ContainsKey(wayType))
            _timetable[wayType].AddTime(times, dateType);
        else
            _timetable.Add(wayType, new Timetable(times, dateType));
    }

    public void AddTime(IEnumerable<TimeOnly> times, TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        _timetable[wayType].AddTime(times, dateType);
    }

    public string GetStationName()
    {
        return _stationName;
    }
    
    public void SetStationName(string stationName)
    {
        _stationName = stationName;
    }
    
    public Dictionary<TimetableExtensions.WayType, Timetable> GetTimetable()
    {
        return _timetable;
    }

    public Timetable GetTimetableUp()
    {
        return _timetable[TimetableExtensions.WayType.Up];
    }
    
    public Timetable GetTimetableDown()
    {
        return _timetable[TimetableExtensions.WayType.Down];
    }

    public override string ToString()
    {
        return _stationName;
    }

    public void AddTime(Dictionary<TimetableExtensions.DayType, List<string>> stationTimetable, TimetableExtensions.WayType wayType)
    {
        _timetable[wayType].AddTime(stationTimetable);
    }

    public void AddTime(Dictionary<TimetableExtensions.DayType, List<TimeOnly>> stationTimetable, TimetableExtensions.WayType wayType)
    {
        _timetable[wayType].AddTime(stationTimetable);
    }
    
    public TimeOnly GetLastTrainTime(TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        return _timetable[wayType].GetLastTrainTime(dateType);
    }
}