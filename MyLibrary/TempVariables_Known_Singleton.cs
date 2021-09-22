using OpenCvSharp;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HAT3p5.MyLibrary
{
    public class TempVariables_Known_Singleton
    {
        public ImgInfo TempImgInfo = new ImgInfo();
        public bool Used = false;
    }
}
