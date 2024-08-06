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

        private void OnScrollToItem(object sender, SettingViewModel.ScrollEventArgs e)
        {
            /*// Find the container for the item and bring it into view
            var container = CheckBoxList1.ItemContainerGenerator.ContainerFromItem(e.Item) as FrameworkElement;
            container?.BringIntoView();*/
        }

        private void SearchTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            SettingViewModel vm = DataContext as SettingViewModel;
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (SuggestionListBox.SelectedItem != null)
                {
                    vm.SelectedCheckBoxItem = SuggestionListBox.SelectedItem as CheckBoxItem;
                }
            }
        }

        private void SuggestionListBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SettingViewModel vm = DataContext as SettingViewModel;
            if (SuggestionListBox.SelectedItem != null)
            {
                var selectedItem = SuggestionListBox.SelectedItem as CheckBoxItem;
                if (selectedItem != null)
                {
                    selectedItem.IsChecked = true;
                    vm.SelectedCheckBoxItem = selectedItem;
                    vm.IsPopupOpen = false;
                }
            }
        }


    }
}
