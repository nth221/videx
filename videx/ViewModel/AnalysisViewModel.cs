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

namespace videx.ViewModel
{
    public class AnalysisViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
        private string outputFilePath = System.IO.Path.Combine(desktopPath, "output", "Thread", "output_video.avi");

        //public string outputFilePath = "C:\\Users\\psy\\Desktop\\output\\Thread1\\output.avi";
        public bool sldrDragStart = false;
        string StartT, EndT;


        public ObservableCollection<string> CheckedItems { get; set; }
        public ObservableCollection<CheckBoxItem> CheckBoxItems { get; set; }
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

        public AnalysisViewModel()
        {
            Analysis();
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

            //LoadImages();
            media_start();

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();




            InitializeCheckBoxItems();

        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private async void Analysis()
        {
            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string outputPath = System.IO.Path.Combine(desktopPath, "edited.mp4");
            await Task.Run(() => YoloProcess.Execute(outputPath));
            if (YoloProcess.flag == 1)
            {
                VideoReady();
            }
        }

        private void DeleteFolder()
        {
            string directoryPath = "C:\\Users\\psy\\Desktop\\output\\Thread1\\exp1";
            Directory.Delete(directoryPath, true);

            Console.WriteLine("Directory is successfully deleted.");
        }

        private void ExecuteSelect(object parameter)
        {

            if (parameter is string parameterString)
            {
                if (int.TryParse(parameterString, out int buttonIndex))
                {
                    ToggleIsChecking();
                    SelectCheckBoxItemsByButtonIndex(buttonIndex);
                }

            }
        }

        private void ToggleIsChecking()
        {
            IsChecking = !IsChecking;
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
                    SelectCheckBoxItems(56, 59);
                    break;

                case 4:
                    SelectCheckBoxItems(24, 79);
                    break;

                case 5:
                    SelectCheckBoxItems(39, 71);
                    break;

                case 6:
                    SelectCheckBoxItems(46, 55);
                    break;

                case 7:
                    SelectCheckBoxItems(62, 78);
                    break;

                case 8:
                    SelectCheckBoxItems(14, 38);
                    break;

                case 9:
                    SelectCheckBoxItems(29, 38);
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
                OnSelectedLabelsChanged();

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
            ConcatenatedLabels = string.Join(", ", LabelMap.test_Labels);
        }

        private void OnSelectedLabelsChanged()
        {
            UpdateConcatenatedLabels();
            LoadImages();
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

            List<ImageData> images = YoloProcess.SelectImage();

            if (images != null && images.Count > 0)
            {
                foreach (var imageData in images)
                {
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

            List<ImageData> images = YoloProcess.tableInfo;

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
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            double fps = GetVideoFPS(outputFilePath);
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
            SlderPlayTime = time;
            VideoObject.Pause();
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
    }
}
