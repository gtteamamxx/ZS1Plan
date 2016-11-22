using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZS1Plan
{
    public class PagesManager
    {
        public enum ePagesType
        {
            Timetable,
            SettingsPage
        }
        public static int PagesCount => _stack.Count;

        private static Stack<object> _stack;

        public PagesManager()
        {
            _stack = new Stack<object>();
        }

        public static void AddPage(object Page, ePagesType typeOfPage)
        {
            _stack.Push(new Tuple<ePagesType, object>(typeOfPage, Page));
        }

        public static Tuple<ePagesType, object> GetPage()
        {
            return _stack.Count() == 0 ? null : _stack.Pop() as Tuple<ePagesType, object>;
        }
        public static Tuple<ePagesType, object> GetPageWithoutDelete()
        {
            return _stack.Count() == 0 ? null : _stack.Peek() as Tuple<ePagesType, object>;
        }

        public static void ClearPages()
        {
            _stack.Clear();
        }
    }
}
