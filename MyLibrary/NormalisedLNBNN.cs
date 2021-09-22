using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

using System.Runtime.InteropServices;
using OpenCvSharp.Flann;

namespace HAT3p5.MyLibrary
{

    public class LocalNBNN_Results
    {
        public float Votes = 0;
        public int Label = 0;
        public string Directory;
    }

    class NormalisedLNBNN
    {
        public List<LocalNBNN_Results> NNSearch_LNBNN_Priori(Mat QueryDescs, List<int> labels, OpenCvSharp.Flann.Index finder, int numClasses, List<KeyPoint> TrainKpts,
                                                            List<KeyPoint> TestKpts, List<int> Ktps_Num_Known, double AngleDiff = 10, double SizeDiff = 0.6)
        {
            LocalNBNN_Results[] Results_Array = new LocalNBNN_Results[numClasses];



            float AllVotes = 0;


            /// Rotate over all the descriptors of the test file and do the classification
            for (int d = QueryDescs.Rows - 1; d >= 0; d--)
            {

                int neighbours = 11;
                var curdesc = QueryDescs.Row(d);
                
                int[] knnin;
                float[] knndis;

                /// This function takes the query descriptor and returns the nearest 11 neighbours along with their distances
                finder.KnnSearch(curdesc, out knnin, out knndis, neighbours, new SearchParams(500));

                //double[] Temp_Knndis = new double[knndis.Width * knndis.Height];
                //Marshal.Copy(knndis.DataPointer, Temp_Knndis, 0, knndis.Width * knndis.Height);

                //double[] Temp_Knnin = new double[knnin.Width * knnin.Height];
                //Marshal.Copy(knnin.DataPointer, Temp_Knnin, 0, knnin.Width * knnin.Height);

                //double distb = Temp_Knndis[Temp_Knndis.Count()-1];
                float distb = knndis[neighbours - 1];

                List<bool> classused = new List<bool>();
                classused.AddRange(Enumerable.Repeat(false, numClasses));
                
                //for (int item = 0; item < numClasses; item++)
                //    classused.Add(false);

                ///Rotate over the neighbours and do the voting
                for (int n = 0; n < neighbours - 1; n++)
                {

                    int curlabel = (labels[knnin[n]]);

                    /// skip if this class is voted for
                    if (classused[curlabel])
                        continue;

                    /// skip if keypoints have different rotations
                    if (Math.Abs(TrainKpts[knnin[n]].Angle - TestKpts[d].Angle) > AngleDiff)
                    {
                        //cout<<endl<<"Angle difference constraint"<<endl;
                        continue;
                    }
                    /*
                                        /// skip if keypoints have different size
                                        float SmallerKptSize;
                                        float BiggerKptSize;
                                        if (TrainKpts[knnin[0, n]].Size > TestKpts[d].Size)
                                        {
                                            SmallerKptSize = TestKpts[d].Size;
                                            BiggerKptSize = TrainKpts[knnin[0, n]].Size;
                                        }
                                        else
                                        {
                                            SmallerKptSize = TrainKpts[knnin[0, n]].Size;
                                            BiggerKptSize = TestKpts[d].Size;
                                        }

                                        if ((SmallerKptSize / BiggerKptSize) < SizeDiff)
                                            continue; /// skip if keypoints have different sizes
                    */

                    float val = knndis[n];

                    classused[curlabel] = true;

                    if (Results_Array[curlabel] == null)
                        Results_Array[curlabel] = new LocalNBNN_Results();

                    Results_Array[curlabel].Label = curlabel;
                    Results_Array[curlabel].Votes += distb - val;// - distb;

                    //AllVotes += distb - val;
                }
            }

            for (int votes = 0; votes < Results_Array.Count(); votes++)
            {
                Results_Array[votes].Votes = Results_Array[votes].Votes / Ktps_Num_Known[votes];
                AllVotes += Results_Array[votes].Votes;

                Results_Array[votes].Label = votes;

            }

            for (int votes = 0; votes < Results_Array.Count(); votes++)
            {
                Results_Array[votes].Votes = 100 * (Results_Array[votes].Votes / AllVotes);
            }

            List<LocalNBNN_Results> Result = Results_Array.OfType<LocalNBNN_Results>().ToList();

            return Result;

        }
    }
}
