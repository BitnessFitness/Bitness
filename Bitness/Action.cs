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

        int Reps { get; }

        void Update (IReadOnlyDictionary<JointType, Joint> joints);
    }
}
