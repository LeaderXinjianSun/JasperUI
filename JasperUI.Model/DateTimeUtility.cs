using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace JasperUI.Model
{
    public class DateTimeUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;

            public void FromDateTime(DateTime dateTime)
            {
                wYear = (ushort)dateTime.Year;
                wMonth = (ushort)dateTime.Month;
                wDayOfWeek = (ushort)dateTime.DayOfWeek;
                wDay = (ushort)dateTime.Day;
                wHour = (ushort)dateTime.Hour;
                wMinute = (ushort)dateTime.Minute;
                wSecond = (ushort)dateTime.Second;
                wMilliseconds = (ushort)dateTime.Millisecond;
            }

            public DateTime ToDateTime()
            {
                return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond);
            }
        }
        //设定，获取系统时间,SetSystemTime()默认设置的为UTC时间，比北京时间少了8个小时。
        [DllImport("Kernel32.dll")]
        public static extern bool SetSystemTime(ref SYSTEMTIME time);
        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SYSTEMTIME time);
        [DllImport("Kernel32.dll")]
        public static extern void GetSystemTime(ref SYSTEMTIME time);
        [DllImport("Kernel32.dll")]
        public static extern void GetLocalTime(ref SYSTEMTIME time);
    }

    /*示例：
      private void setLocalTime(string strDateTime)
        {
            DateTimeUtility.SYSTEMTIME st = new DateTimeUtility.SYSTEMTIME();
            DateTimeUtility.GetLocalTime(ref st);
            System.Diagnostics.Debug.WriteLine("GetLocalTime()");
            System.Diagnostics.Debug.WriteLine(st.ToDateTime().ToString("yyyy/MM/dd HH:mm:ss"));
            DateTimeUtility.GetSystemTime(ref st);
            System.Diagnostics.Debug.WriteLine("GetSystemTime()");
            System.Diagnostics.Debug.WriteLine(st.ToDateTime().ToString("yyyy/MM/dd HH:mm:ss"));

            DateTime dt = Convert.ToDateTime("2011/12/12 12:15:20");
            System.Diagnostics.Debug.WriteLine("test time:2011/12/12 12:15:20");
            st.FromDateTime(dt);
            DateTimeUtility.SetLocalTime(ref st);
            System.Diagnostics.Debug.WriteLine("SetLocalTime()");
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            DateTimeUtility.SetSystemTime(ref st);
            System.Diagnostics.Debug.WriteLine("SetSystemTime()");
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
        }
     
     */
}
