using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videx.Model.AnomalyDetection
{
    public class AnomalyData
    {
        [VectorType(1024)]
        public float[]? Features { get; set; }
    }
}
