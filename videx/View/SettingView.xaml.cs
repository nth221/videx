using Microsoft.Win32;
using System;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using videx.Model.YOLOv5;
using System.Collections.Generic;
using videx.ViewModel;
using videx.View;
using System.Windows.Media;

namespace videx.View
{
    /// <summary>
    /// SettingView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingView : Window
    {
        public SettingView()
        {
            InitializeComponent();

            // ViewModel --> View : Currentview 종료 명령
            SettingViewModel viewModel = new SettingViewModel();
            viewModel.SetCurrentWindow(this);

            DataContext = viewModel;
        }


        private void SldrPlayTime_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            // 사용자가 시간대를 변경하면, 잠시 미디어 재생을 멈춘다.
            SettingViewModel vm = DataContext as SettingViewModel;
            if (vm != null)
            {
                vm.sldrDragStart = true;
                vm.VideoObject.Pause();
            }
        }

        private void SldrPlayTime_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            SettingViewModel vm = DataContext as SettingViewModel;
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
            SettingViewModel vm = DataContext as SettingViewModel;
            if (vm != null)
            {
                vm.SlderPlayTime = e.NewValue;
                vm.LblPlayTime = String.Format("{0} / {1}", vm.VideoObject.Position.ToString(@"hh\:mm\:ss"), vm.VideoObject.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss"));
            }
        }

        private void mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedMode = (mode.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedMode == "Dark Mode")
            {
                backgroundMode.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#272537"));
                backgroundMode1.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode2.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode3.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode4.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode5.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode6.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#323345"));
                backgroundMode7.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#272537"));
                backgroundMode8.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#272537"));
            }
            else
            {
                backgroundMode.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ffffff"));
                backgroundMode1.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode2.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode3.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode4.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode5.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode6.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode7.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
                backgroundMode8.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F2F2F2"));
            }
        }

    }
}
