using System.Runtime.InteropServices;
using isdayoff;
using isdayoff.Contract;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Task = System.Threading.Tasks.Task;

namespace tg_metro_ekb_bot
{
    internal static class TgBot
    {
        private static IConfiguration? _config;
        private static ITelegramBotClient? _bot;
        private static IsDayOff _isDayOff = new();

        private const int TimeNum = 7;
        private const string FinalUpStation = "Проспект космонавтов";
        private const string FinalDownStation = "Ботаническая";

        private static MetroTimetable _metroTimetable = new();
        private static string[]? _buttons;

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));

            await using (var sw = new StreamWriter("log.txt", true, System.Text.Encoding.Default))
            {
                await sw.WriteLineAsync(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            }
            var keyboard = СreateKeyboard(_buttons);
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message?.Text != null && _metroTimetable.ContainsStation(message.Text))
                {
                    var currentDateTime = DateTime.Now;
                    var reply = GetStationReply(message.Text, currentDateTime, TimeNum);
                    if (_bot != null)
                        await _bot.SendTextMessageAsync(
                            chatId: message.Chat,
                            text: reply,
                            replyMarkup: keyboard,
                            cancellationToken: cancellationToken
                        );
                    return;
                }
                switch (message?.Text?.ToLower())
                {
                    case "/start":
                        const string text = "Привет, я бот, который поможет тебе узнать расписание метро Екатеринбурга";
                        if (_bot != null)
                            await _bot.SendTextMessageAsync(
                                chatId: message.Chat,
                                text: text,
                                replyMarkup: keyboard,
                                cancellationToken: cancellationToken
                            );
                        return;
                    // case "расписание":
                    //     await Bot.SendTextMessageAsync(
                    //         chatId: message.Chat,
                    //         text: MetroTimetable.GetStationTimetable("Чкаловская", _schedule)[0],
                    //         replyMarkup: keyboard, cancellationToken: cancellationToken);
                    //     return;
                    default:
                        await botClient.SendTextMessageAsync(message!.Chat, "Привет, я бот, который поможет тебе узнать расписание метро Екатеринбурга", replyMarkup: keyboard, cancellationToken: cancellationToken);
                        // save update.Id into database
                        break;
                }
            }
        }
        
        private static string GetStationReply(string stationName, DateTime currentDateTime, int timeNum)
        {
            currentDateTime = DateTime.Parse("2023-03-23 00:00:00");
            // TODO: Решить проблему с расписанием в 00:00 
            var dayTypeNow = _isDayOff.CheckDayAsync(currentDateTime.AddHours(-1)).Result;
            var isWeekend = dayTypeNow == DayType.NotWorkingDay;
            var currentTime = TimeOnly.FromDateTime(currentDateTime);
            var dayType = TimetableExtensions.GetDayType(isWeekend);
            var scheduleUp = _metroTimetable.GetNextTrainTimes(stationName, currentTime, dayType,TimetableExtensions.WayType.Up, timeNum);
            var scheduleDown = _metroTimetable.GetNextTrainTimes(stationName, currentTime, dayType,TimetableExtensions.WayType.Down, timeNum);
            if (scheduleUp.Length < timeNum)
            {
                var toAdd = timeNum - scheduleUp.Length;
                var nextDay = currentDateTime.AddDays(1).Date;
                var nextDayType = _isDayOff.CheckDayAsync(nextDay).Result;
                var nextDayIsWeekend = nextDayType == DayType.NotWorkingDay;
                var NextDayScheduleUp = _metroTimetable.GetNextTrainTimes(stationName, TimeOnly.Parse("00:00"), TimetableExtensions.GetDayType(nextDayIsWeekend), TimetableExtensions.WayType.Up, toAdd);
                scheduleUp = scheduleUp.Concat(NextDayScheduleUp).ToArray();
            }
            if (scheduleDown.Length < timeNum)
            {
                var toAdd = timeNum - scheduleDown.Length;
                var nextDay = currentDateTime.AddDays(1).Date;
                var nextDayType = _isDayOff.CheckDayAsync(nextDay).Result;
                var nextDayIsWeekend = nextDayType == DayType.NotWorkingDay;
                var NextDayScheduleDown = _metroTimetable.GetNextTrainTimes(stationName, TimeOnly.Parse("00:00"), TimetableExtensions.GetDayType(nextDayIsWeekend), TimetableExtensions.WayType.Down, toAdd);
                scheduleDown = scheduleDown.Concat(NextDayScheduleDown).ToArray();
            }
            var nextTrainUp = "";
            var nextTrainDown = "";
            foreach (var time in scheduleUp)
            {
                Console.WriteLine(time);
            }
            if (scheduleUp.Length > 0)
            {
                nextTrainUp = BuildNextTrainString(scheduleUp, currentTime, FinalUpStation);
            }
            if (scheduleDown.Length > 0)
            {
                nextTrainDown = BuildNextTrainString(scheduleDown, currentTime, FinalDownStation);
            }
            var reply = $"{nextTrainUp}{nextTrainDown}";
            return reply;
        }

        private static string BuildNextTrainString(TimeOnly[] schedule, TimeOnly currentTime, string lastStation)
        {
            var nextTrainTime = schedule.First() - currentTime;
            var hours = nextTrainTime.Hours > 0 ? $"{nextTrainTime.Hours} ч. " : "";
            var minutes = nextTrainTime.Minutes > 0 ? $"{nextTrainTime.Minutes} мин." : "";
            var nextTrainText = nextTrainTime.TotalMinutes > 1 ? $"{hours}{minutes}" : "поезд прибывает";
            var returnString = $"Следующий поезд в сторону {lastStation} через: {nextTrainText}"
                           + "\n" + $"Следующие поезда в сторону {lastStation} в: "
                           + string.Join(" ", schedule) + "\n\n";
            return returnString;
        }

        private static ReplyKeyboardMarkup СreateKeyboard([ Optional ] IReadOnlyCollection<string>? keys)
        {
            if (keys == null || keys.Count == 0)
            {
                //TODO: Добавить кнопки из _buttons
                return new ReplyKeyboardMarkup("Расписание");
            }
            // ReSharper disable once RedundantNameQualifier
            return new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
            {
                new[] // first row
                {
                    new KeyboardButton("Чкаловская"),
                    new KeyboardButton("Площадь 1905 года"),
                    new KeyboardButton("Геологическая"),
                },
                new[] // second row
                {
                    new KeyboardButton("Ботаническая"),
                    new KeyboardButton("Динамо"),
                    new KeyboardButton("Уральская"),
                },
                new[] // third row
                {
                    new KeyboardButton("Машиностроителей"),
                    new KeyboardButton("Уралмаш"),
                    new KeyboardButton("Проспект космонавтов"),
                },
            });
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            return Task.CompletedTask;
        }

        private static void UpdateSchedule()
        {
            _metroTimetable.FillMetroTimetable();
            Console.WriteLine("Расписание получено в " + DateTime.Now);
            _buttons = _metroTimetable.GetStationNames();
        }


        private static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");            
            _config = builder.Build();
            if (_config["token"] == null)
            {
                Console.WriteLine("Токен не найден");
                return;
            }
            _bot = new TelegramBotClient(_config["token"] ?? throw new InvalidOperationException());
            var settings = IsDayOffSettings.Build
                .UseDefaultCountry(Country.Russia)
                .Create();
            _isDayOff = new IsDayOff(settings);
            Console.WriteLine("Запущен бот " + _bot.GetMeAsync().Result.FirstName);
            _metroTimetable = new MetroTimetable();
            _buttons = _metroTimetable.GetStationNames();
            var task1 = new Task(UpdateSchedule);
            task1.Start();
            var unused = new Timer(_ => UpdateSchedule(), null, TimeSpan.Zero, TimeSpan.FromHours(3));
            Console.WriteLine("Расписание обновляется каждые 3 часа");
            Console.WriteLine("Получение расписания...");
            task1.Wait();
            task1.Dispose();
            Console.WriteLine("Запуск бота...");
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions();
            _bot.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                    );
            Console.WriteLine("Бот запущен");
            Thread.Sleep(Timeout.Infinite);
            Console.ReadLine();
        }
    }
}