using System;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using OpenCvSharp;


namespace BillySandboxOpenCV
{
    internal class Program
    {
        public static string formatSeconds(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.ToString(@"hh\:mm\:ss\:fff");
        }

        public static Rect parse_dims(string dims_arg)
        {
            var split_dims = dims_arg.Split(',');
            var split_ints = split_dims.Select(s => int.Parse(s)).ToArray();
            return new Rect(split_ints[0], split_ints[1], split_ints[2], split_ints[3]);
        }
        /// <summary>
        /// Retimes a billy hatcher run.
        /// <param name="file">The filename of the video of the run.</param>
        /// <param name="dims">The area, within the footage, that contains the game, specified by x,y,w,h in pixels.
        /// <param name="start">(optional) the time, in seconds, at which to start analyzing the video file.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            string in_fn = args[0];
            string dims = args[1];
            double start = 0;
            if (args.Length > 2)
            {
                start = Double.Parse(args[2]);
            }

            bool debug = false;
            if (args.Contains("--debug"))
            {
                debug = true;
            }
            VideoCapture vidCap = new VideoCapture(in_fn);
            var loadComparison = Cv2.ImRead("loading.png");


            Rect gameRegion = parse_dims(dims);
            Size resolutionStandard = new Size(960, 720);
            Rect loadRegionStandard = new Rect(480, 613, 480, 55); 
            bool isLoading = false;
            double loadThreshold = .1;
            double fps = vidCap.Get(VideoCaptureProperties.Fps);
            int eggWidth = loadRegionStandard.Width * gameRegion.Width / resolutionStandard.Width;
            int eggHeight = loadRegionStandard.Height * gameRegion.Height  / resolutionStandard.Height;
            int eggX = gameRegion.X + loadRegionStandard.X *  gameRegion.Width / resolutionStandard.Width;
            int eggY = gameRegion.Y + loadRegionStandard.Y * gameRegion.Height  / resolutionStandard.Height;
            Rect loadRegion = new Rect(eggX, eggY, eggWidth, eggHeight);
            //resize the comparison image
            loadComparison = loadComparison.Resize(loadRegion.Size);
            InputArray loadArray = InputArray.Create(loadComparison);
            double max_l2_norm = Math.Sqrt(loadRegion.Height * loadRegion.Width *  Math.Pow(255 * Math.Sqrt(3),2)); //math for normalization
            var seek_start = start * 1000;
            double loadFrame = 0.0;
            double totalLoadTime = 0.0;
            double loadStart = 0.0;
            vidCap.Set(VideoCaptureProperties.PosMsec, seek_start); //mim's run starts 17s in ish, user enters this?
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

                
                var loadframe = frame.SubMat(loadRegion);
                InputArray loadframeArray = InputArray.Create(loadframe);
                double l2norm = Cv2.Norm(loadArray, loadframeArray, NormTypes.L2);
                l2norm /= max_l2_norm;



                if (debug)
                {
                    Mat diff = loadframe - loadComparison;
                    Cv2.ImShow("Gaming", diff);
                    if (Cv2.WaitKey(5) >= 0)
                    {
                        break;
                    }

                    Console.WriteLine(l2norm);
                }

                if (isLoading && l2norm > loadThreshold)
                {
                    isLoading = false;
                    double currTime = vidCap.Get(VideoCaptureProperties.PosMsec);
                    Console.WriteLine($"Load ended at {formatSeconds(currTime/1000)} seconds");
                    Console.WriteLine($"L2 Norm: {l2norm} ");
                    totalLoadTime += currTime - loadStart;
                } else if (!isLoading && l2norm <= loadThreshold)
                {
                    isLoading = true;
                    double currTime = vidCap.Get(VideoCaptureProperties.PosMsec);
                    loadStart = currTime;
                    var timeSeconds = TimeSpan.FromMilliseconds(currTime).TotalSeconds;
                    Console.WriteLine($"Load started at {formatSeconds(timeSeconds)} seconds");
                   
                    
                }

            }
         
            Console.WriteLine($"Total Load Time: {formatSeconds(totalLoadTime/1000)}");
        }
    }
}