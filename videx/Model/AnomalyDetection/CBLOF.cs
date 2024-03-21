using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videx.Model.AnomalyDetection
{
    public class CbLOF
    {
        private int _k;
        private double _threshold;

        public CbLOF(int k, double threshold)
        {
            _k = k;
            _threshold = threshold;
        }

        public List<double> Fit(AnomalyData[] data)
        {
            int numInstances = data.Length;
            int numFeatures = data[0].Features.Length;

            List<double> outlierScores = new List<double>();

            // Convert AnomalyData[] to double[][]
            double[][] featureValues = new double[numInstances][];
            for (int i = 0; i < numInstances; i++)
            {
                featureValues[i] = new double[numFeatures];
                for (int j = 0; j < numFeatures; j++)
                {
                    featureValues[i][j] = data[i].Features[j];
                }
            }

            // Calculate distances between points
            double[][] distances = new double[numInstances][];
            for (int i = 0; i < numInstances; i++)
            {
                distances[i] = new double[numInstances];
                for (int j = 0; j < numInstances; j++)
                {
                    distances[i][j] = EuclideanDistance(featureValues[i], featureValues[j]);
                }
            }

            // Calculate reachability distances
            double[][] reachabilityDistances = new double[numInstances][];
            for (int i = 0; i < numInstances; i++)
            {
                reachabilityDistances[i] = new double[numInstances];
                Array.Copy(distances[i], reachabilityDistances[i], numInstances);
                Array.Sort(reachabilityDistances[i]);
            }

            // Calculate local reachability densities
            double[] lrd = new double[numInstances];
            for (int i = 0; i < numInstances; i++)
            {
                double sum = 0.0;
                for (int j = 1; j <= _k; j++)
                {
                    sum += reachabilityDistances[i][j];
                }
                lrd[i] = _k / sum;
            }

            // Calculate local outlier factor
            for (int i = 0; i < numInstances; i++)
            {
                double lrdSum = 0.0;
                for (int j = 0; j < numInstances; j++)
                {
                    if (i != j)
                    {
                        lrdSum += Math.Max(lrd[j] / lrd[i], 1.0);
                    }
                }
                double lof = lrdSum / _k;
                outlierScores.Add(lof);
            }

            return outlierScores;
        }

        private double EuclideanDistance(double[] point1, double[] point2)
        {
            double sum = 0.0;
            for (int i = 0; i < point1.Length; i++)
            {
                sum += Math.Pow(point1[i] - point2[i], 2);
            }
            return Math.Sqrt(sum);
        }
    }
}
