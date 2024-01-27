using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace videx.Model
{
    public class Prediction
    {
        public Box? Box { get; set; }
        public string? Label { get; set; }

        public int Id { get; set; }
        public float Confidence { get; set; }

        public string[]? SelectedLabels { get; set; }

        public string? FileName { get; set; }
    }




}
