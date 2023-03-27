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
        private const string WorkUpStationHeader = $"Рабочие дни в сторону станции \"{FinalUpStation}\"";
        private const string WeekendUpStationHeader = $"Выходные дни в сторону станции \"{FinalUpStation}\"";
        private const string WorkDownStationHeader = $"Рабочие дни в сторону станции \"{FinalDownStation}\"";
        private const string WeekendDownStationHeader = $"Выходные дни в сторону станции \"{FinalDownStation}\"";
        
        private static Dictionary<string, Dictionary<string, string[]>> _schedule = new();
        private static List<string> _buttons = new();

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
                if (message?.Text != null && _schedule.ContainsKey(message.Text))
                {
                    var currentTime = DateTime.Now;
                    var dayType = _isDayOff.CheckDayAsync(currentTime.AddMinutes(-45), cancellationToken).Result;
                    var isWeekend = dayType == DayType.NotWorkingDay;
                    //var isWeekend = currentTime.AddMinutes(-45).DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    string[]? scheduleUp;
                    string[]? scheduleDown;
                    if (isWeekend)
                    { 
                        scheduleUp = _schedule[message.Text].FirstOrDefault(x => x.Key.Contains(WeekendUpStationHeader)).Value;
                        scheduleDown = _schedule[message.Text].FirstOrDefault(x => x.Key.Contains(WeekendDownStationHeader)).Value;
                    }
                    else
                    {
                        scheduleUp = _schedule[message.Text].FirstOrDefault(x => x.Key.Contains(WorkUpStationHeader)).Value;
                        scheduleDown = _schedule[message.Text]
                            .FirstOrDefault(x => x.Key.Contains(WorkDownStationHeader)).Value;
                    }
                    //TODO: Вынесть в отдельный метод
                    var scheduleUp1 = !message.Text.Contains(FinalUpStation) ? scheduleUp.Where(x => DateTime.Parse(x) >= currentTime).ToList() : null;
                    var scheduleDown1 = !message.Text.Contains(FinalDownStation) ? scheduleDown.Where(x => DateTime.Parse(x) >= currentTime).ToList() : null;
                    scheduleUp1?.AddRange(scheduleUp.Where(x => x.StartsWith("00:")).ToList());
                    scheduleDown1?.AddRange(scheduleDown.Where(x => x.StartsWith("00:")).ToList());
                    var reply = GetStationReply(scheduleUp1, scheduleDown1, currentTime, TimeNum);
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
        
        private static string GetStationReply(IReadOnlyList<string>? scheduleUp,IReadOnlyList<string>? scheduleDown, DateTime currentTime, int timeNum)
        {
            var nextTrainUp = "";
            var nextTrainDown = "";
            if (scheduleUp != null)
            {
                var nextTrainUpTime = DateTime.Parse(scheduleUp[0]) - currentTime;
                if (nextTrainUpTime.TotalMinutes < 0)
                {
                    // get time till midnight and add it to DateTime.Parse(scheduleDown[0])
                    nextTrainUpTime = DateTime.Parse(scheduleUp[0]) - currentTime + new TimeSpan(24, 0, 0);
                }
                var hours = nextTrainUpTime.Hours > 0 ? $"{nextTrainUpTime.Hours} ч. " : "";
                var minutes = nextTrainUpTime.Minutes > 0 ? $"{nextTrainUpTime.Minutes} мин." : "";
                var nextTrainText = nextTrainUpTime.TotalMinutes > 1 ?  $"{hours}{minutes}" : "поезд прибывает";
                    nextTrainUp += $"Следующий поезд в сторону {FinalUpStation} через: {nextTrainText}"
                                   + "\n" + $"Следующие поезда в сторону {FinalUpStation} в: "
                                   + string.Join(" ", scheduleUp.Take(timeNum)) + "\n\n";
            }
            if (scheduleDown != null)
            {
                var nextTrainDownTime = DateTime.Parse(scheduleDown[0]) - currentTime;

                if (nextTrainDownTime.TotalMinutes < 0)
                {
                    // get time till midnight and add it to DateTime.Parse(scheduleDown[0])
                    var midnight = DateTime.Parse("23:59") - currentTime + TimeSpan.FromMinutes(1);
                    nextTrainDownTime = midnight + nextTrainDownTime;
                }
                var hours = nextTrainDownTime.Hours > 0 ? $"{nextTrainDownTime.Hours} ч. " : "";
                var minutes = nextTrainDownTime.Minutes > 0 ? $"{nextTrainDownTime.Minutes} мин." : "";
                var nextTrainText = nextTrainDownTime.TotalMinutes > 1 ?  $"{hours}{minutes}" : "поезд прибывает";
                nextTrainDown += $"Следующий поезд в сторону {FinalDownStation} через: {nextTrainText}"
                               + "\n" + $"Следующие поезда в сторону {FinalDownStation} в: "
                               + string.Join(" ", scheduleDown.Take(timeNum)) + "\n\n";
            }

            var reply = $"{nextTrainUp}{nextTrainDown}";
            return reply;
        }

        private static ReplyKeyboardMarkup СreateKeyboard([ Optional ] List<string> keys)
        {
            if (keys.Count == 0)
            {
                //TODO: Добавить кнопки из keys
                return new ReplyKeyboardMarkup("Расписание");
            }
            else
            {
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
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
            return Task.CompletedTask;
        }

        private static void UpdateSchedule()
        {
            _schedule = MetroTimetable.GetMetroTimetable();
            // foreach (var key in _schedule)
            // {
            //     foreach (var key1 in key.Value)
            //     {
            //         Console.WriteLine(key1.Key);
            //         foreach (var s in key1.Value)
            //         {
            //             Console.WriteLine(s);
            //         }
            //     }
            // }
            Console.WriteLine("Расписание получено в " + DateTime.Now);
            _buttons = new List<string>(_schedule.Keys);
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
            var task1 = new Task(UpdateSchedule);
            task1.Start();
            var unused = new Timer(_ => UpdateSchedule(), null, TimeSpan.Zero, TimeSpan.FromHours(2));
            Console.WriteLine("Расписание обновляется каждые 2 часа");
            Console.WriteLine("Получение расписания...");
            task1.Wait();
            Console.WriteLine("Запуск бота...");
            task1.Dispose();
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                // ReSharper disable once RedundantEmptyObjectOrCollectionInitializer
                AllowedUpdates = { }, // receive all update types
            };
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