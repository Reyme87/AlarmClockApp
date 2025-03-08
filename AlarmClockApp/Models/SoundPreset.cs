using System;

namespace AlarmClockApp.Models
{
    class SoundPreset
    {
        private string _path;
        private string _length;
        private string _name;

        public SoundPreset(string path, string length, string name)
        {
            _path = path;
            _length = length;
            _name = name;
        }

        public string Path
        {
            get => _path;
        }

        public string Length
        {
            get => _length;
        }

        public string Name
        {
            get => _name;
        }
    }
}
