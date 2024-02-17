using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using OpenCvSharp;
using videx.Model;
using videx.Model.YOLOv5;
using System.Threading.Tasks;
using videx.View;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using System.Diagnostics;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using OxyPlot.Wpf;
using System.Data.SQLite;
using Dapper;


namespace videx.ViewModel
{
    public class ObjectDetectionViewModel : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        private static string outputFilePath = System.IO.Path.Combine(desktopPath, "output", "Thread", "output_video.avi");

        private static double fps = GetVideoFPS(outputFilePath);

        //public string outputFilePath = "C:\\Users\\psy\\Desktop\\output\\Thread1\\output.avi";
        public bool sldrDragStart = false;
        string StartT, EndT;


        public ObservableCollection<string> CheckedItems { get; set; }
        public ObservableCollection<CheckBoxItem> CheckBoxItems { get; set; }

        public System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        public ObjectDetectionViewModel()
        {

            Analysis(timer);
            SelectedOptions = new ObservableCollection<string>();
            videoObject = new MediaElement();
            playCommand = new RelayCommand(ExecutePlayCommand);
            pauseCommand = new RelayCommand(ExecutePauseCommand);
            stopCommand = new RelayCommand(ExecuteStopCommand);
            SelectCategoryCommand = new RelayCommand(ExecuteSelect, CanExecuteSelect);



            VideoObject.Source = new Uri(outputFilePath);
            VideoObject.LoadedBehavior = MediaState.Manual;
            VideoObject.UnloadedBehavior = MediaState.Manual;

            VideoObject.MediaOpened += VideoObject_MediaOpened;
            VideoObject.MediaEnded += VideoObject_MediaEnded;
            VideoObject.MediaFailed += VideoObject_MediaFailed;

            GetPredefinedColors();

            media_start();

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            InitializeCheckBoxItems();

            buttonCheckStates = Enumerable.Repeat(false, 10).ToArray();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadImages();
            ReloadPlot();
        }

        private async void Analysis(System.Windows.Threading.DispatcherTimer timer)
        {
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string outputPath = System.IO.Path.Combine(desktopPath, "edited.mp4");
            await Task.Run(() => YoloProcess.Execute(outputPath));

            if (YoloProcess.flag == 1)
            {
                VideoReady();
                timer.Stop();
                InitializePlot();
            }
        }

        private bool[] buttonCheckStates; // 각 버튼별로 독립적인 상태를 저장할 배열

        public ICommand SelectCategoryCommand { get; set; }


        private bool isChecking;

        public bool IsChecking
        {
            get { return isChecking; }
            set
            {
                if (isChecking != value)
                {
                    isChecking = value;
                    OnPropertyChanged(nameof(IsChecking));
                }
            }
        }

        private void ExecuteSelect(object parameter)
        {

            if (parameter is string parameterString)
            {
                if (int.TryParse(parameterString, out int buttonIndex))
                {
                    ToggleIsChecking(buttonIndex); // 각 버튼별로 독립적인 상태를 토글
                    SelectCheckBoxItemsByButtonIndex(buttonIndex);
                }

            }
        }

        private void ToggleIsChecking(int buttonIndex)
        {
            buttonCheckStates[buttonIndex] = !buttonCheckStates[buttonIndex];
            IsChecking = buttonCheckStates[buttonIndex];
        }

        private bool CanExecuteSelect(object parameter)
        {
            return true;
        }

        private void SelectCheckBoxItemsByButtonIndex(int buttonIndex)
        {
            switch (buttonIndex)
            {
                case 0:
                    SelectCheckBoxItems(0, 0);
                    break;

                case 1:
                    SelectCheckBoxItems(1, 8);
                    break;

                case 2:
                    SelectCheckBoxItems(9, 11);
                    break;

                case 3:
                    SelectCheckBoxItems(13, 13);
                    SelectCheckBoxItems(59, 62);
                    break;

                case 4:
                    SelectCheckBoxItems(25, 29);
                    break;

                case 5:
                    SelectCheckBoxItems(42, 46);
                    SelectCheckBoxItems(79, 79);
                    break;

                case 6:
                    SelectCheckBoxItems(49, 58);
                    break;

                case 7:
                    SelectCheckBoxItems(65, 77);
                    break;

                case 8:
                    SelectCheckBoxItems(14, 24);
                    break;

                case 9:
                    SelectCheckBoxItems(30, 30);
                    SelectCheckBoxItems(32, 40);
                    break;

                default:
                    break;
            }
        }

        private void SelectCheckBoxItems(int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                CheckBoxItems[i].IsChecked = isChecking;
                //Console.WriteLine(CheckBoxItems[i].IsChecked);
            }
        }

        private void InitializeCheckBoxItems()
        {

            CheckBoxItems = new ObservableCollection<CheckBoxItem>();

            for (int i = 0; i < 80; i++)
            {
                var checkBoxItem = new CheckBoxItem
                {
                    Content = LabelMap.Labels[i % LabelMap.Labels.Length],
                    IsChecked = false
                };
                //checkBoxItem.PropertyChanged += CheckBoxItem_PropertyChanged;
                CheckBoxItems.Add(checkBoxItem);
            }

            CheckBoxItems = new ObservableCollection<CheckBoxItem>(
                LabelMap.Labels.Select(label =>
                {
                    var checkBoxItem = new CheckBoxItem { Content = label, IsChecked = LabelMap.test_Labels.Contains(label) };
                    if (checkBoxItem.IsChecked == true)
                    {
                        checkBoxItem.IsEnabled = true;
                    }
                    checkBoxItem.PropertyChanged += CheckBoxItem_PropertyChanged;

                    if (!checkBoxItem.IsChecked)
                    {
                        checkBoxItem.IsEnabled = false;
                    }

                    return checkBoxItem;
                })
            );
        }

        private void CheckBoxItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckBoxItem.IsChecked))
            {
                LabelMap.test_Labels = CheckBoxItems.Where(item => item.IsChecked).Select(item => item.Content).ToArray();
                OnPropertyChanged(nameof(LabelMap.test_Labels));
                UpdateConcatenatedLabels();

            }
        }

        private string concatenatedLabels;

        public string ConcatenatedLabels
        {
            get { return concatenatedLabels; }
            set
            {
                if (concatenatedLabels != value)
                {
                    concatenatedLabels = value;
                    OnPropertyChanged(nameof(ConcatenatedLabels));
                }
            }
        }

        private void UpdateConcatenatedLabels()
        {
            ConcatenatedLabels = string.Join(",", LabelMap.test_Labels);
            if (string.IsNullOrEmpty(ConcatenatedLabels))
            {
                UpdateImage();
                InitializePlot();
            }
            else
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                LoadImages();
                stopwatch.Stop();
                Console.WriteLine("time : " +
                               stopwatch.ElapsedMilliseconds + "ms");
                ReloadPlot();
            }
        }

        private BitmapImage loadedImage;

        public BitmapImage LoadedImage
        {
            get { return loadedImage; }
            set
            {
                if (loadedImage != value)
                {
                    loadedImage = value;
                    OnPropertyChanged(nameof(LoadedImage));
                }
            }
        }

        private ObservableCollection<CheckBox> imageCheckBoxes;

        public ObservableCollection<CheckBox> ImageCheckBoxes
        {
            get { return imageCheckBoxes; }
            set
            {
                if (imageCheckBoxes != value)
                {
                    imageCheckBoxes = value;
                    OnPropertyChanged(nameof(ImageCheckBoxes));
                }
            }
        }

        private void LoadImages()
        {
            ImageCheckBoxes = new ObservableCollection<CheckBox>();

            List<ImageData> images;

            if (YoloProcess.flag == 1)
            {
                images = YoloProcess.SelectImage();
            }
            else
            {
                images = YoloProcess.selectedObj;
            }

            if (images != null && images.Count > 0)
            {
                foreach (var imageData in images)
                {
                    using (var stream = new MemoryStream(imageData.ImageBytes))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();

                        imageData.Bitmap = bitmapImage;
                    }

                    Image image = new Image();
                    image.Source = imageData.Bitmap;
                    image.Stretch = System.Windows.Media.Stretch.Uniform;
                    image.Width = 100;
                    image.Height = 100;

                    CheckBox checkBox = new CheckBox();
                    checkBox.Tag = imageData;

                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Orientation = Orientation.Vertical;
                    stackPanel.Children.Add(image);
                    stackPanel.Children.Add(new TextBlock()
                    {
                        Text = $"{imageData.Frame}_{imageData.Class}",
                        TextAlignment = TextAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.White)
                    });

                    checkBox.Content = stackPanel;

                    checkBox.Checked += CheckBox_Checked;
                    checkBox.Unchecked += CheckBox_Unchecked;

                    ImageCheckBoxes.Add(checkBox);
                }
            }
        }

        private void UpdateImage()
        {
            ImageCheckBoxes = new ObservableCollection<CheckBox>();

            List<ImageData> images = YoloProcess.allObj;

            if (images != null && images.Count > 0)
            {
                foreach (var imageData in images)
                {
                    using (var stream = new MemoryStream(imageData.ImageBytes))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();

                        imageData.Bitmap = bitmapImage;
                    }


                    Image image = new Image();
                    image.Source = imageData.Bitmap;
                    image.Stretch = System.Windows.Media.Stretch.Uniform;
                    image.Width = 100;
                    image.Height = 100;

                    CheckBox checkBox = new CheckBox();
                    checkBox.Tag = imageData;

                    StackPanel stackPanel = new StackPanel();
                    stackPanel.Orientation = Orientation.Vertical;
                    stackPanel.Children.Add(image);
                    stackPanel.Children.Add(new TextBlock()
                    {
                        Text = $"{imageData.Frame}_{imageData.Class}",
                        TextAlignment = TextAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.White)
                    });

                    checkBox.Content = stackPanel;

                    checkBox.Checked += CheckBox_Checked;
                    checkBox.Unchecked += CheckBox_Unchecked;

                    ImageCheckBoxes.Add(checkBox);
                }
            }
            else
            {
                //Console.WriteLine("null");
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is ImageData imageData)
            {
                Console.WriteLine($"CheckBox Checked! Frame: {imageData.Frame}");

                double timeAtFrame = GetTimeAtFrame(imageData.Frame, fps);

                Console.WriteLine($"Frame {imageData.Frame} is at {timeAtFrame} seconds.");

                MoveSliderToTime(timeAtFrame);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("CheckBox Unchecked!");
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
                // Handle the case where the TimeSpan is too large
                // You might want to limit the input or take alternative actions
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

        private ObservableCollection<string> selectedOptions = new ObservableCollection<string>();

        public ObservableCollection<string> SelectedOptions
        {
            get { return selectedOptions; }
            set
            {
                selectedOptions = value;
                OnPropertyChanged(nameof(SelectedOptions));
            }
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
            var dataPoints = GenerateSelectedDataPoints();

            var model = new PlotModel();

            List<string> distinctClasses = dataPoints.Select(dp => dp.Class).Distinct().ToList();

            for (int i = 0; i < distinctClasses.Count; i++)
            {
                string currentClass = distinctClasses[i];
                var lineSeries = new LineSeries
                {
                    Title = currentClass,
                    Color = predefinedColors[i % predefinedColors.Count], // Use the predefined color based on index
                };

                var classDataPoints = dataPoints.Where(dp => dp.Class == currentClass);
                lineSeries.Points.AddRange(classDataPoints.Select(dp => new DataPoint(dp.XValue, dp.YValue)));

                model.Series.Add(lineSeries);
            }

            model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight, LegendOrientation = LegendOrientation.Vertical });

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Second" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Object Appearance #", Minimum = 0, MajorStep = 1 });

            PlotModel = model;
        }

        private List<DataPointWithClass> GenerateDataPoints()
        {
            int totalFrame = YoloProcess.totalFrames;

            var dataPoints = new List<DataPointWithClass>();

            List<ImageData> tableData = YoloProcess.allObj;

            for (int i = 0; i < totalFrame; i++)
            {
                var frameData = tableData.Where(row => row.Frame == i).ToList();

                foreach (var group in frameData.GroupBy(row => row.Class))
                {
                    string currentClass = group.Key;
                    int objectCount = group.Count();

                    //Console.WriteLine($"Frame = {i}, Class = {currentClass}, Count = {objectCount}");

                    double time = GetTimeAtFrame(i, fps);

                    dataPoints.Add(new DataPointWithClass(time, objectCount, currentClass));
                }
            }

            return dataPoints;
        }

        List<OxyColor> predefinedColors;

        private void ReloadPlot()
        {
            var dataPoints = GenerateSelectedDataPoints();

            var model = new PlotModel();

            List<string> distinctClasses = dataPoints.Select(dp => dp.Class).Distinct().ToList();

            for (int i = 0; i < distinctClasses.Count; i++)
            {
                string currentClass = distinctClasses[i];
                var lineSeries = new LineSeries
                {
                    Title = currentClass,
                    Color = predefinedColors[i % predefinedColors.Count], // Use the predefined color based on index
                };

                var classDataPoints = dataPoints.Where(dp => dp.Class == currentClass);
                lineSeries.Points.AddRange(classDataPoints.Select(dp => new DataPoint(dp.XValue, dp.YValue)));

                model.Series.Add(lineSeries);
            }

            model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight, LegendOrientation = LegendOrientation.Vertical });

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Second" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Object Appearance #", Minimum = 0, MajorStep = 1 });

            PlotModel = model;
        }

        private List<DataPointWithClass> GenerateSelectedDataPoints()
        {
            int totalFrame = YoloProcess.totalFrames;

            var dataPoints = new List<DataPointWithClass>();

            List<ImageData> tableData;

            if (YoloProcess.flag == 1)
            {
                tableData = YoloProcess.SelectImage();
            }
            else
            {
                tableData = YoloProcess.selectedObj;
            }

            for (int i = 0; i < totalFrame; i++)
            {
                var frameData = tableData.Where(row => row.Frame == i).ToList();

                foreach (var group in frameData.GroupBy(row => row.Class))
                {
                    string currentClass = group.Key;
                    int objectCount = group.Count();

                    //Console.WriteLine($"Frame = {i}, Class = {currentClass}, Count = {objectCount}");
                    double time = GetTimeAtFrame(i, fps);

                    dataPoints.Add(new DataPointWithClass(time, objectCount, currentClass));
                }
            }

            return dataPoints;
        }

        private void GetPredefinedColors()
        {
            List<OxyColor> colors = new List<OxyColor>();

            for (int i = 0; i < 80; i++)
            {
                colors.Add(GetRandomColor());
            }

            predefinedColors = colors;
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


        private ICommand summaryCommand;
        public ICommand SummaryCommand
        {
            get
            {
                if (summaryCommand == null)
                {
                    summaryCommand = new RelayCommand(param => Summarization());
                }
                return summaryCommand;
            }
        }

        /*        private async void Summarization()
                {
                    VideoObject.Stop();


                    Console.WriteLine(concatenatedLabels);
                    // 기본 저장 경로를 desktopPath로 설정
                    string summaryPath = System.IO.Path.Combine(desktopPath, "output", "Thread", "output_video.avi");
                    ExportChart();
                    // SaveFileDialog를 사용하여 사용자에게 파일을 저장할 경로를 선택하도록 함
                    SaveFileDialog saveDialog = new SaveFileDialog()
                    {
                        DefaultExt = ".avi",
                        Filter = "AVI files (*.avi)|*.avi|All files (*.*)|*.*",
                        FileName = "output_video.avi"
                    };

                    // 사용자가 저장을 수락하면 파일 경로를 업데이트
                    if (saveDialog.ShowDialog() == true)
                    {
                        string userPath = saveDialog.FileName;

                        // 파일 복사
                        try
                        {
                            File.Copy(summaryPath, userPath, true);
                            MessageBox.Show("Summary Video, Chart exported successfully.", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                        catch (Exception ex)
                        {
                            // 복사 중 오류가 발생한 경우에 대한 처리
                            MessageBox.Show($"Error copying file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // 사용자가 저장을 취소한 경우에 대한 처리
                        return;
                    }
                }*/

        private async void Summarization()
        {
            VideoObject.Stop();

            List<ImageData> summarizeList = new List<ImageData>();

            string connectionStr = "Data Source=mydatabase.db;Version=3;Mode=Serialized;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionStr))
            {
                connection.Open();

                string[] targetClasses = concatenatedLabels.Split(',');

                summarizeList = connection.Query<ImageData>("SELECT Id, Class, Frame, Xmin, Ymin, Xmax, Ymax FROM ImageTable WHERE Class IN @TargetClasses", new { TargetClasses = targetClasses }).ToList();

                foreach (var data in summarizeList)
                {
                    Console.WriteLine($"Id: {data.Id}, Class: {data.Class}, Frame: {data.Frame}");
                }
            }

            string summaryPath = System.IO.Path.Combine(desktopPath, "output", "Thread");
            string inputFilePath = System.IO.Path.Combine(desktopPath, "edited.mp4");
            Make_Selected_Video(summaryPath, inputFilePath, summarizeList);
        }

        private static void Make_Selected_Video(string output_path, string inputFilePath, List<ImageData> summarizeList)
        {
            string outputFileName = Path.Combine(output_path, "summarize_video.avi");

            if (!Directory.Exists(output_path))
            {
                Directory.CreateDirectory(output_path);
                Console.WriteLine($"Directory '{output_path}' is created.");
            }

            using (var videoCapture = new VideoCapture(inputFilePath))
            {
                if (!videoCapture.IsOpened())
                {
                    Console.WriteLine("Can not open video.");
                    return;
                }

                double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                int width = (int)videoCapture.Get(VideoCaptureProperties.FrameWidth);
                int height = (int)videoCapture.Get(VideoCaptureProperties.FrameHeight);

                VideoWriter videoWriter = new VideoWriter(outputFileName, FourCC.XVID, fps, new OpenCvSharp.Size(width, height));
                if (!videoWriter.IsOpened())
                {
                    Console.WriteLine("VideoWriter open failed!");
                    return;
                }
                var frameNumber = 0;

                while (true)
                {
                    using (var frame = new Mat())
                    {
                        videoCapture.Read(frame);

                        if (frame.Empty())
                            break;

                        /* Cv2.Rectangle(frame, new OpenCvSharp.Point(obj.Box.Xmin, obj.Box.Ymin), new OpenCvSharp.Point(obj.Box.Xmax, obj.Box.Ymax), new Scalar(0, 0, 255), 1);
                         Cv2.PutText(frame, obj.Label, new OpenCvSharp.Point(obj.Box.Xmin, obj.Box.Ymin - 5), HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 0, 255), 1);
                         Cv2.PutText(frame, obj.Confidence.ToString("F2"), new OpenCvSharp.Point(obj.Box.Xmin, obj.Box.Ymin + 8), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 0, 0), 1);*/


                        videoWriter.Write(frame);

                    }
                    frameNumber++;
                }
                videoWriter.Release();
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
                    FileName = "chart_object.png"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var stream = File.Create(saveFileDialog.FileName))
                    {
                        exporter.Export(PlotModel, stream);
                    }

                    MessageBox.Show("Object Chart exported successfully.", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("No chart available to export.", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
