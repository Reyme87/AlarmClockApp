using System.Diagnostics;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AlarmClockApp.Commands;
using AlarmClockApp.Properties;
using AlarmClockApp.ViewModels.Base;

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
            if (diff >= 0 && (!Equals(AlarmMinutes, DateTime.Now.Minute) || !Equals(AlarmHours, DateTime.Now.Hour)))
            {
                int totalHours = diff * 24 + AlarmHours - DateTime.Now.Hour;
                int totalMinutes = AlarmMinutes - DateTime.Now.Minute;

                double totalTime = totalHours * 60 * 60 + totalMinutes * 60 - DateTime.Now.Second;
                double currentDiff, hours, minutes;
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
                                _player.Open(new Uri("D:\\Новая папка\\5606-quagmire-toilet-meme.mp3", UriKind.Relative));
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
        }

        private bool CanStartAlarmCommandExecute(object p) => !_countdownTimer.IsRunning;

        #endregion

        #region ResetAlarm

        public ICommand ResetAlarmCommand { get; }

        private void OnResetAlarmCommandExecuted(object p)
        {
            _countdownTimer.Reset();
            _player.Stop();
            RemainingTime = " ";
            AlarmHours = DateTime.Now.Hour;
            AlarmMinutes = DateTime.Now.Minute;
            ChosenDate = DateTime.Now.Date;
        }

        private bool CanResetAlarmCommandExecute(object p) => true;

        #endregion

        #endregion

        public MainWindowViewModel()
        {
            #region Команды

            StartStopwatchCommand = new LambdaCommand(OnStartStopwatchCommandExecuted, CanStartStopwatchCommandExecute);

            PauseStopwatchCommand = new LambdaCommand(OnPauseStopwatchCommandExecuted, CanPauseStopwatchCommandExecute);

            StartAlarmCommand = new LambdaCommand(OnStartAlarmCommandExecuted, CanStartAlarmCommandExecute);

            ResetAlarmCommand = new LambdaCommand(OnResetAlarmCommandExecuted, CanResetAlarmCommandExecute);

            #endregion

            _clock = DateTime.Now.ToString("T");
            _date = DateTime.Now.ToString("d");

            _currentTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1000),
                DispatcherPriority.Normal, (s, e) => Clock = DateTime.Now.ToString("T"), Application.Current.Dispatcher);
            _timer = new Timer(_ => StopwatchTime =
            String.Format("{0:00}:{1:00}.{2:000}", _stopWatch.Elapsed.Minutes, _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds),
            null, 0, 10);
        }
    }
}
