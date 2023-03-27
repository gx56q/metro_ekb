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
        var timetable = MetroTimetable.GetMetroTimetable();
        foreach (var table in timetable.Values.SelectMany(tables => tables.Values))
        {
            for (var i = 0; i < table.Length - 1; i++)
            {
                if (DateTime.Parse(table[i+1]).Hour == 0 && DateTime.Parse(table[i]).Hour < 24)
                    Assert.Pass();
                Assert.That(DateTime.Parse(table[i]), Is.LessThan(DateTime.Parse(table[i + 1])));
            }
        }
    }
}