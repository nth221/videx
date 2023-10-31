using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using videx.MVVM;
using System.Windows.Input;

namespace videx.MVVM.ViewModels
{
    class NavigationViewModel : ViewModelBase
    {
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ICommand SettingCommand { get; set; }
        public ICommand AnalysisCommand { get; set; }

        private void Setting(object obj) => CurrentView = new SettingViewModel();
        private void Analysis(object obj) => CurrentView = new AnalysisViewModel();


        public NavigationViewModel()
        {
            SettingCommand = new RelayCommand(Setting);
            AnalysisCommand = new RelayCommand(Analysis);

            // Startup Page
            CurrentView = new SettingViewModel();
        }
    }
}
