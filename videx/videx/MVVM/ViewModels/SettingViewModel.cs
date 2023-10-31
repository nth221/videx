using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using videx.MVVM;
using videx.MVVM.Model;

namespace videx.MVVM.ViewModels
{
    class SettingViewModel : ViewModelBase
    {
        private readonly PageModel _pageModel;
        public int Dataoption
        {
            get { return _pageModel.SettingValue; }
            set { _pageModel.SettingValue = value; OnPropertyChanged(); }
        }

        public SettingViewModel()
        {
            _pageModel = new PageModel();
            Dataoption = 100528;
        }
    }
}
