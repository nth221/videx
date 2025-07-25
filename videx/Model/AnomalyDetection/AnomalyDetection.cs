﻿using Microsoft.ML;
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
        public static int endFlag = 0;
        private static int framesPerBatch = 32;
        private static int frameCount;

        public static List<double> lofValues;
        public static List<double> cblofValues;
        public static List<double> iForestValues;


        public static void Detection()
        {
            Size imgSize = new Size(224, 224);

            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string modelPath = System.IO.Path.Combine(desktopPath, "S3D.onnx");
            string videoPath = System.IO.Path.Combine(desktopPath, "edited.mp4");

            // 4:54 ~ 5:00  seg 275 ~ 283
            using (var videoCapture = new VideoCapture(videoPath))
            {
                frameCount = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);
                int fps = (int)videoCapture.Fps;
                // Console.WriteLine(frameCount);

                mlcontext = new MLContext();
                List<AnomalyData> dataList = new List<AnomalyData>();

                using (var session = new InferenceSession(modelPath))
                {
                    for (int startFrame = 0; startFrame < frameCount; startFrame += framesPerBatch)
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
                    }
                    ReduceDimension(dataList);
                }
            }
        }
        public static void ReduceDimension(List<AnomalyData> inputList)
        {
            mlcontext = new MLContext();

            AnomalyData[] data = inputList.ToArray();
            trainData = mlcontext.Data.LoadFromEnumerable(data);

            /*// Perform PCA transformation
            var pcaTransformer = mlcontext.Transforms.Conversion.MapValueToKey("Features")
                .Append(mlcontext.Transforms.Conversion.MapKeyToValue("Features"))
                .Append(mlcontext.Transforms.NormalizeMinMax("FeaturesNormalized", "Features"))
                .Append(mlcontext.Transforms.ProjectToPrincipalComponents("PCA", "FeaturesNormalized", rank: 2));

            var transformedData = pcaTransformer.Fit(trainData).Transform(trainData);
            transformedFeatures = mlcontext.Data.CreateEnumerable<OutputData>(transformedData, reuseRowObject: false);*/

            // LOF
            int k = 20;
            lofValues = LOF.CalculateLOF(data, k);
            DrawLOFGraph(lofValues);

            // CBLOF
            // int k = 3;
            double threshold = 0.5; // 1.5;
            CbLOF cblof = new CbLOF(k, threshold);
            cblofValues = cblof.Fit(data);
            DrawcbLOFGraph(cblofValues);


            // Isolation Forest
            int numTrees = frameCount / framesPerBatch;
            int maxTreeDepth = 10;

            IsolationForest iforest = new IsolationForest(numTrees, maxTreeDepth);
            iForestValues = iforest.Train(data);
            DrawiForestGraph(iForestValues);


            endFlag = 1;

            /*Console.WriteLine("LOF Values:");
            for (int i = 0; i < lofValues.Count; i++)
            {
                Console.WriteLine($"Point {i + 1}: {lofValues[i]}");
            }*/




            /*Console.WriteLine("Transformed Data:");
            foreach (var item in transformedFeatures)
            {
                Console.WriteLine(string.Join(", ", item.PCA));
            }

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
            pcaPlot.SavePng("C:\\Users\\psy\\Desktop\\pca_scatter.png", 400, 300);
            Console.WriteLine("Scatter plot saved as pca_scatter.png");*/
        }

        public class OutputData
        {
            [VectorType(2)]
            public float[] PCA { get; set; }
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

        static void DrawLOFGraph(List<double> lofValues)
        {
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

            scatterPlot.Axes.SetLimits(0, lofValues.Count - 1, 0.8, 1.5);

            scatterPlot.SavePng("C:\\Users\\psy\\Desktop\\LOF_ScatterPlot.png", 600, 400);
            Console.WriteLine("Scatter plot saved as LOF_ScatterPlot.png");
        }

        static void DrawcbLOFGraph(List<double> cblofValues)
        {
            ScottPlot.Plot scatterPlot = new ScottPlot.Plot();

            // Add points with LOF values
            for (int i = 0; i < cblofValues.Count; i++)
            {
                double x = i; // X-coordinate
                double y = cblofValues[i]; // Y-coordinate (LOF value)

                if (i > 0)
                {
                    double xPrev = i - 1;
                    double yPrev = cblofValues[i - 1];
                    scatterPlot.Add.Line(xPrev, yPrev, x, y);
                }

            }

            scatterPlot.Title("cbLOF Values");
            scatterPlot.XLabel("Data Point");
            scatterPlot.YLabel("cbLOF Value");

            scatterPlot.Axes.SetLimits(0, cblofValues.Count - 1, 0.8, 1.5);

            scatterPlot.SavePng("C:\\Users\\psy\\Desktop\\cbLOF_ScatterPlot.png", 600, 400);
            Console.WriteLine("Scatter plot saved as cbLOF_ScatterPlot.png");
        }

        static void DrawiForestGraph(List<double> iForestValues)
        {
            ScottPlot.Plot scatterPlot = new ScottPlot.Plot();

            // Add points with LOF values
            for (int i = 0; i < iForestValues.Count; i++)
            {
                double x = i; // X-coordinate
                double y = iForestValues[i]; // Y-coordinate (LOF value)

                if (i > 0)
                {
                    double xPrev = i - 1;
                    double yPrev = iForestValues[i - 1];
                    scatterPlot.Add.Line(xPrev, yPrev, x, y);
                }

            }

            scatterPlot.Title("iForest Values");
            scatterPlot.XLabel("Data Point");
            scatterPlot.YLabel("iForest Value");

            scatterPlot.Axes.SetLimits(0, iForestValues.Count - 1, 0, 1.5);

            scatterPlot.SavePng("C:\\Users\\psy\\Desktop\\iForest_ScatterPlot.png", 600, 400); // User names -> VODE-IDX
            Console.WriteLine("Scatter plot saved as iForest_ScatterPlot.png");
        }
    }
}