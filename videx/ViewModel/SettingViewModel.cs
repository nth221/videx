using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using videx.Model;
using videx.View;
using Microsoft.Win32;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using videx.Model.YOLOv5;

namespace videx.ViewModel
{
    public class SettingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string[] SelectedLabels { get; set; }
        string filePath; 
        string editVideoPath;
        public bool sldrDragStart = false; 
        public bool check_time = false;
        bool edit_check = false;
        public TimeSpan ST, ET;
        Stopwatch videoEdit_time = new Stopwatch();
        private System.Windows.Window currentWindow;




        /* Filter Option List */

        public ObservableCollection<string> CheckedItems { get; set; }
        public ObservableCollection<CheckBoxItem> CheckBoxItems { get; set; }


        public SettingViewModel()
        {
            SelectedOptions = new ObservableCollection<string>();
            videoObject = new MediaElement();
            playCommand = new RelayCommand(ExecutePlayCommand, CanExecutePlayCommand);
            pauseCommand = new RelayCommand(ExecutePauseCommand, CanExecutePauseCommand);
            stopCommand = new RelayCommand(ExecuteStopCommand, CanExecuteStopCommand);


            CheckedItems = new ObservableCollection<string>();

            InitializeCheckBoxItems();

            VideoObject.LoadedBehavior = MediaState.Manual;
            VideoObject.UnloadedBehavior = MediaState.Manual;

            VideoObject.MediaOpened += VideoObject_MediaOpened;
            VideoObject.MediaEnded += VideoObject_MediaEnded;
            VideoObject.MediaFailed += VideoObject_MediaFailed;

        }

        private void UpdateVideoObjectPosition()
        {
            if (TimeSpan.TryParseExact(SetTime, "hh\\:mm\\:ss", null, out TimeSpan setTime) &&
                TimeSpan.TryParseExact(EndTime, "hh\\:mm\\:ss", null, out TimeSpan endTime))
            {
                VideoObject.Position = setTime;
                ST = setTime;
                ET = endTime;
                Console.WriteLine(ST);
                Console.WriteLine(ET);

                if (VideoObject.Position > endTime)
                {
                    VideoObject.Position = endTime;
                    ET = endTime;
                    Console.WriteLine(ST);
                    Console.WriteLine(ET);
                }
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
                checkBoxItem.PropertyChanged += CheckBoxItem_PropertyChanged;
                CheckBoxItems.Add(checkBoxItem);
            }
        }

        private void CheckBoxItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CheckBoxItem.IsChecked))
            {
                SelectedLabels = CheckBoxItems.Where(item => item.IsChecked).Select(item => item.Content).ToArray();
                OnPropertyChanged(nameof(SelectedLabels));
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
            ConcatenatedLabels = string.Join(", ", SelectedLabels);
        }


        private void OnSelectedLabelsChanged()
        {
            UpdateConcatenatedLabels();

        }

        private void UpdateTestLabels()
        {
            Array.Clear(LabelMap.test_Labels, 0, LabelMap.test_Labels.Length);

            for (int i = 0; i < SelectedLabels.Length && i < LabelMap.test_Labels.Length; i++)
            {
                string selectedLabel = SelectedLabels[i];

                int indexInLabels = Array.IndexOf(LabelMap.Labels, selectedLabel);

                if (indexInLabels != -1)
                {
                    LabelMap.test_Labels[indexInLabels] = selectedLabel;
                    Console.WriteLine($"SelectedLabel: {selectedLabel}, Index in Labels: {indexInLabels}");
                }
            }

        }


        private ObservableCollection<string> selectedOptions;
        public ObservableCollection<string> SelectedOptions
        {
            get { return selectedOptions; }
            set
            {
                if (selectedOptions != value)
                {
                    selectedOptions = value;
                    OnPropertyChanged(nameof(SelectedOptions));
                }
            }
        }

        private string _videofilename;
        public string Videofilename
        {
            get { return _videofilename; }
            set
            {
                if (_videofilename != value)
                {
                    _videofilename = value;
                    OnPropertyChanged(nameof(Videofilename));
                }
            }
        }

        private string _videofile;
        public string Videofile
        {
            get { return _videofile; }
            set
            {
                if (_videofile != value)
                {
                    _videofile = value;
                    OnPropertyChanged(nameof(Videofile));
                }
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


        private string setTime;
        public string SetTime
        {
            get { return setTime; }
            set
            {
                if (setTime != value)
                {
                    setTime = value;
                    OnPropertyChanged(nameof(SetTime));

                    UpdateVideoObjectPosition();
                }
            }
        }

        private string endTime;
        public string EndTime
        {
            get { return endTime; }
            set
            {
                if (endTime != value)
                {
                    endTime = value;
                    OnPropertyChanged(nameof(EndTime));

                    UpdateVideoObjectPosition();
                }
            }
        }

        private ICommand btnSelectFileCommand;
        public ICommand BtnSelectFileCommand
        {
            get
            {
                if (btnSelectFileCommand == null)
                {
                    btnSelectFileCommand = new RelayCommand(param => BtnSelectFile());
                }
                return btnSelectFileCommand;
            }
        }

        private ICommand btnRemoveFileCommand;
        public ICommand BtnRemoveFileCommand
        {
            get
            {
                if (btnRemoveFileCommand == null)
                {
                    btnRemoveFileCommand = new RelayCommand(param => BtnRemoveFile());
                }
                return btnRemoveFileCommand;
            }
        }


        private Visibility checkBoxVisibility = Visibility.Hidden;

        private bool isChecked;

        public Visibility CheckBoxVisibility
        {
            get { return checkBoxVisibility; }
            set
            {
                if (checkBoxVisibility != value)
                {
                    checkBoxVisibility = value;
                    OnPropertyChanged(nameof(CheckBoxVisibility));
                }
            }
        }

        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));

                    if (isChecked)
                    {
                        HandleCheckBoxChecked();
                    }
                    else
                    {
                        HandleCheckBoxUnchecked();
                    }
                }
            }
        }

        private void HandleCheckBoxChecked()
        {
            VideoObject.Visibility = Visibility.Visible;
        }

        private void HandleCheckBoxUnchecked()
        {
            VideoObject.Visibility = Visibility.Hidden;
        }


        private void BtnSelectFile()
        {
            OpenFileDialog dlg = new OpenFileDialog()
            {
                DefaultExt = ".avi",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                filePath = dlg.FileName;
                Videofilename = filePath.Substring(filePath.LastIndexOf('\\') + 1);
                CheckBoxVisibility = Visibility.Visible;

                OnPropertyChanged(nameof(SelectedOptions));
                OnPropertyChanged(nameof(VideoInfo));
                VideoObject.Source = new Uri(dlg.FileName);

                DispatcherTimer timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += TimerTickHandler;
                timer.Start();
            }
        }

        private void BtnRemoveFile()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the selected items?", "Confirm deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                VideoObject.Source = null;
                CheckBoxVisibility = Visibility.Hidden;
            }
        }


        private ICommand analysisCommand;
        public ICommand AnalysisCommand
        {
            get
            {
                if (analysisCommand == null)
                {
                    analysisCommand = new RelayCommand(param => Analysis_Start());
                }
                return analysisCommand;
            }
        }

        private async void Analysis_Start()
        {
            string outputPath = System.IO.Path.GetDirectoryName(filePath) + "\\edited.mp4";


            CutAndSaveVideo(filePath, outputPath, ST, ET);


            currentWindow.Visibility = Visibility.Collapsed;
            LoadingView loadingView = new LoadingView();
            loadingView.Show();

            UpdateTestLabels();
            await Task.Run(() => YoloProcess.Execute(outputPath));

            loadingView.Close();
            AnalysisView AnalysisView = new AnalysisView();
            AnalysisView.Show();

            OnPropertyChanged(nameof(SetTime));
        }

        static void CutAndSaveVideo(string inputPath, string outputPath, TimeSpan startTime, TimeSpan endTime)
        {
            using (var videoCapture = new VideoCapture(inputPath))
            using (var videoWriter = new VideoWriter(outputPath, FourCC.XVID, videoCapture.Fps, new OpenCvSharp.Size(videoCapture.FrameWidth, videoCapture.FrameHeight)))
            {
                var frame = new Mat();
                var frameIndex = (int)(videoCapture.Fps * startTime.TotalSeconds);

                videoCapture.Set(VideoCaptureProperties.PosFrames, frameIndex);

                while (videoCapture.Read(frame))
                {
                    if (videoCapture.PosFrames >= (int)(videoCapture.Fps * endTime.TotalSeconds))
                        break;

                    videoWriter.Write(frame);
                }
            }
        }

        public void SetCurrentWindow(System.Windows.Window window)
        {
            currentWindow = window;
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

            Videofile = "Video : " + filePath;
            VideoInfo = "Video Length : " + VideoObject.NaturalDuration.TimeSpan + "\n" + "Resolution : " + VideoObject.NaturalVideoWidth + "x" + VideoObject.NaturalVideoHeight;
        }

        private void VideoObject_MediaOpened(object sender, RoutedEventArgs e)
        {
            SliderMinimum = 0;
            SliderMaximum = VideoObject.NaturalDuration.TimeSpan.TotalSeconds;
            SetTime = "00:00:00";
            EndTime = VideoObject.NaturalDuration.ToString();
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

        private bool CanExecutePlayCommand(object parameter)
        {
            return !IsPlaying;
        }

        private void ExecutePauseCommand(object parameter)
        {
            IsPlaying = false;
            VideoObject.Pause();
        }

        private bool CanExecutePauseCommand(object parameter)
        {
            return IsPlaying;
        }

        private void ExecuteStopCommand(object parameter)
        {
            IsPlaying = false;
            VideoObject.Stop();
        }

        private bool CanExecuteStopCommand(object parameter)
        {
            return IsPlaying;
        }
    }
}