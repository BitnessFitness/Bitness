using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitness
{
    interface Action
    {

        bool InProgress { get; }
        int Reps { get; }

        WatchedJoint[] WatchedJoints
        {
            get;
        }

        string Update (IReadOnlyDictionary<JointType, Joint> joints);
    }
}
