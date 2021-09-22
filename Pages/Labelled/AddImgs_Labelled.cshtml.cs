using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HAT3p5.MyLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

namespace HAT3p5.Pages.Labelled
{
    [IgnoreAntiforgeryToken]
    public class AddImgs_LabelledModel : PageModel
    {
        public ImgInfo Imgs_Local = new ImgInfo();
        public IEnumerable<string> ValidFiles { get; set; }

        // flags to control the logic
        public bool DuplicatedHS { get; set; }
        public bool ExistingHS { get; set; }
        public bool InvalidForm { get; set; }

        public static string HSName { get; set; }

        // needed to get the wwwroot directory
        private IWebHostEnvironment _hostingEnvironment;
        private readonly TempVariables_Known_Singleton _TempVariables_Singleton;
        private readonly GlobalVariables _GlobalVariables;

        public AddImgs_LabelledModel(IWebHostEnvironment hostingEnvironment, TempVariables_Known_Singleton TempVariables_Singleton, GlobalVariables GlobalVariables)
        {
            _hostingEnvironment = hostingEnvironment;
            _TempVariables_Singleton = TempVariables_Singleton;
            _GlobalVariables = GlobalVariables;
        }

        public void OnGet()
        {
            _TempVariables_Singleton.Used = false;
            _TempVariables_Singleton.TempImgInfo.Descriptors = "";
            _TempVariables_Singleton.TempImgInfo.FileNames.Clear();
            _TempVariables_Singleton.TempImgInfo.HSName = "";
            _TempVariables_Singleton.TempImgInfo.Keypoints.Clear();
            _TempVariables_Singleton.TempImgInfo.Label = 0;
            _TempVariables_Singleton.TempImgInfo.NumberOfFiles = 0;
            _TempVariables_Singleton.TempImgInfo.FilePath = "";

            DuplicatedHS = false;
            ExistingHS = false;
            InvalidForm = false;
            ViewData["ValidFiles"] = 0;

            ViewData["confirmation"] = "No name has been assigned yet !";
        }

        public ActionResult OnPostDeleteHS(string HS)
        {
            for (int SameHS = 0; SameHS < _GlobalVariables.KnownImgs.Count(); SameHS++)
            {
                if (HS == _GlobalVariables.KnownImgs[SameHS].HSName)
                {
                    _GlobalVariables.KnownImgs.Remove(_GlobalVariables.KnownImgs[SameHS]);
                    break;
                }
            }

            this.OnGet();
            return Page();
        }

        public void OnPostHSName(string HSName_Post = "Nothing")
        {
            HSName = HSName_Post;
            foreach (var HS in _GlobalVariables.KnownImgs)
            {
                if (HSName == HS.HSName)
                {
                    DuplicatedHS = true;
                    ViewData["confirmation"] = $"The uploaded files wil be added to: {HSName_Post}";
                    break;
                }
            }

            if (!DuplicatedHS)
                ViewData["confirmation"] = $"{HSName_Post} !";
        }

        public ActionResult OnPostUpload(List<IFormFile> files)
        {
            if (files != null && files.Count > 0)
            {
                // Save the files and the ImgInfo information in temp folders and temp variables
                string folderName = "Temp";
                string webRootPath = _hostingEnvironment.WebRootPath;
                string newPath = Path.Combine(webRootPath, folderName);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }

                // Delete all files in Temp directory before starting to save new files
                Directory.EnumerateFiles(newPath).ToList().ForEach(f => System.IO.File.Delete(f));
                foreach (var dir in Directory.EnumerateDirectories(newPath).ToList())
                {
                    Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
                }
                Directory.EnumerateDirectories(newPath).ToList().ForEach(f => System.IO.Directory.Delete(f));

                // Save selected files to the Temp directory
                foreach (IFormFile item in files)
                {
                    if (item.Length > 0)
                    {
                        string fileName = ContentDispositionHeaderValue.Parse(item.ContentDisposition).FileName.Trim('"');
                        string fullPath = Path.Combine(newPath, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            item.CopyTo(stream);
                        }

                        //TempVariables.TempImgInfo.FileNames.Add(fileName);
                        //_TempVariables_Singleton.TempImgInfo.FileNames.Add(fileName);
                    }
                }

                //TempVariables.TempImgInfo.FilePath = newPath;
                _TempVariables_Singleton.TempImgInfo.FilePath = newPath;

                ValidFiles = Directory.EnumerateFiles(newPath, "*.*", SearchOption.AllDirectories).Where(s => s.ToLower().EndsWith(".jpeg") ||
                s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".tif") || s.ToLower().EndsWith(".tiff") || s.ToLower().EndsWith(".png")
                || s.ToLower().EndsWith(".bmp"));

                // Convert ".tif" files to ".jpg"
                foreach (var img in ValidFiles)
                {
                    if (Path.GetExtension(img).ToLower() == ".tif" || Path.GetExtension(img).ToLower() == ".tiff")
                    {
                        Mat Current_Image = new Mat(img, ImreadModes.Color);
                        string Path_of_Converted_Image = newPath + "\\" + Path.GetFileNameWithoutExtension(img) + ".jpg";
                        Current_Image.ImWrite(Path_of_Converted_Image);

                        _TempVariables_Singleton.TempImgInfo.FileNames.Add(Path.GetFileNameWithoutExtension(img) + ".jpg");

                        System.IO.File.Delete(img);
                    }
                    else
                    {
                        _TempVariables_Singleton.TempImgInfo.FileNames.Add(Path.GetFileName(img));
                    }
                }

                ViewData["ValidFiles"] = ValidFiles.Count();
                //TempVariables.TempImgInfo.NumberOfFiles = ValidFiles.Count();
                _TempVariables_Singleton.TempImgInfo.NumberOfFiles += ValidFiles.Count();


                return this.Content("Success");
            }
            return this.Content("Fail");
        }
        public ActionResult OnPostAddHS()
        {
            string TempDir = "Temp";
            string RootDir = _hostingEnvironment.WebRootPath;
            string DirToDelete = Path.Combine(RootDir, TempDir);
            if (HSName != null && Directory.EnumerateFiles(DirToDelete).Count() > 0)
            {
                // check whether the handwriting style exist
                int SameHS = 0;
                for (; SameHS < _GlobalVariables.KnownImgs.Count(); SameHS++)
                {
                    if (HSName == _GlobalVariables.KnownImgs[SameHS].HSName)
                    {
                        ExistingHS = true;
                        break;
                    }
                }

                // if the style does not exist, then create a new one
                if (!ExistingHS)
                {
                    string folderName = "Labelled_Images";
                    string SubFolderName = HSName;
                    string webRootPath = _hostingEnvironment.WebRootPath;
                    string newPath = Path.Combine(webRootPath, folderName);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }

                    string StylePath = Path.Combine(newPath, SubFolderName);
                    if (!Directory.Exists(StylePath))
                    {
                        Directory.CreateDirectory(StylePath);
                    }

                    foreach (var file in Directory.GetFiles(_TempVariables_Singleton.TempImgInfo.FilePath))
                        System.IO.File.Copy(file, Path.Combine(StylePath, Path.GetFileName(file)), true);

                    ImgInfo TempImg = new ImgInfo();
                    TempImg.FilePath = StylePath;
                    TempImg.HSName = HSName;
                    TempImg.FileNames.AddRange(_TempVariables_Singleton.TempImgInfo.FileNames);
                    TempImg.NumberOfFiles = _TempVariables_Singleton.TempImgInfo.NumberOfFiles;

                    _GlobalVariables.KnownImgs.Add(TempImg);
                }

                // if the style exist, add the new files to the existed one
                else
                {
                    foreach (var file in Directory.GetFiles(_TempVariables_Singleton.TempImgInfo.FilePath))
                        System.IO.File.Copy(file, Path.Combine(_GlobalVariables.KnownImgs[SameHS].FilePath, Path.GetFileName(file)), true);

                    foreach (var FileName_Temp in _TempVariables_Singleton.TempImgInfo.FileNames)
                    {
                        bool NameFound = false;
                        foreach (var FileName_Unknown in _GlobalVariables.KnownImgs[SameHS].FileNames)
                        {
                            if (FileName_Temp == FileName_Unknown)
                            {
                                NameFound = true;
                                break;
                            }
                        }

                        if (!NameFound)
                        {

                            _GlobalVariables.KnownImgs[SameHS].NumberOfFiles++;
                            _GlobalVariables.KnownImgs[SameHS].FileNames.Add(FileName_Temp);
                        }
                    }
                }

                // Delete temporary files directories and objects
                //_TempVariables_Singleton.TempImgInfo.FileNames.Clear();

                Directory.EnumerateFiles(DirToDelete).ToList().ForEach(f => System.IO.File.Delete(f));
                foreach (var dir in Directory.EnumerateDirectories(DirToDelete).ToList())
                {
                    Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
                }
                Directory.EnumerateDirectories(DirToDelete).ToList().ForEach(f => System.IO.Directory.Delete(f));

                this.OnGet();
                return Page();
                // return RedirectToPage("Index");
            }
            else
            {
                Directory.EnumerateFiles(DirToDelete).ToList().ForEach(f => System.IO.File.Delete(f));
                foreach (var dir in Directory.EnumerateDirectories(DirToDelete).ToList())
                {
                    Directory.EnumerateFiles(dir).ToList().ForEach(f => System.IO.File.Delete(f));
                }
                Directory.EnumerateDirectories(DirToDelete).ToList().ForEach(f => System.IO.Directory.Delete(f));

                InvalidForm = true;

                return Page();
            }
        }
    }
}