using System;

namespace AlarmClockApp.Models
{
    class SoundPreset
    {
        private string _path;
        private string _name;

        public SoundPreset(string path, string name)
        {
            _path = path;
            _name = name;
        }

        public string Path
        {
            get => _path;
        }

        public string Name
        {
            get => _name;
        }
    }
}
