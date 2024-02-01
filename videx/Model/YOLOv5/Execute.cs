using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.Data.SQLite;
using Image = System.Drawing.Image;
using Point = OpenCvSharp.Point;
using System.IO;
using OpenCvSharp.Extensions;
using Dapper;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using videx.Model;


namespace videx.Model.YOLOv5
{
    public class YoloProcess
    {
        // private static string expDir;
        public static int flag;
        private static string strConn = "Data Source=mydatabase.db;Version=3;Mode=Serialized;";



        public static void Execute(string inputFilePath)
        {
            // DB Connection & Create Table
            CreateTable();

            //var detector = new YoloDetector("C:\\Users\\psy\\Desktop\\yolov5s.onnx");

            string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
            string yolov5sPath = System.IO.Path.Combine(desktopPath, "yolov5s.onnx");
            var detector = new YoloDetector(yolov5sPath);

            List<string> outputFilePaths = new List<string>();

            //string final_path = "C:\\Users\\psy\\Desktop\\output\\Thread1\\output_video.avi";

            string output_path = System.IO.Path.Combine(desktopPath, "output", "Thread");
            string final_path = System.IO.Path.Combine(desktopPath, "output", "Thread", "output_video.avi");

            Stopwatch stopwatch = new Stopwatch();

            int maxThreadCount = Environment.ProcessorCount;

            var capture = new VideoCapture(inputFilePath);

            var totalFrames = (int)capture.Get(VideoCaptureProperties.FrameCount);

            int segmentSize = totalFrames / maxThreadCount;

            int diff = totalFrames - segmentSize * maxThreadCount;

            int[] start = new int[maxThreadCount];
            int[] end = new int[maxThreadCount];
            Console.WriteLine($"Maximum thread count for this system: {maxThreadCount}, totalFrames : {totalFrames}");

            for (int i = 0; i < maxThreadCount; i++)
            {
                start[i] = i * segmentSize;
                end[i] = (i + 1) * segmentSize;
            }



            stopwatch.Start();

            ThreadLocal<SQLiteConnection> threadLocalConnection = new ThreadLocal<SQLiteConnection>(() =>
            {
                SQLiteConnection connection = new SQLiteConnection(strConn);
                connection.Open();
                return connection;
            });

            Thread[] threads = new Thread[maxThreadCount];

            using (SQLiteConnection connection = new SQLiteConnection(strConn))
            {
                try
                {
                    connection.Open();

                    for (int i = 0; i < maxThreadCount; i++)
                    {
                        int startFrame = i * segmentSize;
                        int endFrame = (i + 1) * segmentSize;
                        threads[i] = new Thread(() =>
                        {
                            using (var connection = threadLocalConnection.Value)
                            {
                                DoSomething(connection, output_path, detector, inputFilePath, outputFilePaths, startFrame, endFrame);
                            }
                        });
                        Console.WriteLine($"Start Frame : {startFrame}, End Frame : {endFrame}, Adder : {segmentSize}");
                    }

                    Console.WriteLine("DB Connection is Good and Create ImageTable");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Console.WriteLine("Start threads...");

            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Finished");

            var sortedFilePaths = outputFilePaths.OrderBy(path => ExtractNumberFromFileName(path));

            var outputFilePath = sortedFilePaths.ToList();

            //string imagePath = expDir;

            //UpdateAllImage(imagePath);
            CombineVideo(outputFilePath, final_path);

            stopwatch.Stop();
            Console.WriteLine("time : " +
                           stopwatch.ElapsedMilliseconds + "ms");
        }



        private static void DoSomething(SQLiteConnection connection, string ouput_path, YoloDetector detector, string inputFilePath, List<string> outputFilePaths, int startFrame, int endFrame)
        {
            string outputFileName = Path.Combine(ouput_path, $"output_{startFrame}-{endFrame}.avi");
            //Image croppedImage = null;

            if (!Directory.Exists(ouput_path))
            {
                Directory.CreateDirectory(ouput_path);
                Console.WriteLine($"Directory '{ouput_path}'is created.");
            }

/*            int expNumber = 1;

            do
            {
                expDir = Path.Combine(ouput_path, $"exp{expNumber}");
                expNumber++;
            }
            while (Directory.Exists(expDir));*/

            //Directory.CreateDirectory(expDir);

            using (var videoCapture = new VideoCapture(inputFilePath))
            {
                if (!videoCapture.IsOpened())
                {
                    Console.WriteLine("Can not open video.");
                    return;
                }

                double fps = videoCapture.Get(VideoCaptureProperties.Fps);
                int width = (int)videoCapture.Get(VideoCaptureProperties.FrameWidth);
                int height = (int)videoCapture.Get(VideoCaptureProperties.FrameHeight);

                videoCapture.Set(VideoCaptureProperties.PosFrames, startFrame);

                VideoWriter videoWriter = new VideoWriter(outputFileName, FourCC.XVID, fps, new OpenCvSharp.Size(width, height));
                if (!videoWriter.IsOpened())
                {
                    Console.WriteLine("VideoWriter open failed!");
                    return;
                }

                for (int frameNumber = startFrame; frameNumber < endFrame; frameNumber++)
                {

                    using (var frame = new Mat())
                    {
                        videoCapture.Read(frame);

                        if (frame.Empty())
                            break;

                        var result = detector.objectDetection(frame);

                        List<Image> croppedImagesList = new List<Image>();

                        using (var dispImage = frame.Clone())
                        {
                            foreach (var obj in result)
                            {
                                if (obj.Label != null)
                                {
                                    Cv2.Rectangle(dispImage, new Point(obj.Box.Xmin, obj.Box.Ymin), new Point(obj.Box.Xmax, obj.Box.Ymax), new Scalar(0, 0, 255), 1);
                                    Cv2.PutText(dispImage, obj.Label, new Point(obj.Box.Xmin, obj.Box.Ymin - 5), HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 0, 255), 1);
                                    Cv2.PutText(dispImage, obj.Confidence.ToString("F2"), new Point(obj.Box.Xmin, obj.Box.Ymin + 8), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 0, 0), 1);

                                    Rectangle cropArea = new Rectangle((int)obj.Box.Xmin, (int)obj.Box.Ymin, (int)(obj.Box.Xmax - obj.Box.Xmin), (int)(obj.Box.Ymax - obj.Box.Ymin));
                                    Image croppedImage = Crop.cropImage(frame.ToBitmap(), cropArea);
                                    if (croppedImage != null)
                                    {
                                        byte[] imgBytes = ImageToByteArray(croppedImage);
                                        connection.Execute("INSERT INTO ImageTable (Class, Frame, ImageBytes) VALUES (@Class, @Frame, @ImageBytes)", new { Class = obj.Label, Frame = frameNumber, ImageBytes = imgBytes });
                                        croppedImage.Dispose();
                                    }
                                }
                            }

                            videoWriter.Write(dispImage);
                        }
                    }
                }

                videoWriter.Release();
                Console.WriteLine($"Video splitting completed for frames {startFrame} to {endFrame}.");
                outputFilePaths.Add(outputFileName);
                Console.WriteLine($"Video file created: {outputFileName}");
            }
        }

        static byte[] ImageToByteArray(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        static int ExtractNumberFromFileName(string fileName)
        {
            var match = Regex.Match(fileName, @"output_(\d+)-?(\d*).avi", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                int startNumber = int.Parse(match.Groups[1].Value);
                return startNumber;
            }

            return 0;
        }


        private static void CreateTable()
        {
            using (SQLiteConnection connection = new SQLiteConnection(strConn))
            {

                try
                {
                    connection.Open();

                    connection.Execute("DROP TABLE IF EXISTS ImageTable");
                    connection.Execute("CREATE TABLE ImageTable (Id INTEGER PRIMARY KEY AUTOINCREMENT,Class TEXT, Frame INTEGER, ImageBytes BLOB)");

                    Console.WriteLine("DB Connection is Good and Create ImageTable");
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

/*        private static void UpdateAllImage(string directoryPath)
        {
            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(strConn))
                {
                    connection.Open();

                    string[] pngFiles = Directory.GetFiles(directoryPath, "*.png");

                    for (int i = 0; i < pngFiles.Length; i++)
                    {
                        byte[] imageBytes = File.ReadAllBytes(pngFiles[i]);

                        string fileName = Path.GetFileNameWithoutExtension(pngFiles[i]);
                        string[] fileNameParts = fileName.Split('_');

                        if (fileNameParts.Length == 2 && int.TryParse(fileNameParts[0], out int objFrame))
                        {
                            string objClass = fileNameParts[1];

                            var imageData = new ImageData
                            {
                                ImageBytes = imageBytes,
                            };

                            connection.Execute("INSERT INTO ImageTable (Class, Frame, ImageBytes) VALUES (@Class, @Frame, @ImageBytes)",
                                new { Class = objClass, Frame = objFrame, imageData.ImageBytes });
                        }
                        else
                        {
                            Console.WriteLine($"Invalid file name format: {pngFiles[i]}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }*/

        public static List<ImageData> SelectImage()
        {
            List<ImageData> imageList = new List<ImageData>();

            using (SQLiteConnection connection = new SQLiteConnection(strConn))
            {
                connection.Open();

                string[] targetClasses = LabelMap.test_Labels;

                imageList = connection.Query<ImageData>("SELECT Id, Class, Frame, ImageBytes FROM ImageTable WHERE Class IN @TargetClasses", new { TargetClasses = targetClasses }).ToList();

                foreach (var imageData in imageList)
                {
                    using (var stream = new MemoryStream(imageData.ImageBytes))
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();

                        imageData.Bitmap = bitmapImage;
                    }
                }
            }

            return imageList;
        }

        public static void CombineVideo(List<string> inputVideoPaths, string outputDirectory)
        {
            string[] VideoPaths = inputVideoPaths.ToArray();

            MergeVideos(VideoPaths, outputDirectory);

            Console.WriteLine("Video merging completed.");
            flag = 1;
        }

        static void MergeVideos(string[] inputVideoPaths, string outputVideoPath)
        {
            VideoCapture firstVideoCapture = new VideoCapture(inputVideoPaths[0]);
            int width = firstVideoCapture.FrameWidth;
            int height = firstVideoCapture.FrameHeight;
            double fps = firstVideoCapture.Fps;

            VideoWriter videoWriter = new VideoWriter(outputVideoPath, FourCC.XVID, fps, new OpenCvSharp.Size(width, height));

            foreach (var inputVideoPath in inputVideoPaths)
            {
                VideoCapture videoCapture = new VideoCapture(inputVideoPath);

                while (true)
                {
                    Mat frame = new Mat();
                    videoCapture.Read(frame);
                    if (frame.Empty())
                        break;

                    videoWriter.Write(frame);
                }

                videoCapture.Release();
            }

            videoWriter.Release();
            var video = new VideoCapture(outputVideoPath);
            var totalFrames = (int)video.Get(VideoCaptureProperties.FrameCount);
        }
    }
}
