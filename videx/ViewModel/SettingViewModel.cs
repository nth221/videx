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
        public static string filePath;
        public bool sldrDragStart = false;
        public bool check_time = false;
        bool edit_check = false;
        public static TimeSpan ST, ET;
        Stopwatch videoEdit_time = new Stopwatch();
        private System.Windows.Window currentWindow;




        /* Filter Option List */

        public ObservableCollection<string> CheckedItems { get; set; }
        public ObservableCollection<CheckBoxItem> CheckBoxItems { get; set; }



        public SettingViewModel()
        {
            SelectedOptions = new ObservableCollection<string>();
            videoObject = new MediaElement();
            playCommand = new RelayCommand(ExecutePlayCommand);
            pauseCommand = new RelayCommand(ExecutePauseCommand);
            stopCommand = new RelayCommand(ExecuteStopCommand);
            SelectCategoryCommand = new RelayCommand(ExecuteSelect, CanExecuteSelect);


            CheckedItems = new ObservableCollection<string>();

            InitializeCheckBoxItems();

            VideoObject.LoadedBehavior = MediaState.Manual;
            VideoObject.UnloadedBehavior = MediaState.Manual;

            VideoObject.MediaOpened += VideoObject_MediaOpened;
            VideoObject.MediaEnded += VideoObject_MediaEnded;
            VideoObject.MediaFailed += VideoObject_MediaFailed;

            // 각 버튼별로 독립적인 상태를 저장할 변수 초기화
            buttonCheckStates = Enumerable.Repeat(false, 10).ToArray();
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
                Console.WriteLine(CheckBoxItems[i].IsChecked);
            }
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

        private bool _fileSelected;
        public bool FileSelected
        {
            get { return _fileSelected; }
            set
            {
                if (_fileSelected != value)
                {
                    _fileSelected = value;
                    OnPropertyChanged(nameof(FileSelected));

                    if (_fileSelected) //checked
                    {
                        VideoObject.Source = new Uri(filePath);
                        VideoObject.Play();
                        DispatcherTimer timer = new DispatcherTimer()
                        {
                            Interval = TimeSpan.FromSeconds(1)
                        };
                        timer.Tick += TimerTickHandler;
                        timer.Start();
                    }
                    else  //unchecked
                    {
                        VideoObject.Source = null;
                        VideoInfo = null;
                        Videofile = null;
                        SetTime = null;
                        EndTime = null;
                    }
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
            VideoObject.Stop();
            //string outputPath = System.IO.Path.GetDirectoryName(filePath) + "\\edited.mp4";
            string outputPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory) + "\\edited.mp4";


            await Task.Run(() => CutAndSaveVideo(filePath, outputPath, ST, ET));

            currentWindow.Visibility = Visibility.Collapsed;

            UpdateTestLabels();
            ObjectDetectionView ObjectDetectionView = new ObjectDetectionView();
            ObjectDetectionView.Show();

            OutlierDetectionView outlierDetectionView = new OutlierDetectionView();
            outlierDetectionView.Show();

            OnPropertyChanged(nameof(SetTime));
        }

        static void CutAndSaveVideo(string inputPath, string outputPath, TimeSpan startTime, TimeSpan endTime)
        {
            string ffmpegPath = "C:\\Users\\VODE-IDX\\Desktop\\ffmpeg\\bin\\ffmpeg.exe";
            string arguments = $"-i \"{inputPath}\" -ss {startTime} -t {endTime - startTime} -c:v copy -c:a copy \"{outputPath}\"";

            Process process = new Process();
            process.StartInfo.FileName = ffmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
/*
            process.ErrorDataReceived += (s, e) => Console.WriteLine("Error: " + e.Data);
            process.OutputDataReceived += (s, e) => Console.WriteLine("Output: " + e.Data);*/

            process.Start();
            process.WaitForExit();
            /*            using (var videoCapture = new VideoCapture(inputPath))
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
                        }*/
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
    }
}
