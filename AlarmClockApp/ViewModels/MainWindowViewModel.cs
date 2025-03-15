using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AlarmClockApp.Commands;
using AlarmClockApp.Models;
using AlarmClockApp.Properties;
using AlarmClockApp.ViewModels.Base;
using Microsoft.Win32;

namespace AlarmClockApp.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Заголовок окна
        /// <summary> Заголовок окна </summary>
        private string _title = "Alarm Clock";

        public string Title
        {
            get => _title;

            set => Set(ref _title, value);
        }
        #endregion

        #region Время на часах

        /// <summary> Время и дата на часах </summary>
        private string _clock = "00:00";
        private string _date = "DD.MM.YY";
        private DispatcherTimer _currentTimer;
        private readonly Timer _timer;


        public string Clock
        {
            get => _clock;
            set => Set(ref _clock, value);
        }

        public string Date
        {
            get => _date;
            set => Set(ref _date, value);
        }

        public TimeSpan UpdateInterval
        {
            get { return _currentTimer.Interval; }
            set { _currentTimer.Interval = value; }
        }

        #endregion

        #region Время на секундомере
        /// <summary> Время на секундомере </summary>
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private string _stopwatchTime = "00:00.0";
        private bool _isPaused = false;
        private string _startButtonName = "Start";
        private string _stopButtonName = "Stop";


        public string StopwatchTime
        {
            get => _stopwatchTime;
            set => Set(ref _stopwatchTime, value);
        }

        public string StartButtonName
        {
            get => _startButtonName;
            set => Set(ref _startButtonName, value);
        }

        public string StopButtonName
        {
            get => _stopButtonName;
            set => Set(ref _stopButtonName, value);
        }

        #endregion

        #region Время на будильнике
        /// <summary> Время на будильнике </summary>

        private int _alarmHours = DateTime.Now.Hour;
        private int _alarmMinutes = DateTime.Now.Minute;
        private DateTime _chosenDate = DateTime.Now.Date;
        private string _remainingTime = " ";
        private Stopwatch _countdownTimer = new Stopwatch();
        private CancellationTokenSource _cts;
        private MediaPlayer _player;

        public int AlarmHours
        {
            get => _alarmHours;
            set
            {
                if (value >= 0 && value < 24)
                {
                    Set(ref _alarmHours, value);
                }
                else
                {
                    _remainingTime = "Error! Invalid time format.";
                }
            }
        }

        public int AlarmMinutes
        {
            get => _alarmMinutes;
            set
            {
                if (value >= 0 && value < 60)
                {
                    Set(ref _alarmMinutes, value);
                }
                else
                {
                    _remainingTime = "Error! Invalid time format.";
                }
            }
        }

        public string RemainingTime
        {
            get => _remainingTime;
            set => Set(ref _remainingTime, value);
        }

        public DateTime ChosenDate
        {
            get => _chosenDate;
            set => Set(ref _chosenDate, value);
        }

        #endregion

        private DatePreset _selectedPreset;
        private ObservableCollection<DatePreset> _presets;

        public DatePreset SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                Set(ref _selectedPreset, value);
                AlarmHours = Int32.Parse(_selectedPreset.Hour);
                AlarmMinutes = Int32.Parse(_selectedPreset.Minute);
                ChosenDate = _selectedPreset.Date;
            }
        }

        public ObservableCollection<DatePreset> Presets
        {
            get => _presets;
        }

        private SoundPreset _selectedSoundPreset;
        private ObservableCollection<SoundPreset> _soundPresets;

        public SoundPreset SelectedSoundPreset
        {
            get => _selectedSoundPreset;
            set
            {
                Set(ref _selectedSoundPreset, value);
            }
        }

        public ObservableCollection<SoundPreset> SoundPresets
        {
            get => _soundPresets;
        }

        #region Команды

        #region StartStopwatch

        public ICommand StartStopwatchCommand { get; }

        private void OnStartStopwatchCommandExecuted(object p)
        {
            _stopWatch.Start();
            StartButtonName = "Conitnue";
            StopButtonName = "Stop";
            _isPaused = false;
        }

        private bool CanStartStopwatchCommandExecute(object p) => !_stopWatch.IsRunning;

        #endregion

        #region PauseStopwatch

        public ICommand PauseStopwatchCommand { get; }

        private void OnPauseStopwatchCommandExecuted(object p)
        {
            if (!_isPaused)
            {
                _stopWatch.Stop();
                StopButtonName = "Reset";
                _isPaused = true;
            }
            else
            {
                _stopWatch.Reset();
                StopButtonName = "Stop";
                StartButtonName = "Start";
                _isPaused = false;
            }
        }

        private bool CanPauseStopwatchCommandExecute(object p) => true;

        #endregion

        #region StartAlarm

        public ICommand StartAlarmCommand { get; }

        private async void OnStartAlarmCommandExecuted(object p)
        {
            string[] dateArr = (ChosenDate - DateTime.Now.Date).ToString().Split('.');
            int diff;          
            bool isValid = int.TryParse(dateArr[0], out diff);
            if (diff >= 0)
            {
                if (AlarmHours * 60 + AlarmMinutes <= DateTime.Now.Hour * 60 + DateTime.Now.Minute && diff == 0)
                {
                    diff += 1;
                    ChosenDate = ChosenDate.AddDays(1);
                }
                int totalHours = diff * 24 + AlarmHours - DateTime.Now.Hour;
                int totalMinutes = AlarmMinutes - DateTime.Now.Minute;

                double totalTime = totalHours * 60 * 60 + totalMinutes * 60 - DateTime.Now.Second;
                double currentDiff, hours, minutes;

                if (totalTime > 0)
                {
                    _countdownTimer.Start();
                    try
                    {
                        using (_cts = new CancellationTokenSource())
                        {
                            while (_countdownTimer.IsRunning)
                            {
                                currentDiff = totalTime - _countdownTimer.Elapsed.TotalSeconds;
                                hours = (int)currentDiff / (60 * 60);
                                minutes = (int)currentDiff / 60 % 60;
                                RemainingTime = $"{hours} h. {minutes} m.";
                                await Task.Delay(1000, _cts.Token);

                                if (_countdownTimer.Elapsed.TotalSeconds >= totalTime)
                                {
                                    _countdownTimer.Reset();
                                    RemainingTime = "IT'S TIME!";
                                    _player = new MediaPlayer();
                                    if (SoundPresets.Count != 0)
                                    {
                                        _player.Open(new Uri(SelectedSoundPreset.Path));
                                    }
                                    else
                                    {
                                        _player.Open(new Uri("D:\\Новая папка\\5606-quagmire-toilet-meme.mp3", UriKind.Relative));
                                    }

                                    _player.Play();
                                }
                            }
                            _countdownTimer.Reset();
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Debug.Fail(ex.ToString());
                    }
                    _cts = null;
                }
                else
                {
                    RemainingTime = "Error!";
                }
            }
        }

        private bool CanStartAlarmCommandExecute(object p) => !_countdownTimer.IsRunning;

        #endregion

        #region ResetAlarm

        public ICommand ResetAlarmCommand { get; }

        private void OnResetAlarmCommandExecuted(object p)
        {
            _countdownTimer.Reset();
            _player?.Stop();
            RemainingTime = " ";
            AlarmHours = DateTime.Now.Hour;
            AlarmMinutes = DateTime.Now.Minute;
            ChosenDate = DateTime.Now.Date;
        }

        private bool CanResetAlarmCommandExecute(object p) => true;

        #endregion

        #region AddPreset

        public ICommand AddPresetCommand { get; }

        public void OnAddPresetCommandExecuted(object p)
        {
            for (int i = 0; i < Presets.Count; i++)
            {
                if (AlarmHours == Int32.Parse(Presets[i].Hour) && AlarmMinutes == Int32.Parse(Presets[i].Minute))
                {
                    return;
                }
            }
            DatePreset newPreset;
            if (AlarmHours * 60 + AlarmMinutes <= DateTime.Now.Hour * 60 + DateTime.Now.Minute && ChosenDate.Day - DateTime.Now.Day == 0)
            {
                newPreset = new DatePreset(AlarmHours.ToString(), AlarmMinutes.ToString(), ChosenDate.AddDays(1));
            }
            else
            {
                newPreset = new DatePreset(AlarmHours.ToString(), AlarmMinutes.ToString(), ChosenDate);
            }
            Presets.Add(newPreset);
        }

        public bool CanAddPresetCommandExecute(object p) => true;

        #endregion

        #region RemovePreset

        public ICommand RemovePresetCommand { get; }

        public void OnRemovePresetCommandExecuted(object p)
        {
            Presets.Remove(_selectedPreset);
        }

        public bool CanRemovePresetCommandExecute(object p) => true;

        #endregion

        #region AddSound

        public ICommand AddSoundCommand { get; }

        public void OnAddSoundCommandExecuted(object p)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            SoundPreset soundPreset;
            ofd.Title = "Выбор звука";
            ofd.Filter = "Файлы (*.wav, *.mp3, *.m4a)|*.wav, *.mp3, *.m4a";

            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.SafeFileName;
                string path = ofd.FileName;
                fileName = fileName.Replace(".m4a", "").Replace(".mp3", "").Replace(".wav", "");

                for (int i = 0; i < SoundPresets.Count; i++)
                {
                    if (SoundPresets[i].Name == fileName)
                    {
                        return;
                    }
                }
                
                soundPreset = new SoundPreset(path, fileName);
                SoundPresets.Add(soundPreset);
            }
        }

        public bool CanAddSoundCommandExecute(object p) => !_countdownTimer.IsRunning;

        #endregion

        #region RemoveSound

        public ICommand RemoveSoundCommand { get; }

        public void OnRemoveSoundCommandExecuted(object p)
        {
            SoundPresets.Remove(_selectedSoundPreset);
        }

        public bool CanRemoveSoundCommandExecute(object p) => !_countdownTimer.IsRunning;

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            #region Команды

            StartStopwatchCommand = new LambdaCommand(OnStartStopwatchCommandExecuted, CanStartStopwatchCommandExecute);

            PauseStopwatchCommand = new LambdaCommand(OnPauseStopwatchCommandExecuted, CanPauseStopwatchCommandExecute);

            StartAlarmCommand = new LambdaCommand(OnStartAlarmCommandExecuted, CanStartAlarmCommandExecute);

            ResetAlarmCommand = new LambdaCommand(OnResetAlarmCommandExecuted, CanResetAlarmCommandExecute);

            AddPresetCommand = new LambdaCommand(OnAddPresetCommandExecuted, CanAddPresetCommandExecute);

            RemovePresetCommand = new LambdaCommand(OnRemovePresetCommandExecuted, CanRemovePresetCommandExecute);

            AddSoundCommand = new LambdaCommand(OnAddSoundCommandExecuted, CanAddSoundCommandExecute);

            RemoveSoundCommand = new LambdaCommand(OnRemoveSoundCommandExecuted, CanRemoveSoundCommandExecute);

            #endregion

            _clock = DateTime.Now.ToString("T");
            _date = DateTime.Now.ToString("d");

            _currentTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000),
                DispatcherPriority.Normal, (s, e) => Clock = DateTime.Now.ToString("T"), Application.Current.Dispatcher);
            _timer = new Timer(_ => StopwatchTime =
            String.Format("{0:00}:{1:00}.{2:000}", _stopWatch.Elapsed.Minutes, _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds),
            null, 0, 10);

            _presets = new ObservableCollection<DatePreset>();
            _soundPresets = new ObservableCollection<SoundPreset>();
        }
    }
}
