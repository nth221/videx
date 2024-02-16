using Microsoft.Win32;
using OxyPlot;
using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using videx.ViewModel;
using static OpenCvSharp.Stitcher;

namespace videx.View
{
    /// <summary>
    /// OutlierDetectionView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OutlierDetectionView : Window
    {
        public OutlierDetectionView()
        {
            InitializeComponent();
        }
    private void SldrPlayTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            OutlierDetectionViewModel vm = DataContext as OutlierDetectionViewModel;
            if (vm != null)
            {
                vm.sldrDragStart = true;
                vm.VideoObject.Pause();
            }
        }

        private void SldrPlayTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {

            OutlierDetectionViewModel vm = DataContext as OutlierDetectionViewModel;
            if (vm != null)
            {
                vm.VideoObject.Position = TimeSpan.FromSeconds(vm.SlderPlayTime);
                vm.VideoObject.Pause();

                vm.VideoObject.Play();
                vm.sldrDragStart = false;
            }

        }

        private void SldrPlayTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            OutlierDetectionViewModel vm = DataContext as OutlierDetectionViewModel;
            if (vm != null)
            {
                vm.SlderPlayTime = e.NewValue;
                vm.LblPlayTime = String.Format("{0} / {1}", vm.VideoObject.Position.ToString(@"hh\:mm\:ss"), vm.VideoObject.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss"));
            }
        }

        private void PlotView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition((UIElement)sender);

            double xCoordinate = position.X;

            Console.WriteLine($"Clicked at X-coordinate: {xCoordinate}");
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorMode = "Black";

            Properties.Settings.Default.Save();
        }

        private void LightMode_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ColorMode = "White";

            Properties.Settings.Default.Save();
        }
    }

}