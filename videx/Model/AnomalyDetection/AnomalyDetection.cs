using Microsoft.ML;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using ScottPlot;
using System.Windows.Markup;

namespace videx.Model.AnomalyDetection
{
    public class AnomalyDetection
    {
        private static MLContext mlcontext;
        private static IDataView trainData;
        public static IEnumerable<OutputData> transformedFeatures;
        public static List<double> lofValues;
        public static int endFlag = 0;
        public static void Detection(string inputFilePath, TimeSpan ST)
        {
            Size imgSize = new Size(224, 224);

            string modelPath = "C:\\Users\\VODE-IDX\\Desktop\\AnomalyDetection\\AnomalyDetection\\jm_1.onnx";
            // string videoPath = "C:\\Users\\VODE-IDX\\Desktop\\input video\\car.mp4";

            // 4:54 ~ 5:00  seg 275 ~ 283

            using (var videoCapture = new VideoCapture(inputFilePath))
            {
                int frameCount = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);
                int fps = (int)videoCapture.Fps;
                Console.WriteLine(frameCount);

                int framesPerBatch = 32;
                int cnt = 1;

                mlcontext = new MLContext();
                List<AnomalyData> dataList = new List<AnomalyData>();

                using (var session = new InferenceSession(modelPath))
                {
                    for (int startFrame = (int)ST.TotalSeconds * fps; startFrame < frameCount; startFrame += framesPerBatch)
                    {
                        int currentBatchSize = Math.Min(framesPerBatch, frameCount - startFrame);

                        var inputTensor = new DenseTensor<float>(new int[] { 1, 3, framesPerBatch, imgSize.Height, imgSize.Width });

                        double startTime = startFrame / videoCapture.Fps;

                        for (int i = 0; i < currentBatchSize; i++)
                        {
                            using (var frame = new Mat())
                            {
                                videoCapture.Read(frame);

                                Cv2.Resize(frame, frame, imgSize);

                                CopyImageToTensor(frame, inputTensor, i);
                            }
                        }

                        // Get the timestamp of the last frame in the segment
                        int endFrame = startFrame + currentBatchSize - 1;
                        double endTime = endFrame / videoCapture.Fps;

                        Console.WriteLine($"Segment {startFrame / framesPerBatch + 1}: {startTime} ~ {endTime}");


                        var inputs = new NamedOnnxValue[]
                        {
                        NamedOnnxValue.CreateFromTensor<float>("input", inputTensor)
                        };

                        using (var results = session.Run(inputs))
                        {
                            var outputTensor = results.First().AsTensor<float>();
                            var features = outputTensor.ToArray();

                            dataList.Add(new AnomalyData { Features = features });
                        }
                        cnt++;
                    }
                    ReduceDimension(dataList);
                }
            }
            endFlag = 1;
        }
        public static void ReduceDimension(List<AnomalyData> inputList)
        {
            mlcontext = new MLContext();

            AnomalyData[] data = inputList.ToArray();
            trainData = mlcontext.Data.LoadFromEnumerable(data);

            // Perform PCA transformation
            var pcaTransformer = mlcontext.Transforms.Conversion.MapValueToKey("Features")
                .Append(mlcontext.Transforms.Conversion.MapKeyToValue("Features"))
                .Append(mlcontext.Transforms.NormalizeMinMax("FeaturesNormalized", "Features"))
                .Append(mlcontext.Transforms.ProjectToPrincipalComponents("PCA", "FeaturesNormalized", rank: 2));

            var transformedData = pcaTransformer.Fit(trainData).Transform(trainData);
            transformedFeatures = mlcontext.Data.CreateEnumerable<OutputData>(transformedData, reuseRowObject: false);


            int k = 3; // Number of neighbors
            lofValues = CalculateLOF(data, k);

            /*        Console.WriteLine("LOF Values:");
                    for (int i = 0; i < lofValues.Count; i++)
                    {
                        Console.WriteLine($"Point {i + 1}: {lofValues[i]}");
                    }
            */
            ScottPlot.Plot scatterPlot = new ScottPlot.Plot();

            // Add points with LOF values
            for (int i = 0; i < lofValues.Count; i++)
            {
                double x = i; // X-coordinate
                double y = lofValues[i]; // Y-coordinate (LOF value)

                if (i > 0)
                {
                    double xPrev = i - 1;
                    double yPrev = lofValues[i - 1];
                    scatterPlot.Add.Line(xPrev, yPrev, x, y);
                }

            }

            scatterPlot.Title("LOF Values");
            scatterPlot.XLabel("Data Point");
            scatterPlot.YLabel("LOF Value");

            scatterPlot.Axes.SetLimits(0, lofValues.Count - 1, 0, 1.5);

            // Save the plot to a file or display it
            scatterPlot.SavePng("C:\\Users\\VODE-IDX\\Desktop\\LOF_ScatterPlot.png", 600, 400);
            Console.WriteLine("Scatter plot saved as LOF_ScatterPlot.png");


            /* Console.WriteLine("Transformed Data:");
             foreach (var item in transformedFeatures)
             {
                 Console.WriteLine(string.Join(", ", item.PCA));
             }*/

            var xData = transformedFeatures.Select(item => item.PCA[0]).ToArray();
            var yData = transformedFeatures.Select(item => item.PCA[1]).ToArray();
            var frameNumbers = Enumerable.Range(1, transformedFeatures.Count()).ToArray();

            double maxX = xData.Max();
            double maxY = yData.Max();
            double minX = xData.Min();
            double minY = yData.Min();

            // Create a scatter plot
            ScottPlot.Plot pcaPlot = new();
            for (int i = 0; i < xData.Length; i++)
            {
                pcaPlot.Add.Marker(x: xData[i], y: yData[i]);

                var txt = pcaPlot.Add.Text(frameNumbers[i].ToString(), xData[i] + 0.001, yData[i] + 0.001);
                txt.Label.Rotation = -90;
                txt.Label.Alignment = Alignment.MiddleLeft;
            }

            pcaPlot.Axes.SetLimits(minX, maxX, minY, maxY);

            // Save the plot to a file or display it
            pcaPlot.SavePng("C:\\Users\\VODE-IDX\\Desktop\\pca_scatter.png", 400, 300);
            Console.WriteLine("Scatter plot saved as pca_scatter.png");
        }

        public class OutputData
        {
            [VectorType(2)]
            public float[] PCA { get; set; }
        }

        public class AnomalyData
        {
            [VectorType(1024)]
            public float[]? Features { get; set; }
        }

        static void CopyImageToTensor(Mat image, DenseTensor<float> tensor, int index)
        {
            for (int c = 0; c < 3; c++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    for (int w = 0; w < image.Width; w++)
                    {
                        tensor[0, c, index, h, w] = image.At<Vec3b>(h, w)[c] / 255.0f; // 이미지를 [0, 1] 범위로 정규화
                    }
                }
            }
        }

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
