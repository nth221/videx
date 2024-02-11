using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using videx.ViewModel;

namespace videx.View
{
    /// <summary>
    /// AnalysisView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AnalysisView : Window
    {
        public AnalysisView()
        {
            InitializeComponent();
        }
        private void SldrPlayTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            // 사용자가 시간대를 변경하면, 잠시 미디어 재생을 멈춘다.
            AnalysisViewModel vm = DataContext as AnalysisViewModel;
            if (vm != null)
            {
                vm.sldrDragStart = true;
                vm.VideoObject.Pause();
            }
        }

        private void SldrPlayTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {

            AnalysisViewModel vm = DataContext as AnalysisViewModel;
            if (vm != null)
            {
                // 사용자가 지정한 시간대로 이동하면, 이동한 시간대로 값을 지정한다.
                vm.VideoObject.Position = TimeSpan.FromSeconds(vm.SlderPlayTime);
                vm.VideoObject.Pause();

                // 멈췄던 미디어를 재실행한다.
                vm.VideoObject.Play();
                vm.sldrDragStart = false;
            }

        }

        private void SldrPlayTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ViewModel의 SlderPlayTime 속성 업데이트
            AnalysisViewModel vm = DataContext as AnalysisViewModel;
            if (vm != null)
            {
                vm.SlderPlayTime = e.NewValue;
                vm.LblPlayTime = String.Format("{0} / {1}", vm.VideoObject.Position.ToString(@"hh\:mm\:ss"), vm.VideoObject.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss"));
            }
        }
    }
}
