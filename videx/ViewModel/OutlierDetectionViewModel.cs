using OpenCvSharp;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using videx.Model.YOLOv5;
using videx.Model;
using static videx.ViewModel.AnalysisViewModel;
using System.Text.RegularExpressions;
using videx.View;

namespace videx.ViewModel
{
    public class OutlierDetectionViewModel : INotifyPropertyChanged
    {
    public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        private static string inputFilePath = System.IO.Path.Combine(desktopPath, "bus.mp4");

        private static double fps = GetVideoFPS(inputFilePath);

        public bool sldrDragStart = false;
        string StartT, EndT;

        public OutlierDetectionViewModel()
        {
            videoObject = new MediaElement();
            playCommand = new RelayCommand(ExecutePlayCommand);
            pauseCommand = new RelayCommand(ExecutePauseCommand);
            stopCommand = new RelayCommand(ExecuteStopCommand);

            VideoObject.Source = new Uri(inputFilePath);
            VideoObject.LoadedBehavior = MediaState.Manual;
            VideoObject.UnloadedBehavior = MediaState.Manual;

            VideoObject.MediaOpened += VideoObject_MediaOpened;
            VideoObject.MediaEnded += VideoObject_MediaEnded;
            VideoObject.MediaFailed += VideoObject_MediaFailed;

            //LoadImages();
            media_start();

            InitializePlot();
        }

        static double GetVideoFPS(string videoPath)
        {
            using (var videoCapture = new VideoCapture(videoPath))
            {
                return videoCapture.Fps;
            }
        }

        static double GetTimeAtFrame(int frameNumber, double fps)
        {
            double seconds = frameNumber / fps;
            return seconds;
        }

        private void MoveSliderToTime(double time)
        {
            TimeSpan newPosition = TimeSpan.FromSeconds(time);

            if (newPosition.TotalSeconds <= TimeSpan.MaxValue.TotalSeconds)
            {
                VideoObject.Position = newPosition;
                SlderPlayTime = time;
                VideoObject.Pause();
            }
            else
            {
                Console.WriteLine("The specified time is too large.");
            }
        }

        private double _slderPlaytime;
        public double SlderPlayTime
        {
            get { return _slderPlaytime; }
            set
            {
                if (_slderPlaytime != value)
                {
                    _slderPlaytime = value;
                    OnPropertyChanged(nameof(SlderPlayTime));
                }
            }
        }

        private double sliderMinimum;
        public double SliderMinimum
        {
            get { return sliderMinimum; }
            set
            {
                if (sliderMinimum != value)
                {
                    sliderMinimum = value;
                    OnPropertyChanged(nameof(SliderMinimum));
                }
            }
        }

        private double sliderMaximum;
        public double SliderMaximum
        {
            get { return sliderMaximum; }
            set
            {
                if (sliderMaximum != value)
                {
                    sliderMaximum = value;
                    OnPropertyChanged(nameof(SliderMaximum));
                }
            }
        }

        private string videoInfo;
        public string VideoInfo
        {
            get { return videoInfo; }
            set
            {
                if (videoInfo != value)
                {
                    videoInfo = value;
                    OnPropertyChanged(nameof(VideoInfo));
                }
            }
        }

        private string _lblPlayTime;
        public string LblPlayTime
        {
            get { return _lblPlayTime; }
            set
            {
                if (_lblPlayTime != value)
                {
                    _lblPlayTime = value;
                    OnPropertyChanged(nameof(LblPlayTime));
                }
            }
        }

        private void media_start()
        {
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += TimerTickHandler;
            timer.Start();
        }

        private void TimerTickHandler(object sender, EventArgs e)
        {
            if (sldrDragStart)
                return;

            if (VideoObject.Source == null || !VideoObject.NaturalDuration.HasTimeSpan)
            {
                LblPlayTime = "No file selected...";
                return;
            }

            SlderPlayTime = VideoObject.Position.TotalSeconds;

        }

        private void VideoObject_MediaOpened(object sender, RoutedEventArgs e)
        {
            SliderMinimum = 0;
            SliderMaximum = VideoObject.NaturalDuration.TimeSpan.TotalSeconds;

            StartT = "00:00:00";
            EndT = VideoObject.NaturalDuration.ToString();
        }

        private void VideoObject_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoObject.Stop();
        }

        private void VideoObject_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("Video playback failed : " + e.ErrorException.Message.ToString());
        }

        private MediaElement _videoElement = new MediaElement();
        public MediaElement VideoObject
        {
            get { return _videoElement; }
            set { _videoElement = value; }
        }

        private bool isPlaying;
        private MediaElement videoObject;
        private RelayCommand playCommand;
        private RelayCommand pauseCommand;
        private RelayCommand stopCommand;

        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (isPlaying != value)
                {
                    isPlaying = value;
                    OnPropertyChanged(nameof(IsPlaying));
                }
            }
        }

        public ICommand PlayCommand
        {
            get { return playCommand; }
        }

        public ICommand PauseCommand
        {
            get { return pauseCommand; }
        }

        public ICommand StopCommand
        {
            get { return stopCommand; }
        }

        private void ExecutePlayCommand(object parameter)
        {
            IsPlaying = true;
            VideoObject.Play();
        }

        private void ExecutePauseCommand(object parameter)
        {
            IsPlaying = false;
            VideoObject.Pause();
        }

        private void ExecuteStopCommand(object parameter)
        {
            IsPlaying = false;
            VideoObject.Stop();
        }

        private PlotModel plotModel;

        public PlotModel PlotModel
        {
            get { return plotModel; }
            set
            {
                plotModel = value;
                OnPropertyChanged(nameof(PlotModel));
            }
        }

        private void InitializePlot()
        {
            var dataPoints = GenerateDataPoints();

            var model = new PlotModel();
            var lineSeries = new LineSeries();
            lineSeries.Points.AddRange(dataPoints);
            model.Series.Add(lineSeries);

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Second" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Outlier" });

            PlotModel = model;
        }

        private DataPoint[] GenerateDataPoints()
        {
            var capture = new VideoCapture(inputFilePath);
            var totalFrame = (int)capture.Get(VideoCaptureProperties.FrameCount);

            var random = new Random();
            var dataPoints = new DataPoint[totalFrame];
            for (int i = 0; i < totalFrame; i++)
            {
                double time = GetTimeAtFrame(i, fps);
                double yValue;
                if ((i >= 30 && i <= 40) || (i >= 80 && i <= 90) || (i >= 230 && i <= 240) || (i >= 580 && i <= 590))
                {
                    yValue = 100 + random.NextDouble() * 50;
                }
                else
                {
                    yValue = random.NextDouble() * 10; 
                }

                dataPoints[i] = new DataPoint(time, yValue);
            }

            return dataPoints;
        }

        private RelayCommand _plotClickCommand;
        public RelayCommand PlotClickCommand
        {
            get
            {
                return _plotClickCommand ?? (_plotClickCommand = new RelayCommand(param => PlotClick((MouseEventArgs)param)));
            }
        }

        private void PlotClick(MouseEventArgs e)
        {
            // 여기에서 클릭한 지점의 시간을 계산하고 동영상을 이동하는 로직 추가
            if (PlotModel != null && PlotModel.Axes.Count == 2)
            {
                var xAxis = PlotModel.Axes[0] as DateTimeAxis;
                if (xAxis != null)
                {
                    double xValue = xAxis.InverseTransform(e.GetPosition(null).X);
                    TimeSpan clickedTime = TimeSpan.FromMilliseconds(xValue);

                    Console.WriteLine(xValue);
                    MoveVideoToTime(clickedTime);
                }
            }
        }

        private void MoveVideoToTime(TimeSpan time)
        {
            VideoObject.Position = time;
            VideoObject.Pause();
        }

    }
}