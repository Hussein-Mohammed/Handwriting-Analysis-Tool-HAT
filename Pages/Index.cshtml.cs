using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HAT3p5.MyLibrary;

namespace HAT3p5.Pages
{
    public class IndexModel : PageModel
    {
        private IWebHostEnvironment _hostingEnvironment;
        private readonly GlobalVariables _GlobalVariables;
        public IndexModel(IWebHostEnvironment hostingEnvironment, GlobalVariables GlobalVariables)
        {
            _hostingEnvironment = hostingEnvironment;
            _GlobalVariables = GlobalVariables;
        }

        public void OnGet()
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

            // Delete all files in "Results"
            string ResultPath = webRootPath + "\\Results";
            if (!Directory.Exists(ResultPath))
            {
                Directory.CreateDirectory(ResultPath);
            }
            Directory.EnumerateFiles(ResultPath).ToList().ForEach(f => System.IO.File.Delete(f));

            // delete the temp files
            string TempPath = webRootPath + "\\Temp";
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            Directory.EnumerateFiles(TempPath).ToList().ForEach(f => System.IO.File.Delete(f));
            foreach (var dir in Directory.EnumerateDirectories(TempPath).ToList())
            {
                Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
            }
            Directory.EnumerateDirectories(TempPath).ToList().ForEach(f => System.IO.Directory.Delete(f));

            // Delete all files in "KeypointsImages" directory
            string KeypointsImages = webRootPath + "\\KeypointsImages";
            if (!Directory.Exists(KeypointsImages))
            {
                Directory.CreateDirectory(KeypointsImages);
            }
            Directory.EnumerateFiles(KeypointsImages).ToList().ForEach(f => System.IO.File.Delete(f));

            // Delete GlobalVariables
            _GlobalVariables.AllDescs_Known.Release();
            _GlobalVariables.AllKeypoints_Known.Clear();
            _GlobalVariables.AllResults.Clear();
            _GlobalVariables.Keypoints_Num_Known.Clear();
            _GlobalVariables.KnownImgs.Clear();
            _GlobalVariables.Labels_Known.Clear();
            _GlobalVariables.UnknownImgs.Clear();
        }
    }
}
