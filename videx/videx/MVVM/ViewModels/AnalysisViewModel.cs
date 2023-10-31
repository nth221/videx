using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using videx.MVVM.Model;

namespace videx.MVVM.ViewModels
{
    class AnalysisViewModel : ViewModelBase
    {
        private readonly PageModel _pageModel;
        public string AnalysisValue
        {
            get { return _pageModel.DataPick; }
            set { _pageModel.DataPick = value; OnPropertyChanged(); }
        }

        public AnalysisViewModel()
        {
            _pageModel = new PageModel();
            //AnalysisValue = 100528;
        }
    }
}
