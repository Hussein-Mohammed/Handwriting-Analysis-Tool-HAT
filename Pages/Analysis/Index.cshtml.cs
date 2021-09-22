using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coravel.Queuing.Interfaces;
using HAT3p5.MyLibrary;
using LongRunningSignalr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.Flann;
using OpenCvSharp.XFeatures2D;

namespace HAT3p5.Pages.Analysis
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        public SelectList Options_KeyPoints { get; set; } = new SelectList(new List<string> { "SIFT", "FAST" }, "FAST");
        //public SelectList Options_KeyPoints { get; set; } = new SelectList(GlobalVariables.OptionsOfKeypoints, GlobalVariables.OptionsOfKeypoints[0]);

        [BindProperty]
        public string SelectedKpt { get; set; } = "FAST";

        public static string SelectedKpt_Static { get; set; } = "FAST";
        public static float SelectedParameter { get; set; } = 10;

        public bool ResultsReady { get; set; }

        private IWebHostEnvironment _hostingEnvironment;
        private readonly GlobalVariables _GlobalVariables;
        private readonly IHubContext<JobProgressHub> _hubContext;
        private readonly IQueue _queue;
        public IndexModel(IWebHostEnvironment hostingEnvironment, IQueue queue, IHubContext<JobProgressHub> hubContext, GlobalVariables GlobalVariables)
        {
            _hostingEnvironment = hostingEnvironment;
            _GlobalVariables = GlobalVariables;
            _queue = queue;
            _hubContext = hubContext;
        }

        public void OnGet()
        {
            ViewData["JobId"] = Guid.NewGuid().ToString("N");
            ViewData["AnalysisFinished"] = "False";

            ResultsReady = false;

            foreach (var Style in _GlobalVariables.UnknownImgs)
            {
                Style.Descriptors = "";
                Style.Keypoints.Clear();
            }

            foreach (var Style in _GlobalVariables.KnownImgs)
            {
                Style.Descriptors = "";
                Style.Keypoints.Clear();
            }

            // Delete all existing analysis data
            _GlobalVariables.AllDescs_Known.Release();
            _GlobalVariables.AllKeypoints_Known.Clear();
            _GlobalVariables.AllResults.Clear();
            _GlobalVariables.Keypoints_Num_Known.Clear();
            _GlobalVariables.Labels_Known.Clear();

            // Delete all files with ".xml" extension in the "Unlabelled_Images" and "Labelled_Images" directories
            string Unlabelled_Images = "Unlabelled_Images";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Path_Unlabelled = Path.Combine(webRootPath, Unlabelled_Images);
            if (!Directory.Exists(Path_Unlabelled))
            {
                Directory.CreateDirectory(Path_Unlabelled);
            }

            foreach (var dir in Directory.EnumerateDirectories(Path_Unlabelled).ToList())
            {
                string[] directoryFiles = System.IO.Directory.GetFiles(dir, "*.xml");
                foreach (string directoryFile in directoryFiles)
                {
                    System.IO.File.Delete(directoryFile);
                }
            }

            string Labelled_Images = "Labelled_Images";
            string Path_Labelled = Path.Combine(webRootPath, Labelled_Images);

            foreach (var dir in Directory.EnumerateDirectories(Path_Labelled).ToList())
            {
                string[] directoryFiles = System.IO.Directory.GetFiles(dir, "*.xml");
                foreach (string directoryFile in directoryFiles)
                {
                    System.IO.File.Delete(directoryFile);
                }
            }

            // Delete all files in "Results"
            string ResultPath = webRootPath + "\\Results";
            if (!Directory.Exists(ResultPath))
            {
                Directory.CreateDirectory(ResultPath);
            }
            Directory.EnumerateFiles(ResultPath).ToList().ForEach(f => System.IO.File.Delete(f));

            // Delete all files in "KeypointsImages" directory
            string KeypointsImages = webRootPath + "\\KeypointsImages";
            if (!Directory.Exists(KeypointsImages))
            {
                Directory.CreateDirectory(KeypointsImages);
            }
            Directory.EnumerateFiles(KeypointsImages).ToList().ForEach(f => System.IO.File.Delete(f));
        }

        public void OnPostKptSelection()
        {
            SelectedKpt_Static = SelectedKpt;
            _GlobalVariables.Kpts_Detector = SelectedKpt_Static;
            //var Selection = Request.Form["SelectedKpt"];
            //if(SelectedKpt == "SIFT")
            //    GlobalVariables.Kpts_Detector = "SIFT";
            //if (SelectedKpt == "FAST")
            //    GlobalVariables.Kpts_Detector = "FAST";

        }

        public void OnPostParameter(float Parameter)
        {
            if (SelectedKpt_Static == "SIFT")
                SelectedParameter = (int)Parameter;
            //GlobalVariables.Angle_Diff = (int)Parameter;
            if (SelectedKpt_Static == "FAST")
                SelectedParameter = Parameter;

            _GlobalVariables.Selected_Parameter = Parameter;
        }

        public Task StartAnalysis(string jobId)
        {
            float ProgressPerc = 0;
            _hubContext.Clients.Group(jobId).SendAsync("DetectProgress", Math.Round(ProgressPerc, 0));

            float ProgressShare_FeaturesPerImage = 50.0F / (_GlobalVariables.UnknownImgs.Count() + _GlobalVariables.KnownImgs.Count());
            float ProgressShare_NLNBNN = 50.0F / _GlobalVariables.UnknownImgs.Count();

            // ***************************************
            // Extract features from Unlabelled images
            // ***************************************

            foreach (var Style in _GlobalVariables.UnknownImgs)
            {
                //string HS_Current = new DirectoryInfo(dir).Name;

                //ImgInfo UnknownImg_Current =  GlobalVariables.UnknownImgs.First(m => m.HSName == HS_Current);

                Mat CurrentDescs_Unknown = new Mat();

                var UnlabelledImages = Directory.EnumerateFiles(Style.FilePath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpeg") ||
                s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif") || s.ToLower().EndsWith(".tiff") || s.ToLower().EndsWith(".png")
                || s.ToLower().EndsWith(".bmp"));

                foreach (var img in UnlabelledImages)
                {

                    Mat Current_Image = new Mat(img, ImreadModes.Grayscale);
                    KeyPoint[] Current_Keypoints;
                    List<KeyPoint> Current_Keypoints_List = new List<KeyPoint>();
                    var SIFT_Obj = SIFT.Create();

                    //if (GlobalVariables.Kpts_Detector == "FAST")
                    if (SelectedKpt_Static == "FAST")
                    {
                        var fastCPU = FastFeatureDetector.Create(1, true);
                        Current_Keypoints = fastCPU.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                        Current_Keypoints_List = Current_Keypoints_List.OrderByDescending(x => x.Response).ToList();
                        int SizeOfKeypoints = Current_Keypoints_List.Count();
                        int ConsideredKpts = (int)(SizeOfKeypoints * SelectedParameter / 100.0);
                        //Current_Keypoints_List.Clear();
                        Current_Keypoints_List = Current_Keypoints_List.Take(ConsideredKpts).ToList();
                        Current_Keypoints = Current_Keypoints_List.ToArray();
                    }
                    else //(GlobalVariables.Kpts_Detector == "SIFT")
                    {
                        Current_Keypoints = SIFT_Obj.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                    }

                    Mat CurrentDescs_Unknown_Temp = new Mat();
                    SIFT_Obj.Compute(Current_Image, ref Current_Keypoints, CurrentDescs_Unknown_Temp);
                    CurrentDescs_Unknown.PushBack(CurrentDescs_Unknown_Temp);

                    //TempImg.Descriptors.PushBack(CurrentDescs_Unknown);
                    Style.Keypoints.AddRange(Current_Keypoints_List);
                }

                FileStorage Features_Stream = new FileStorage(Style.FilePath + "/" + Style.HSName + ".xml", FileStorage.Modes.Write);
                Features_Stream.Write("Descs", CurrentDescs_Unknown);
                Features_Stream.ReleaseAndGetString();
                //sCurrentDescs_Unknown.Release();

                Style.Descriptors = Style.FilePath + "/" + Style.HSName + ".xml";

                ProgressPerc += ProgressShare_FeaturesPerImage;
                _hubContext.Clients.Group(jobId).SendAsync("DetectProgress", Math.Round(ProgressPerc, 0));
            }

            // ****************************************
            // Extracting features from Labelled images
            // ****************************************

            int Labels_Counter = 0;
            foreach (var Style in _GlobalVariables.KnownImgs)
            {
                Mat CurrentDescs_Known = new Mat();
                int Keypoints_Num_Known_temp = 0;

                var LabelledImages = Directory.EnumerateFiles(Style.FilePath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpeg") ||
                s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif") || s.ToLower().EndsWith(".tiff") || s.ToLower().EndsWith(".png")
                || s.ToLower().EndsWith(".bmp"));

                foreach (string img in LabelledImages)
                {

                    Mat Current_Image = new Mat(img, ImreadModes.Grayscale);
                    KeyPoint[] Current_Keypoints;
                    List<KeyPoint> Current_Keypoints_List = new List<KeyPoint>();
                    var SIFT_Obj = SIFT.Create();

                    //if (GlobalVariables.Kpts_Detector == "FAST")
                    if (SelectedKpt_Static == "FAST")
                    {
                        var fastCPU = FastFeatureDetector.Create(1, true);
                        Current_Keypoints = fastCPU.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                        Current_Keypoints_List = Current_Keypoints_List.OrderByDescending(x => x.Response).ToList();
                        int SizeOfKeypoints = Current_Keypoints_List.Count();
                        int ConsideredKpts = (int)(SizeOfKeypoints * SelectedParameter / 100.0);
                        //Current_Keypoints_List.Clear();
                        Current_Keypoints_List = Current_Keypoints_List.Take(ConsideredKpts).ToList();
                        Current_Keypoints = Current_Keypoints_List.ToArray();
                    }
                    else //(GlobalVariables.Kpts_Detector == "SIFT")
                    {
                        Current_Keypoints = SIFT_Obj.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                    }

                    Keypoints_Num_Known_temp += Current_Keypoints_List.Count();

                    Mat CurrentDescs_Known_Temp = new Mat();
                    SIFT_Obj.Compute(Current_Image, ref Current_Keypoints, CurrentDescs_Known_Temp);
                    CurrentDescs_Known.PushBack(CurrentDescs_Known_Temp);

                    _GlobalVariables.AllKeypoints_Known.AddRange(Current_Keypoints);

                    Style.Keypoints.AddRange(Current_Keypoints);
                }

                FileStorage Features_Stream = new FileStorage(Style.FilePath + "/" + Style.HSName + ".xml", FileStorage.Modes.Write);
                Features_Stream.Write("Descs", CurrentDescs_Known);
                Features_Stream.ReleaseAndGetString();

                Style.Descriptors = Style.FilePath + "/" + Style.HSName + ".xml";

                _GlobalVariables.Keypoints_Num_Known.Add(Keypoints_Num_Known_temp);
                _GlobalVariables.Labels_Known.AddRange(Enumerable.Repeat(Labels_Counter, CurrentDescs_Known.Rows));
                Labels_Counter++;

                //CurrentDescs_Known.Release();
                ProgressPerc += ProgressShare_FeaturesPerImage;
                _hubContext.Clients.Group(jobId).SendAsync("DetectProgress", Math.Round(ProgressPerc, 0));
            }

            // **********************************************************************
            // Analyse the Labelled and Unlabelled images using Normalised Local NBNN
            // **********************************************************************

            DataTable MyDataTable = new DataTable();
            MyDataTable.Columns.AddRange(new DataColumn[3] { new DataColumn("File", typeof(string)), new DataColumn("Best Match", typeof(string)), new DataColumn("Score", typeof(string)) });

            Mat AllKnownDescs = new Mat();
            foreach (var Img in _GlobalVariables.KnownImgs)
            {
                Mat TempMat = new Mat();
                FileStorage Features_Stream = new FileStorage(Img.Descriptors, FileStorage.Modes.Read);
                TempMat = Features_Stream["Descs"].ReadMat();

                AllKnownDescs.PushBack(TempMat);
                Features_Stream.ReleaseAndGetString();
            }

            KDTreeIndexParams Index_Param = new KDTreeIndexParams(4);
            OpenCvSharp.Flann.Index Descs_Known_Index = new OpenCvSharp.Flann.Index(AllKnownDescs, Index_Param);

            foreach (var Img in _GlobalVariables.UnknownImgs)
            {
                Mat CurrentDescriptors = new Mat();
                FileStorage Features_Stream = new FileStorage(Img.Descriptors, FileStorage.Modes.Read);

                CurrentDescriptors = Features_Stream["Descs"].ReadMat();

                NormalisedLNBNN WriterClassifier = new NormalisedLNBNN();

                // rotate over all the (Unknown) images, and classify them using the function below:
                int Angle_Diff;
                if (SelectedKpt_Static == "SIFT")
                    Angle_Diff = (int)SelectedParameter;
                else
                    Angle_Diff = 10;
                List<LocalNBNN_Results> CurrentResult = new List<LocalNBNN_Results>();
                CurrentResult = WriterClassifier.NNSearch_LNBNN_Priori(CurrentDescriptors, _GlobalVariables.Labels_Known, Descs_Known_Index,
                                                       _GlobalVariables.KnownImgs.Count(), _GlobalVariables.AllKeypoints_Known, Img.Keypoints,
                                                       _GlobalVariables.Keypoints_Num_Known, Angle_Diff);

                CurrentResult = CurrentResult.OrderByDescending(x => x.Votes).ToList();
                //CurrentResult = CurrentResult.OrderBy(x => x.Votes).ToList();

                _GlobalVariables.AllResults.Add(CurrentResult);


                ProgressPerc += ProgressShare_NLNBNN;
                _hubContext.Clients.Group(jobId).SendAsync("DetectProgress", Math.Round(ProgressPerc, 0));

            }

            //GlobalVariables.Results_Ready = true;
            ResultsReady = true;

            // return RedirectToPage("Results");

            //ProgressPerc = 100;
            //_hubContext.Clients.Group(jobId).SendAsync("DetectProgress", Math.Round(ProgressPerc, 0));

            return Task.CompletedTask;

        }

        public void OnPostAnalyse()
        {
            string jobId = Guid.NewGuid().ToString("N");
            //_queue.QueueAsyncTask(() => DetectPatterns(jobId));
            _queue.QueueTask(() => StartAnalysis(jobId));

            ViewData["JobId"] = jobId;

        }
    }
}