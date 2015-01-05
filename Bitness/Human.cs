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
            CalcAngles();
        }

        public void CalcAngles()
        {
            // TODO
        }
    }
}
