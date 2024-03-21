using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videx.Model.AnomalyDetection
{
    public class LOF
    {
        private static double EuclideanDistance(AnomalyData point1, AnomalyData point2)
        {
            double sum = 0.0;
            for (int i = 0; i < point1.Features.Length; i++)
            {
                sum += Math.Pow(point1.Features[i] - point2.Features[i], 2);
            }
            return Math.Sqrt(sum);
        }

        // Find k-nearest neighbors of a point
        private static List<int> FindNeighbors(AnomalyData[] input, int pointIndex, int k)
        {
            var distances = new Dictionary<int, double>();
            for (int i = 0; i < input.Length; i++)
            {
                if (i == pointIndex) continue; // Skip the point itself
                double distance = EuclideanDistance(input[pointIndex], input[i]);
                distances.Add(i, distance);
            }
            return distances.OrderBy(x => x.Value).Take(k).Select(x => x.Key).ToList();
        }

        // Calculate reachability distance
        private static double ReachabilityDistance(AnomalyData[] input, int pointIndex, int neighborIndex, int k)
        {
            var neighbors = FindNeighbors(input, pointIndex, k);
            double dist = EuclideanDistance(input[pointIndex], input[neighborIndex]);
            double kthDist = neighbors.Max(n => EuclideanDistance(input[pointIndex], input[n]));
            return Math.Max(dist, kthDist);
        }

        // Calculate local reachability density
        private static double LocalReachabilityDensity(AnomalyData[] input, int pointIndex, int k)
        {
            var neighbors = FindNeighbors(input, pointIndex, k);
            double sum = 0.0;
            foreach (int neighborIndex in neighbors)
            {
                sum += ReachabilityDistance(input, pointIndex, neighborIndex, k);
            }
            return neighbors.Count == 0 ? 0 : sum / neighbors.Count;
        }

        // Calculate local outlier factor
        public static List<double> CalculateLOF(AnomalyData[] input, int k)
        {
            var lofValues = new List<double>();
            for (int i = 0; i < input.Length; i++)
            {
                double lrdPoint = LocalReachabilityDensity(input, i, k);
                var neighbors = FindNeighbors(input, i, k);
                double sum = 0.0;
                foreach (int neighborIndex in neighbors)
                {
                    double lrdNeighbor = LocalReachabilityDensity(input, neighborIndex, k);
                    sum += lrdNeighbor / lrdPoint;
                }
                double lof = sum / neighbors.Count;
                lofValues.Add(lof);
            }
            return lofValues;
        }
    }
}
