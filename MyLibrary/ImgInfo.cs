using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HAT3p5.MyLibrary
{
    public class ImgInfo
    {
        
        public string Descriptors { get; set; }

        [Display(Name = "Name of Handwriting Style")]
        public string HSName { get; set; }

        [Display(Name = "Number of Files")]
        public int NumberOfFiles { get; set; }

        public int Label { get; set; }
        public List<string> FileNames = new List<string>();
        public string FilePath { get; set; }
        public List<KeyPoint> Keypoints = new List<KeyPoint>();
    }
}
