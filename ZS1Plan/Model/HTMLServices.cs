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

        /// <summary>
        /// Gets a source code of site
        /// </summary>
        /// <param name="url">url adress</param>
        /// <returns>source code of site</returns>
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

        /// <summary>
        /// Gets list of timetables from site
        /// </summary>
        /// <returns></returns>
        public static async Task GetData()
        {
            var parser = new HtmlParser();
            var document = await parser.ParseAsync(await GetSource("http://zs-1.pl/plan_nauczyciele/lista.html"));

            var listOfTables = document.QuerySelectorAll("ul");

            await FormatClasses(listOfTables[1].Children.Count(), listOfTables[0]);
            await FormatTeachers(listOfTables[0].Children.Count(), listOfTables[1]);

            InvokeAllTimeTableDownloaded();
        }

        /// <summary>
        /// Downloads info about timetables
        /// </summary>
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

                //iterates for 5 days 
                for (var d = 0; d < 5; d++)
                {
                    var day = new Day { Lessons = new List<Lesson>() };

                    for (var h = 0; h < listOfHours.Count(); h++)
                    {
                        var lesson = new Lesson();
                        // 2, because 0 is h'our number, 1 is a ring time (eg. 7:10 -> xxx )
                        var item = listOfHours[h].Children[2 + d];

                        //if there is not any lesson
                        if (item.InnerHtml == NoLessonString)
                        {
                            day.Lessons.Add(lesson);
                            continue;
                        }

                        var spanList = item.QuerySelectorAll("span");
                        var adressList = item.QuerySelectorAll("a");
                        var pList = spanList.Where(p => p.ClassName == "p").ToList();
                        var sList = spanList.Where(p => p.ClassName == "s").ToList();

                        lesson.lesson1Name = pList.First().TextContent;
                        lesson.lesson1Place = sList.First().TextContent;

                        //sometimes, a class dont have a number of group in p.ClassName.TextContent
                        //so we have to check it manually
                        if (!lesson.lesson1Name.Contains("1/2") && !lesson.lesson1Name.Contains("2/2"))
                        {
                            lesson.lesson1Name += (item.TextContent.Contains("1/2")
                                ? "-1/2"
                                : item.TextContent.Contains("2/2") ? "-2/2" : "");
                        }

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

                        //sometimes model was other from main model,so we have to
                        //check all configuration and put it in right place
                        if (sList.Count == 2)
                        {
                            string name = "";
                            string tag = "";
                            string taghref = "";

                            switch (pList.Count)
                            {
                                case 2:
                                    name = pList[1].TextContent;
                                    tag = adressList.Where(p => p.ClassName == "n").ToList()[1].TextContent;
                                    taghref = adressList[1].GetAttribute("href");
                                    break;
                                case 3:
                                    name = pList[1].TextContent;

                                    if (!name.Contains("1/2") || !name.Contains("2/2"))
                                    {
                                        int positionOfLesson2Name = item.TextContent.IndexOf(name, StringComparison.Ordinal);
                                        var substring = item.TextContent.Substring(positionOfLesson2Name, item.TextContent.Length - positionOfLesson2Name);
                                        name += substring.Contains("1/2") ? "-1/2" : substring.Contains("2/2") ? "-2/2" : "";
                                    }

                                    tag = pList[2].TextContent;
                                    break;
                                case 4:
                                    name = pList[2].TextContent;
                                    tag = pList[3].TextContent;
                                    break;
                                default:
                                    name = "error";
                                    break;
                            }

                            lesson.lesson2Name = name;
                            lesson.lesson2Place = sList[1].TextContent;
                            lesson.lesson2Tag = tag;
                            lesson.lesson2TagHref = taghref;
                        }
                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = Lesson.LessonType.Class;
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
                    var day = new Day() { Lessons = new List<Lesson>() };

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
                                int indexOfAdressElementInString = outerHtml.IndexOf(adressElement.OuterHtml,
                                    StringComparison.Ordinal);

                                fullString += adressElement.TextContent +
                                              outerHtml.Substring(indexOfAdressElementInString + adressElement.OuterHtml.Length, 5)
                                                  .Trim();
                            }

                            if (fullString[fullString.Length - 1] == ',')
                            {
                                fullString = fullString.Remove(fullString.Length - 1, 1);
                            }

                            lesson.lesson2Name = fullString;
                        }
                        else
                        {
                            lesson.lesson2Name = adressList.First().TextContent +
                                                 ((item.TextContent.Contains("1/2"))
                                                     ? "-1/2"
                                                     : item.TextContent.Contains("2/2") ? "-2/2" : "");
                        }

                        lesson.lesson1TagHref = adressList.First().GetAttribute("href");

                        day.Lessons.Add(lesson);
                    }

                    timetable.days.Add(day);
                }

                timetable.type = Lesson.LessonType.Teacher;
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
