using System;

namespace AlarmClockApp.Models
{
    internal class DatePreset
    {
        private string _hour;
        private string _minute;
        private DateTime _date;

        public DatePreset(string hour, string minute, DateTime date)
        {
            _hour = hour;
            _minute = minute;
            _date = date;
        }

        public string Hour
        {
            get => _hour;
        }

        public string Minute
        {
            get => _minute;
        }

        public DateTime Date
        {
            get => _date;
        }
    }
}
