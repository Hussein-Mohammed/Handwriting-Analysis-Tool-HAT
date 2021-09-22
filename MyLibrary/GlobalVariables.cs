using HAT3p5.MyLibrary;
using OpenCvSharp;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HAT3p5.MyLibrary
{
    public class GlobalVariables
    {
        public List<ImgInfo> UnknownImgs = new List<ImgInfo>();
        
        public List<ImgInfo> KnownImgs = new List<ImgInfo>();
        
        public Mat AllDescs_Known = new Mat();
        
        public List<KeyPoint> AllKeypoints_Known = new List<KeyPoint>();
        
        public List<int> Labels_Known = new List<int>();
        
        public List<int> Keypoints_Num_Known = new List<int>();
        
        public List<List<LocalNBNN_Results>> AllResults = new List<List<LocalNBNN_Results>>();

        public string Kpts_Detector = "FAST";

        public float Selected_Parameter = 10;
    }

}
