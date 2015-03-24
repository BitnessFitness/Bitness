using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitness
{
    class WatchedJoint
    {
        private JointType jointType;
        private int direction;

        public JointType Joint
        {
            get
            {
                return jointType;
            }
        }

        public WatchedJoint (JointType jointType, int direction)
        {
            this.jointType = jointType;
            this.direction = direction;
        }
    }
}
