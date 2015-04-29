using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitness
{
    public class Player
    {
        public enum State {
            SYNCING,
            SYNCED,
            NOT_SYNCED,
            TUTORIAL,
            COMPLETED
        };

        public State state = State.NOT_SYNCED;
        public Body body;
        public JumpingJack exercise;
        public int Reps = 0;

        public Player (Body body, JumpingJack exercise)
        {
            this.body = body;
            this.exercise = exercise;
        }

        public bool Update(IReadOnlyDictionary<JointType, Joint> joints)
        {
            bool repCompleted = this.exercise.Update(joints);
            if (repCompleted)
            {
                this.Reps = this.exercise.Reps;
            }
            return repCompleted;
        }
    }
}
