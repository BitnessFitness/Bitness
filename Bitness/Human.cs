using Microsoft.Kinect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitness
{
    struct Human
    {
        private Skeleton _skeleton;


        public Human(Skeleton skeleton)
        {
            this._skeleton = skeleton;
        }

        public void CalcAngles(Skeleton _skel)
        {
            SkeletonPoint elbowL = _skel.Joints[JointType.ElbowLeft].Position;
            SkeletonPoint wristL = _skel.Joints[JointType.WristLeft].Position;
            SkeletonPoint shoulderL = _skel.Joints[JointType.ShoulderLeft].Position;

            //Console.WriteLine("Elbow: " + elbowL.X + " " + elbowL.Y + " " + elbowL.Z);

            Vector4 elbowVector = new Vector4() { X = elbowL.X, Y = elbowL.Y, Z = elbowL.Z };
            Vector4 wristVector = new Vector4() { X = wristL.X, Y = wristL.Y, Z = wristL.Z };
            Vector4 shoulderVector = new Vector4() { X = shoulderL.X, Y = shoulderL.Y, Z = shoulderL.Z };

            double angle = (CalcAngle(elbowVector, wristVector, shoulderVector) / Math.PI) * 180.0;

            if (!double.IsNaN(angle))
            {
                Console.WriteLine("Angle: " + angle.ToString());
            }
        }
        /// <summary>
        /// Returns the angle of jointB
        /// </summary>
        /// <param name="jointA"></param>
        /// <param name="jointB"></param>
        /// <param name="jointC"></param>
        private double CalcAngle(Vector4 jointA, Vector4 jointB, Vector4 jointC)
        {
            double A = Dist(jointB, jointC);
            double B = Dist(jointA, jointC);
            double C = Dist(jointA, jointB);

            //Console.WriteLine("A:" + A + " B:" + B + " C:" + C);

            return Math.Acos(
                (Math.Pow(C, 2) - Math.Pow(A, 2) - Math.Pow(B, 2))
                / (-2 * A * B)
                );

        }

        private double Dist(Vector4 a, Vector4 b)
        {
            // Console.WriteLine("" + a.X + " " + a.Y + " " + a.Z);
            double xd = b.X - a.X;
            double yd = b.Y - a.Y;
            double zd = b.Z - a.Z;

            return Math.Sqrt(Math.Pow(xd, 2) + Math.Pow(yd, 2) + Math.Pow(zd, 2));
        }
    }
}
