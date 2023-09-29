using System;
using System.Security.AccessControl;
using OpenCvSharp;


namespace BillySandboxOpenCV
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {
            var in_fn = "monkey_103.mp4";
            //double seek_start_ms = 80.0 * 1000;
            double load_frame_start_ms = 85.0 * 1000;
            VideoCapture vidCap = new VideoCapture(in_fn);
            var loadComparison = Cv2.ImRead("loading.png");
            InputArray loadArray = InputArray.Create(loadComparison);
            
            Rect gameRegion = new Rect(320, 0, 960, 720); //these are monkey's dimensions
            Rect loadRegion = new Rect(480, 613, 480, 55); //un hard-code later
            bool isLoading = false;
            double loadThreshold = .1;
            double fps = vidCap.Get(VideoCaptureProperties.Fps);
            double loadFrames = 0.0;
            double totalLoadTime = 0.0;
            double loadStart = 0.0;
            vidCap.Set(VideoCaptureProperties.PosMsec, 4000);
            if (!vidCap.IsOpened())
            {
                Console.WriteLine("OOPS");
                Console.ReadLine();
            }

            Mat frame = new Mat();
            while (vidCap.IsOpened())
            {
                vidCap.Read(frame);
                if (frame.Empty())
                {
                    Console.WriteLine("frame empty :(");
                    Console.WriteLine(vidCap.Get(VideoCaptureProperties.PosMsec) * 1000.0);
                    break;
                }

                var gameframe = frame.SubMat(gameRegion);
                var loadframe = gameframe.SubMat(loadRegion);
                InputArray loadframeArray = InputArray.Create(loadframe);
                double l2norm = Cv2.Norm(loadArray, loadframeArray, NormTypes.L2);
                l2norm /= loadframe.Width * loadframe.Height * 255;
                l2norm *= 100;
                if (isLoading && l2norm > loadThreshold)
                {
                    isLoading = false;
                    double currTime = vidCap.Get(VideoCaptureProperties.PosMsec);
                    Console.WriteLine($"Load ended at {currTime/1000} seconds");
                    Console.WriteLine($"L2 Norm: {l2norm} ");
                    totalLoadTime += currTime - loadStart;
                } else if (!isLoading && l2norm <= loadThreshold)
                {
                    isLoading = true;
                    double currTime = vidCap.Get(VideoCaptureProperties.PosMsec);
                    loadStart = currTime;
                    var timeSeconds = TimeSpan.FromMilliseconds(currTime).TotalSeconds;
                    Console.WriteLine($"Load started at {timeSeconds} seconds");
                   
                    
                }

            }
            Console.WriteLine(vidCap.IsOpened());
            Console.WriteLine($"Total Load Time: {totalLoadTime/1000}");
        }
    }
}