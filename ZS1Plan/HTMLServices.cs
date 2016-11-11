using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using AngleSharp.Parser.Html;
using System.Net.Http;
using AngleSharp.Dom;

namespace ZS1Plan
{
    public static class HtmlServices
    {
        private const string NoLessonString = @"&nbsp;";

        public delegate void TimeTableDownloaded(Timetable timetable, int lenght);
        public delegate void AllTimeTablesDownloaed();

        public static event TimeTableDownloaded OnTimeTableDownloaded;
        public static event AllTimeTablesDownloaed OnAllTimeTablesDownloaded;

        private static async Task<string> GetSource(string url)
        {
            var http = new HttpClient();
            var stream = await http.GetAsync(url);
            var data = await stream.Content.ReadAsByteArrayAsync();

            return @Encoding.UTF8.GetString(data);
        }

        public static void InvokeAllTimeTableDownloaded()
        {
            OnAllTimeTablesDownloaded?.Invoke();
        }

        public static async Task GetData()
        {
            var parser = new HtmlParser();
            var document = await parser.ParseAsync(await GetSource("http://zs-1.pl/plan_nauczyciele/lista.html"));

            var listOfTables = document.QuerySelectorAll("ul");

            await FormatClasses(listOfTables[1].Children.Count(), listOfTables[0]);
            await FormatTeachers(listOfTables[0].Children.Count(), listOfTables[1]);

            InvokeAllTimeTableDownloaded();
        }

        private static async Task FormatClasses(int listOfTeachersElementsNum, IElement listOfClasses)
        {
            for (var i = 0; i < listOfClasses.Children.Count(); i++)
            {
                var url = "http://zs-1.pl/plan_nauczyciele/" + listOfClasses.Children[i].FirstElementChild.GetAttribute("href");
                var document = await new HtmlParser().ParseAsync(await GetSource(url));

                var className = document.QuerySelector("span").TextContent;

                var listOfHours = document.QuerySelectorAll("table").Where(p => p.ClassName == "tabela").ToList().First()
                    .QuerySelectorAll("tr").Where(p => p.FirstElementChild.ClassName == "nr").ToList();

                var timetable = new Timetable
                {
                    name = className,
                    days = new List<Day>()
                };

                for (var d = 0; d < 5; d++)
                {
                    var day = new Day { Lessons = new List<Lesson>() };

                    for (var h = 0; h < listOfHours.Count(); h++)
                    {
                        var lesson = new Lesson();
                        // 2, because 0 is h'our number, 1 is a ring time (eg. 7:10 -> xxx )
                        var item = listOfHours[h].Children[2 + d];

                        if (item.InnerHtml == NoLessonString)
                        {
                            day.Lessons.Add(lesson);
                            continue;
                        }

                        var spanList = item.QuerySelectorAll("span");
                        lesson.lesson1Name = spanList.First(p => p.ClassName == "p").TextContent;

                        if (!lesson.lesson1Name.Contains("1/2") && !lesson.lesson1Name.Contains("2/2"))
                            lesson.lesson1Name += (item.TextContent.Contains("1/2") ? "-1/2" : item.TextContent.Contains("2/2") ? "-2/2" : "");

                        lesson.lesson1Place = spanList.First(p => p.ClassName == "s").TextContent;

                        var adressList = item.QuerySelectorAll("a");

                        if (adressList.Count() == 0)
                        {
                            lesson.lesson1Tag = spanList.Where(p => p.ClassName == "p").ToList()[1].TextContent;
                            lesson.lesson1TagHref = "";
                        }
                        else
                        {
                            lesson.lesson1Tag = adressList.First(p => p.ClassName == "n").TextContent;
                            lesson.lesson1TagHref = adressList.First().GetAttribute("href");
                        }

                        if (spanList.Count(p => p.ClassName == "p") == 2 && (spanList.Count(p => p.ClassName == "s") == 2))
                        {
                            lesson.lesson2Name = spanList.Where(p => p.ClassName == "p").ToList()[1].TextContent;
                            lesson.lesson2Place = spanList.Where(p => p.ClassName == "s").ToList()[1].TextContent;
                            lesson.lesson2Tag = adressList.Where(p => p.ClassName == "n").ToList()[1].TextContent;
                            lesson.lesson2TagHref = adressList[1].GetAttribute("href");
                        }
                        else if (spanList.Count(p => p.ClassName == "p") == 4 && (spanList.Count(p => p.ClassName == "s") == 2))
                        {
                            lesson.lesson2Name = spanList.Where(p => p.ClassName == "p").ToList()[2].TextContent;
                            lesson.lesson2Place = spanList.Where(p => p.ClassName == "s").ToList()[1].TextContent;
                            lesson.lesson2Tag = spanList.Where(p => p.ClassName == "p").ToList()[3].TextContent;
                            lesson.lesson2TagHref = "";
                        }
                        else if (spanList.Count(p => p.ClassName == "p") == 3 && (spanList.Count(p => p.ClassName == "s") == 2))
                        {
                            lesson.lesson2Name = spanList.Where(p => p.ClassName == "p").ToList()[1].TextContent;

                            if (!lesson.lesson2Name.Contains("1/2") || !lesson.lesson2Name.Contains("2/2"))
                            {
                                int positionOfLesson2Name = item.TextContent.IndexOf(lesson.lesson2Name, StringComparison.Ordinal);
                                var substring = item.TextContent.Substring(positionOfLesson2Name, item.TextContent.Length - positionOfLesson2Name);
                                lesson.lesson2Name += substring.Contains("1/2") ? "-1/2" : substring.Contains("2/2") ? "-2/2" : "";
                            }

                            lesson.lesson2Place = spanList.Where(p => p.ClassName == "s").ToList()[1].TextContent;
                            lesson.lesson2Tag = spanList.Where(p => p.ClassName == "p").ToList()[2].TextContent;
                            lesson.lesson2TagHref = "";
                        }

                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = 0;
                OnTimeTableDownloaded?.Invoke(timetable, listOfClasses.Children.Count() + listOfTeachersElementsNum);
            }
        }

        private static async Task FormatTeachers(int classesTimetablesNum, IElement listOfTeachers)
        {
            for (var i = 0; i < listOfTeachers.Children.Count(); i++)
            {
                var url = "http://zs-1.pl/plan_nauczyciele/" + listOfTeachers.Children[i].FirstElementChild.GetAttribute("href");
                var document = await new HtmlParser().ParseAsync(await GetSource(url));

                var className = document.QuerySelector("span").TextContent;

                var listOfHours = document.QuerySelectorAll("table").First(p => p.ClassName == "tabela")
                    .QuerySelectorAll("tr").Where(p => p.FirstElementChild.ClassName == "nr").ToList();

                var timetable = new Timetable()
                {
                    name = className,
                    days = new List<Day>()
                };

                for (var d = 0; d < 5; d++)
                {
                    var day = new Day() {Lessons = new List<Lesson>()};

                    for (var h = 0; h < listOfHours.Count(); h++)
                    {
                        var lesson = new Lesson();
                        // 2, because 0 is h'our number, 1 is a ring time (eg. 7:10 -> xxx )
                        var item = listOfHours[h].Children[2 + d];

                        if (item.InnerHtml == NoLessonString)
                        {
                            day.Lessons.Add(lesson);
                            continue;
                        }

                        var spanList = item.QuerySelectorAll("span");
                        lesson.lesson1Name = spanList.First(p => p.ClassName == "p").TextContent;
                        lesson.lesson1Place = spanList.First(p => p.ClassName == "s").TextContent;

                        var adressList = item.QuerySelectorAll("a");
                        if (adressList.Count() > 1)
                        {
                            var outerHtml = item.OuterHtml;
                            var fullString = string.Empty;

                            foreach (var adressElement in adressList)
                            {
                                int indexOfAdressElementInString = outerHtml.IndexOf(adressElement.OuterHtml, StringComparison.Ordinal);

                                fullString += adressElement.TextContent +
                                   outerHtml.Substring(indexOfAdressElementInString + adressElement.OuterHtml.Length, 5).Trim();
                            }

                            if (fullString[fullString.Length - 1] == ',')
                                fullString = fullString.Remove(fullString.Length - 1, 1);

                            lesson.lesson2Name = fullString;
                        }
                        else
                            lesson.lesson2Name = adressList.First().TextContent + ((item.TextContent.Contains("1/2")) ? "-1/2" : item.TextContent.Contains("2/2") ? "-2/2" : "");
                        lesson.lesson1TagHref = adressList.First().GetAttribute("href");

                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = 1;
                OnTimeTableDownloaded?.Invoke(timetable, classesTimetablesNum + listOfTeachers.Children.Count());
            }
        }

        public static bool UserHasInternetConnection()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            return (profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }
    }
}
