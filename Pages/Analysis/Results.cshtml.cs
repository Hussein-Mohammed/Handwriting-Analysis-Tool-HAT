using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HAT3p5.MyLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;

namespace HAT3p5.Pages.Analysis
{
    [IgnoreAntiforgeryToken]
    public class ResultsModel : PageModel
    {
        public bool DeleteClicked { get; set; }

        public List<string> ResultFiles = new List<string>();

        private IWebHostEnvironment _hostingEnvironment;
        private readonly GlobalVariables _GlobalVariables;
        public ResultsModel(IWebHostEnvironment hostingEnvironment, GlobalVariables GlobalVariables)
        {
            _hostingEnvironment = hostingEnvironment;
            _GlobalVariables = GlobalVariables;
        }

        public void OnGet()
        {
            DeleteClicked = false;

            string folderName = "Results";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(newPath))
            {
                Directory.CreateDirectory(newPath);
            }

            var Results_csv = new StringBuilder();

            int ImgCounter = 0;
            foreach (var Img in _GlobalVariables.UnknownImgs)
            {
                Results_csv.AppendLine("Results for " + Img.HSName);
                Results_csv.AppendLine("Rank,Directory,Score");

                int RankCounter = 1;
                foreach (var Res in _GlobalVariables.AllResults[ImgCounter])
                {
                    Results_csv.AppendLine(Convert.ToString(RankCounter++) + "," + _GlobalVariables.KnownImgs[Res.Label].HSName + "," + Convert.ToString(Math.Round(Res.Votes, 1)));
                }
                Results_csv.AppendLine("");

                ImgCounter++;

                System.IO.File.WriteAllText(newPath + "/" + Img.HSName + ".csv", Results_csv.ToString());

                Results_csv.Clear();

                List<string> temp = new List<string>(Directory.EnumerateFiles(newPath));
                ResultFiles = temp;
            }

        }

        public ActionResult OnPostDownload(List<string> files)
        {
            string folderName = "Results";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);
            string TempPath = webRootPath + "\\Temp";

            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            string archive = TempPath + "\\Results.zip";

            // create a new archive
            ZipFile.CreateFromDirectory(newPath, archive);

            return File("/Temp/Results.zip", "application/zip", "Results.zip");
        }

        public RedirectToPageResult OnPostDeleteAll()
        {
            // Delete all directories and files in the "Unlabelled_Images" directory
            string Unlabelled_Images = "Unlabelled_Images";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Path_Unlabelled = Path.Combine(webRootPath, Unlabelled_Images);
            if (!Directory.Exists(Path_Unlabelled))
            {
                Directory.CreateDirectory(Path_Unlabelled);
            }

            foreach (var dir in Directory.EnumerateDirectories(Path_Unlabelled).ToList())
            {
                Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
            }
            Directory.EnumerateDirectories(Path_Unlabelled).ToList().ForEach(f => System.IO.Directory.Delete(f));

            // Delete all directories and files in the "Labelled_Images" directory
            string Labelled_Images = "Labelled_Images";
            string Path_Labelled = Path.Combine(webRootPath, Labelled_Images);
            if (!Directory.Exists(Path_Labelled))
            {
                Directory.CreateDirectory(Path_Labelled);
            }

            foreach (var dir in Directory.EnumerateDirectories(Path_Labelled).ToList())
            {
                Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
            }
            Directory.EnumerateDirectories(Path_Labelled).ToList().ForEach(f => System.IO.Directory.Delete(f));

            // Delete all files in "Results" and "Temp" directories
            string TempPath = webRootPath + "\\Temp";
            string ResultPath = webRootPath + "\\Results";
            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            Directory.EnumerateFiles(ResultPath).ToList().ForEach(f => System.IO.File.Delete(f));

            // Delete all files in "KeypointsImages" directory
            string KeypointsImages = webRootPath + "\\KeypointsImages";
            Directory.EnumerateFiles(KeypointsImages).ToList().ForEach(f => System.IO.File.Delete(f));

            // Delete GlobalVariables
            _GlobalVariables.AllDescs_Known.Release();
            _GlobalVariables.AllKeypoints_Known.Clear();
            _GlobalVariables.AllResults.Clear();
            _GlobalVariables.Keypoints_Num_Known.Clear();
            _GlobalVariables.KnownImgs.Clear();
            _GlobalVariables.Labels_Known.Clear();
            _GlobalVariables.UnknownImgs.Clear();

            DeleteClicked = true;

            return RedirectToPage("/Index");
        }

        public ActionResult OnPostDrawKeypoint()
        {
            string KeypointsImages = "KeypointsImages";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string Path_Keypoints = Path.Combine(webRootPath, KeypointsImages);
            if (!Directory.Exists(Path_Keypoints))
            {
                Directory.CreateDirectory(Path_Keypoints);
            }

            foreach (var Style in _GlobalVariables.UnknownImgs)
            {
                var UnlabelledImages = Directory.EnumerateFiles(Style.FilePath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpeg") ||
                s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif") || s.ToLower().EndsWith(".tiff") || s.ToLower().EndsWith(".png")
                || s.ToLower().EndsWith(".bmp"));

                foreach (var img in UnlabelledImages)
                {
                    Mat Current_Image = new Mat(img, ImreadModes.Grayscale);
                    KeyPoint[] Current_Keypoints;
                    List<KeyPoint> Current_Keypoints_List = new List<KeyPoint>();

                    if (_GlobalVariables.Kpts_Detector == "FAST")
                    {
                        var fastCPU = FastFeatureDetector.Create(1, true);
                        Current_Keypoints = fastCPU.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                        Current_Keypoints_List = Current_Keypoints_List.OrderByDescending(x => x.Response).ToList();
                        int SizeOfKeypoints = Current_Keypoints_List.Count();
                        int ConsideredKpts = (int)(SizeOfKeypoints * _GlobalVariables.Selected_Parameter / 100.0);
                        //Current_Keypoints_List.Clear();
                        Current_Keypoints_List = Current_Keypoints_List.Take(ConsideredKpts).ToList();
                        Current_Keypoints = Current_Keypoints_List.ToArray();
                    }
                    else //(GlobalVariables.Kpts_Detector == "SIFT")
                    {
                        var SIFT_Obj = SIFT.Create();
                        Current_Keypoints = SIFT_Obj.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                    }

                    Mat Current_Image_Color = new Mat(img, ImreadModes.Color);
                    Mat KeypointsImage = new Mat();
                    Cv2.DrawKeypoints(Current_Image_Color, Current_Keypoints_List, KeypointsImage, null, flags: DrawMatchesFlags.DrawRichKeypoints);

                    string CurrentPath = Path.Combine(Path_Keypoints, Path.GetFileName(img));//Style.FileNames[ImgCounter]);
                    KeypointsImage.SaveImage(CurrentPath);
                }
            }

            foreach (var Style in _GlobalVariables.KnownImgs)
            {
                var LabelledImages = Directory.EnumerateFiles(Style.FilePath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpeg") ||
                s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif") || s.ToLower().EndsWith(".tiff") || s.ToLower().EndsWith(".png")
                || s.ToLower().EndsWith(".bmp"));

                foreach (var img in LabelledImages)
                {
                    Mat Current_Image = new Mat(img, ImreadModes.Grayscale);
                    KeyPoint[] Current_Keypoints;
                    List<KeyPoint> Current_Keypoints_List = new List<KeyPoint>();

                    if (_GlobalVariables.Kpts_Detector == "FAST")
                    {
                        var fastCPU = FastFeatureDetector.Create(1, true);
                        Current_Keypoints = fastCPU.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                        Current_Keypoints_List = Current_Keypoints_List.OrderByDescending(x => x.Response).ToList();
                        int SizeOfKeypoints = Current_Keypoints_List.Count();
                        int ConsideredKpts = (int)(SizeOfKeypoints * _GlobalVariables.Selected_Parameter / 100.0);
                        //Current_Keypoints_List.Clear();
                        Current_Keypoints_List = Current_Keypoints_List.Take(ConsideredKpts).ToList();
                        Current_Keypoints = Current_Keypoints_List.ToArray();
                    }
                    else //(GlobalVariables.Kpts_Detector == "SIFT")
                    {
                        var SIFT_Obj = SIFT.Create();
                        Current_Keypoints = SIFT_Obj.Detect(Current_Image);
                        Current_Keypoints_List = Current_Keypoints.ToList();
                    }

                    Mat Current_Image_Color = new Mat(img, ImreadModes.Color);
                    Mat KeypointsImage = new Mat();
                    Cv2.DrawKeypoints(Current_Image_Color, Current_Keypoints_List, KeypointsImage, null, flags: DrawMatchesFlags.DrawRichKeypoints);

                    string CurrentPath = Path.Combine(Path_Keypoints, Path.GetFileName(img));//Style.FileNames[ImgCounter]);
                    KeypointsImage.SaveImage(CurrentPath);
                }
            }

            // create a new archive
            string TempPath = webRootPath + "\\Temp";
            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            string archive = TempPath + "\\Keypoints.zip";
            ZipFile.CreateFromDirectory(Path_Keypoints, archive);
            return File("/Temp/Keypoints.zip", "application/zip", "Keypoints.zip");
        }
    }
}