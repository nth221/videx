using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace videx.MVVM.Views
{
    /// <summary>
    /// SettingView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingView : UserControl
    {
        public ObservableCollection<string> FileList { get; set; }
        bool sldrDragStart = false;
        string selectedFileName;

        public SettingView()
        {
            InitializeComponent();
            FileList = new ObservableCollection<string>();

            // 부모 체크박스의 Checked 이벤트에 이벤트 핸들러를 연결
            //ParentCheckBox.Checked += ParentCheckBox_Checked;

            //fileListBox.ItemSource = FileList;
        }

        /* private void ParentCheckBox_Checked(object sender, RoutedEventArgs e)
         {
             // 부모 체크박스가 체크되었을 때 호출되는 이벤트 핸들러

             bool isChecked = ParentCheckBox.IsChecked ?? false;

             // 하위 체크박스들을 찾습니다. 이 예제에서는 StackPanel 안에 있는 모든 체크박스
             FindCheckBoxes(StackPanel1, isChecked);
         }

         // StackPanel 내의 모든 하위 체크박스를 찾아서 체크
         private void FindCheckBoxes(Panel panel, bool isChecked)
         {
             foreach (UIElement child in panel.Children)
             {
                 if (child is CheckBox checkBox)
                 {
                     checkBox.IsChecked = isChecked;
                 }
                 else if (child is Panel nestedPanel)
                 {
                     // 중첩된 StackPanel과 같은 다른 패널이 있는 경우 재귀적으로 호출
                     FindCheckBoxes(nestedPanel, isChecked);
                 }
             }
         }*/

        private void Button_FileRemove(object sender, RoutedEventArgs e)
        {
            
            MessageBoxResult result = MessageBox.Show("선택한 항목을 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                mediaMain.Source = null;
                // checkBox1.IsChecked = false; // 체크 박스 상태를 변경하여 체크 해제
                // checkBox2.IsChecked = false; // 다른 체크 박스도 체크 해제

                // 이제 선택한 항목을 삭제할 수 있습니다.
            }
        }

        private void Button_FileAdd(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void MediaMain_MediaOpened(object sender, RoutedEventArgs e)
        {
            // 미디어 파일이 열리면, 플레이타임 슬라이더의 값을 초기화 한다.
            sldrPlayTime.Minimum = 0;
            sldrPlayTime.Maximum = mediaMain.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void MediaMain_MediaEnded(object sender, RoutedEventArgs e)
        {
            // 미디어 중지
            mediaMain.Stop();
        }

        private void MediaMain_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // 미디어 파일 실행 오류시
            MessageBox.Show("동영상 재생 실패 : " + e.ErrorException.Message.ToString());
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            // Win32 DLL 을 사용하여 선택할 파일 다이얼로그를 실행한다.
            OpenFileDialog dlg = new OpenFileDialog()
            {
                DefaultExt = ".avi",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {

                // 선택한 파일을 Media Element에 지정하고 초기화한다.
                mediaMain.Source = new Uri(dlg.FileName);
                mediaMain.Volume = 0.5;
                mediaMain.SpeedRatio = 1;


                // 선택한 파일의 이름
                selectedFileName = dlg.FileName;

                mediaMain.Source = new Uri(selectedFileName);
                mediaMain.LoadedBehavior = MediaState.Manual;

               /* // MediaOpened 이벤트를 통해 영상 정보 획득
                mediaMain.MediaOpened += (sender, e) =>
                {
                    // 영상의 길이 가져오기 (초 단위)
                    double videoLength = mediaMain.NaturalDuration.TimeSpan.TotalSeconds;

                    // 슬라이더의 최댓값 설정
                    videoLengthSlider.Maximum = videoLength;

                    // 영상 정보를 표시
                    videoInfoText.Text = $"영상 정보: {Path.GetFileName(selectedFileName)} ({videoLength} 초)";
                };*/

                // 동영상 파일의 Timespan 제어를 위해 초기화와 이벤트처리기를 추가한다.
                DispatcherTimer timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += TimerTickHandler;
                timer.Start();

                // 선택한 파일을 실행
                mediaMain.Play();

            }
        }

        // 미디어파일 타임 핸들러
        // 미디어파일의 실행시간이 변경되면 호출된다.
        void TimerTickHandler(object sender, EventArgs e)
        {
            // 미디어파일 실행시간이 변경되었을 때 사용자가 임의로 변경하는 중인지를 체크한다.
            if (sldrDragStart)
                return;

            if (mediaMain.Source == null
                || !mediaMain.NaturalDuration.HasTimeSpan)
            {
                lblPlayTime.Content = "No file selected...";
                return;
            }

            // 미디어 파일 총 시간을 슬라이더와 동기화한다.
            sldrPlayTime.Value = mediaMain.Position.TotalSeconds;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null)
                return;

            mediaMain.Play();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null)
                return;

            mediaMain.Stop();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (mediaMain.Source == null)
                return;

            mediaMain.Pause();
        }

        private void SldrVolume_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {

        }

        private void SldrVolume_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // 사용자가 변경한 볼륨값으로 미디어 볼륨값을 변경한다.
            mediaMain.Volume = sldrPlayTime.Value;
        }

        private void SldrPlayTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            // 사용자가 시간대를 변경하면, 잠시 미디어 재생을 멈춘다.
            sldrDragStart = true;
            mediaMain.Pause();
        }

        private void SldrPlayTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // 사용자가 지정한 시간대로 이동하면, 이동한 시간대로 값을 지정한다.
            mediaMain.Position = TimeSpan.FromSeconds(sldrPlayTime.Value);

            // 멈췄던 미디어를 재실행한다.
            mediaMain.Play();
            sldrDragStart = false;
        }

        private void SldrPlayTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaMain.Source == null)
                return;

            // 플레이시간이 변경되면, 표시영역을 업데이트한다.
            lblPlayTime.Content = String.Format("{0} / {1}", mediaMain.Position.ToString(@"mm\:ss"), mediaMain.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
        }

        private void checkBoxListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {

        }
    }

}
