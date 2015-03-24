using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitness
{
    class JumpingJack : Action
    {

        private WatchedJoint[] watchedJoints;
        private int reps = 0;
        private bool inProgress = false;
        private IReadOnlyDictionary<JointType, Joint> lastPosition = null; 

        public int Reps
        {
            get
            {
                return reps;
            }
        }

        public bool InProgress
        {
            get
            {
                return inProgress;
            }
        }

        public WatchedJoint[] WatchedJoints
        {
            get
            {
                return watchedJoints;
            }
        }

        /// <summary>
        /// Constructor for a JumpingJack event
        /// </summary>
        /// <param name="joints">List of kinect Joints</param>
        /// <param name="directions">The direction that the joint should be going (1 for up, -1 for down)</param>
        public JumpingJack (JointType[] joints, int[] directions)
        {
            WatchedJoint[]  watchedJoints = new WatchedJoint[joints.Length];

            for (int i = 0; i < joints.Length; i++)
            {
                watchedJoints[i] = new WatchedJoint(joints[i], directions[i]);
            }
            this.watchedJoints = watchedJoints;
        }


        public string Update (IReadOnlyDictionary<JointType, Joint> joints)
        {
            if (lastPosition == null)
            {
                lastPosition = joints;
            }

            for(int i = 0; i < watchedJoints.Length; i++)
            {
                WatchedJoint watchedJoint = watchedJoints[i];
                Joint joint = joints[watchedJoint.Joint];
                if (joint.Position.Y > lastPosition[watchedJoint.Joint].Position.Y)
                {
                    inProgress = true;
                    return "up";
                }
                else
                {
                    inProgress = true;
                    return "down";
                }
            }

            inProgress = false;
            return "no joints";
        }
    }
}
