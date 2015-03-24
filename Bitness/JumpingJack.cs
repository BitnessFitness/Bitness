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
        enum State
        {
            UP,
            DOWN,
            REST
        }

        private const float THRESHOLD = 0.05f;

        private int reps = 0;
        private bool inProgress = false;
        private IReadOnlyList<JointType> watchedJoints = null;
        private IReadOnlyDictionary<JointType, Joint> lastPosition = null;
        private State workoutState;

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

        /// <summary>
        /// Constructor for a JumpingJack event
        /// </summary>
        /// <param name="joints">List of kinect Joints</param>
        /// <param name="directions">The direction that the joint should be going (1 for up, -1 for down)</param>
        public JumpingJack(IReadOnlyList<JointType> watchedJoints)
        {
            this.watchedJoints = watchedJoints;
            this.workoutState = State.REST;
        }


        public void Update(IReadOnlyDictionary<JointType, Joint> joints)
        {
            if (lastPosition == null)
            {
                this.lastPosition = joints;
            }

            if (this.workoutState == State.REST)
            {
                // check is jumping up
                if (StateChanged(joints))
                    this.workoutState = State.UP;
            }
            else if (this.workoutState == State.UP)
            {
                // check if moving downwards
                if(StateChanged(joints))
                    this.workoutState = State.DOWN;
            }
            else
            {
                // check if now at rest
                if(StateChanged(joints))
                {
                    this.workoutState = State.REST;
                    this.reps++;
                }
            }

            this.lastPosition = joints;
        }

        private bool StateChanged(IReadOnlyDictionary<JointType, Joint> joints)
        {
            bool thresholdPassed = true;

            if(this.workoutState == State.REST)
            {
                foreach (JointType watchedJointType in this.watchedJoints)
                {
                    Joint last = this.lastPosition[watchedJointType];
                    Joint current = joints[watchedJointType];

                    if (current.Position.Y <= last.Position.Y + THRESHOLD &&
                        current.Position.Y >= last.Position.Y - THRESHOLD)
                        thresholdPassed = false;
                }
            }
            else if(this.workoutState == State.DOWN)
            {
                foreach (JointType watchedJointType in this.watchedJoints)
                {
                    Joint last = this.lastPosition[watchedJointType];
                    Joint current = joints[watchedJointType];

                    if (current.Position.Y <= last.Position.Y + THRESHOLD &&
                        current.Position.Y >= last.Position.Y - THRESHOLD)
                        thresholdPassed = false;
                }
            }
            else if (this.workoutState == State.UP)
            {
                foreach (JointType watchedJointType in this.watchedJoints)
                {
                    Joint last = this.lastPosition[watchedJointType];
                    Joint current = joints[watchedJointType];

                    if (current.Position.Y <= last.Position.Y + THRESHOLD)
                        thresholdPassed = false;
                }
            }
            return thresholdPassed;
        }
    }
}
