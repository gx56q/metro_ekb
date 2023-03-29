namespace tg_metro_ekb_bot;

public class Timetable
{
    private Dictionary<TimetableExtensions.DayType, List<TimeOnly>> TimetableDict { get; set; } = new();
    

    public Timetable(Dictionary<TimetableExtensions.DayType, List<string>> timetableDict)
    {
        TimetableDict = timetableDict
            .ToDictionary(kvp => kvp.Key, 
                kvp => kvp.Value.Select(TimeOnly.Parse).ToList());
        Sort();
    }
    
    public Timetable(Dictionary<TimetableExtensions.DayType, List<TimeOnly>> timetableDict)
    {
        TimetableDict = timetableDict;
    }

    public Timetable()
    {
        
    }

    public Timetable(IEnumerable<string> timetableDict, TimetableExtensions.DayType dateType)
    {
        TimetableDict.Add(dateType, timetableDict.Select(TimeOnly.Parse).ToList());
        Sort();
    }

    public void AddTime(TimeOnly time, TimetableExtensions.DayType dateType)
    {
        if (TimetableDict.ContainsKey(dateType))
            TimetableDict[dateType].Add(time);
        else
            TimetableDict.Add(dateType, new List<TimeOnly> {time});
        Sort();
    }
    
    public void AddTime(string time, TimetableExtensions.DayType dateType)
    {
        if (TimetableDict.ContainsKey(dateType))
            TimetableDict[dateType].Add(TimeOnly.Parse(time));
        else
            TimetableDict.Add(dateType, new List<TimeOnly> {TimeOnly.Parse(time)});
        Sort();
    }
    
    public void AddTime(IEnumerable<string> times, TimetableExtensions.DayType dateType)
    {
        if (TimetableDict.ContainsKey(dateType))
            TimetableDict[dateType].AddRange(times.Select(TimeOnly.Parse));
        else
            TimetableDict.Add(dateType, times.Select(TimeOnly.Parse).ToList());
        Sort();
    }
    
    public void AddTime(IEnumerable<TimeOnly> times, TimetableExtensions.DayType dateType)
    {
        if (TimetableDict.ContainsKey(dateType))
            TimetableDict[dateType].AddRange(times);
        else
            TimetableDict.Add(dateType, times.ToList());
        Sort();
    }

    public void AddTime(Dictionary<TimetableExtensions.DayType, List<string>> stationTimetable)
    {
        foreach (var (dateType, times) in stationTimetable)
        {
            AddTime(times, dateType);
        }
        Sort();
    }

    public void AddTime(Dictionary<TimetableExtensions.DayType, List<TimeOnly>> stationTimetable)
    {
        foreach (var (dateType, times) in stationTimetable)
        {
            AddTime(times, dateType);
        }
        Sort();
    }
    
    public Dictionary<TimetableExtensions.DayType, List<TimeOnly>> GetTimetableDict()
    {
        return TimetableDict;
    }
    
    public TimeOnly[] GetNextTrainTimes(TimeOnly now, TimetableExtensions.DayType dateType, int count = 5)
    {
        if (!TimetableDict.ContainsKey(dateType))
            return Array.Empty<TimeOnly>();
        var index = TimetableDict[dateType].FindIndex(time => time > now);
        if (index == -1)
            index = TimetableDict[dateType].FindLastIndex(time => time <= TimeOnly.Parse("23:59"));
        var nextTrains = TimetableDict[dateType].Skip(index).Take(count).ToArray();
        return nextTrains;
    }
    
    private void Sort()
    {
        foreach (var (_, times) in TimetableDict)
        {
            var timesAfterMidnight = times.Where(time => time <= TimeOnly.Parse("00:00")).ToList();
            times.RemoveAll(time => time <= TimeOnly.Parse("00:00"));
            times.AddRange(timesAfterMidnight);
        }
    }
    
    public TimeOnly GetLastTrainTime(TimetableExtensions.DayType dateType)
    {
        return TimetableDict.ContainsKey(dateType) ? TimetableDict[dateType].Last() : default;
    }
}