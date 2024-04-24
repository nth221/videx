using OpenCvSharp;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using OxyPlot.Wpf;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using Microsoft.Win32;
using videx.Model.AnomalyDetection;
using videx.Model.YOLOv5;
using static videx.ViewModel.ObjectDetectionViewModel;
using System.Collections.Generic;
using videx.Model;

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
        private static string inputFilePath = System.IO.Path.Combine(desktopPath, "edited.mp4");

        private static double fps = GetVideoFPS(inputFilePath);

        public bool sldrDragStart = false;
        string StartT, EndT;

        public System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        public ICommand ShowGraphCommand { get; set; }

        private static List<double> dataPoints;
        public OutlierDetectionViewModel()
        {
            ShowGraphCommand = new RelayCommand(ShowGraph);

            OutlierAnalysis(timer);

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

            media_start();

            timer.Interval = TimeSpan.FromSeconds(0.3);
            timer.Tick += Timer_Tick;
            timer.Start();

            // InitializePlot();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // ReloadPlot();
        }

        private async void OutlierAnalysis(System.Windows.Threading.DispatcherTimer timer)
        {
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string outputPath = System.IO.Path.Combine(desktopPath, "edited.mp4");

            await Task.Run(() => AnomalyDetection.Detection());

            if (AnomalyDetection.endFlag == 1)
            {
                VideoReady();
                timer.Stop();
                InitializePlot();
            }
        }

        private ICommand saveGraph;
        public ICommand SaveGraph
        {
            get
            {
                if (saveGraph == null)
                {
                    saveGraph = new RelayCommand(param => ExportChart());
                }
                return saveGraph;
            }
        }

        private void ExportChart()
        {
            if (PlotModel != null)
            {
                PlotModel.Background = OxyColors.White;

                var exporter = new PngExporter { Width = 600, Height = 400 };

                var saveFileDialog = new SaveFileDialog
                {
                    DefaultExt = ".png",
                    Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*",
                    FileName = "chart_outlier.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var stream = File.Create(saveFileDialog.FileName))
                    {
                        exporter.Export(PlotModel, stream);
                    }
                    MessageBox.Show("Outlier Chart exported successfully.", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No chart available to export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void ShowGraph(object parameter)
        {
            if (parameter is string algorithm)
            {

                switch (algorithm)
                {
                    case "LOF":
                        Console.WriteLine("LOF");
                        dataPoints = AnomalyDetection.lofValues;
                        ReloadPlot(dataPoints);
                        break;
                    case "cbLOF":
                        Console.WriteLine("cbLOF");
                        dataPoints = AnomalyDetection.cblofValues;
                        ReloadPlot(dataPoints);
                        break;
                    case "iForest":
                        Console.WriteLine("iForest");
                        dataPoints = AnomalyDetection.iForestValues;
                        ReloadPlot(dataPoints);
                        break;
                }
            }
        }

        private void InitializePlot()
        {
            var dataPoints = AnomalyDetection.lofValues;

            var model = new PlotModel();
            var lineSeries = new LineSeries();

            // Extracting data from transformedFeatures and adding to lineSeries
            for (int i = 0; i < dataPoints.Count-1; i++)
            {
                lineSeries.Points.Add(new DataPoint(i + 1, dataPoints[i]));
            }

            model.Series.Add(lineSeries);

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "First PCA Component" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Second PCA Component"});

            PlotModel = model;
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
                var xAxis = PlotModel.Axes[0];
                if (xAxis != null)
                {
                    double xValue = xAxis.InverseTransform(e.GetPosition(null).X);
                    TimeSpan clickedTime = TimeSpan.FromSeconds(xValue);

                    Console.WriteLine(xValue);
                    MoveVideoToTime(clickedTime);
                }
            }
        }

        private void MoveVideoToTime(TimeSpan time)
        {
            VideoObject.Position = time;
            VideoObject.Pause();
            VideoObject.Play();
        }


        private Visibility loading;

        public Visibility Loading
        {
            get { return loading; }
            set
            {
                if (loading != value)
                {
                    loading = value;
                    OnPropertyChanged(nameof(Loading));
                }
            }
        }

        public void VideoReady()
        {
            Loading = Visibility.Collapsed;
        }

        List<OxyColor> predefinedColors;

        private void ReloadPlot(List<double> dataPoints)
        {
            var model = new PlotModel();
            var lineSeries = new LineSeries();

            // Extracting data from transformedFeatures and adding to lineSeries
            for (int i = 0; i < dataPoints.Count-1; i++)
            {
                lineSeries.Points.Add(new DataPoint(i + 1, dataPoints[i]));
            }

            model.Series.Add(lineSeries);

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "First PCA Component" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Second PCA Component" });

            PlotModel = model;
        }

        private static Random random = new Random();

        private static OxyColor GetRandomColor()
        {
            byte[] color = new byte[3];
            random.NextBytes(color);
            return OxyColor.FromRgb(color[0], color[1], color[2]);
        }

        public class DataPointWithClass
        {
            public double XValue { get; set; }
            public double YValue { get; set; }
            public string Class { get; set; }

            public DataPointWithClass(double xValue, double yValue, string className)
            {
                XValue = xValue;
                YValue = yValue;
                Class = className;
            }
        }
    }
}