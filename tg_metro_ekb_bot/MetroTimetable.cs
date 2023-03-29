namespace tg_metro_ekb_bot;

public class MetroTimetable
{
    private readonly Dictionary<string, Station> _stations = new();
    public MetroTimetable() { }
    
    public MetroTimetable(Dictionary<string, Station> stations)
    {
        _stations = stations;
    }
    
    public bool ContainsStation(string stationName)
    {
        return _stations.ContainsKey(stationName);
    }
    
    public void AddStation(Station station)
    {
        _stations.Add(station.GetStationName(), station);
    }
    
    
    public void AddTime(string stationName, IEnumerable<TimeOnly> times, TimetableExtensions.DayType dateType, TimetableExtensions.WayType wayType)
    {
        _stations[stationName].AddTime(times, dateType, wayType);
    }
    
    public Station GetStation(string stationName)
    {
        return _stations[stationName];
    }
    
    public Dictionary<string, Station> GetMetroTimetable()
    {
        return _stations;
    }
    
    public Dictionary<TimetableExtensions.WayType, Timetable> GetStationTimetable(string stationName)
    {
        return _stations[stationName].GetTimetable();
    }
    
    public string[]? GetStationNames()
    {
        return _stations.Keys.ToArray();
    }
    
    public void FillMetroTimetable()
    {
        var stations =  MetroTimetableExtensions.GetStations();
        _stations.Clear();
        foreach (var station in stations)
        {
            _stations.Add(station.GetStationName(), station);
        }
    }

    public TimeOnly[] GetNextTrainTimes(string stationName, TimeOnly currentTime, TimetableExtensions.DayType dateType,
        TimetableExtensions.WayType wayType, int count = 5)
    {
        var station = GetStation(stationName);
        var timetable = station.GetTimetable();
        if (!timetable.ContainsKey(wayType))
            return Array.Empty<TimeOnly>();
        var nextTrainTime = timetable[wayType].GetNextTrainTimes(currentTime, dateType, count);
        return nextTrainTime;
    }
    
    public TimeOnly GetLastTrainTime(string stationName, TimetableExtensions.DayType dateType,
        TimetableExtensions.WayType wayType)
    {
        var station = GetStation(stationName);
        var timetable = station.GetTimetable();
        if (!timetable.ContainsKey(wayType))
            return default;
        var lastTrainTime = timetable[wayType].GetLastTrainTime(dateType);
        return lastTrainTime;
    }
    
}