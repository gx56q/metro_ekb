using tg_metro_ekb_bot;

namespace test_bot;

public class Tests
{
    [SetUp]
    public void Setup()
    {	
    }
    
    [Test]
    public static void TestIfSorted()
    {
        var stations = MetroTimetableExtensions.GetStations();
        foreach (var station in stations)
        {
            var timetable = station.GetTimetable();
            foreach (var wayTimetable in timetable.Values)
            {
                foreach (var (_, times) in wayTimetable.GetTimetableDict())
                {
                    var sortedTimes = times.OrderBy(time => time).ToList();
                    var timesAfterMidnight = times.Where(time => time >= TimeOnly.Parse("00:00")).ToList();
                    sortedTimes.RemoveAll(time => time >= TimeOnly.Parse("00:00"));
                    sortedTimes.AddRange(timesAfterMidnight);
                    Assert.That(times, Is.EqualTo(sortedTimes));
                }
            }
        }
    }
}