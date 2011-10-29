using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNI;

namespace Plugin.OpenNI.Model
{
    public class KinectUser
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is calibrated.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is calibrated; otherwise, <c>false</c>.
        /// </value>
        public bool IsCalibrated { get; set; }

        /// <summary>
        /// Gets the joints for this user.
        /// </summary>
        public IDictionary<SkeletonJoint, KinectJoint> Joints { get; private set; }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public int UserId { get; private set; }        

        public KinectUser(int userId)
        {
            this.UserId = userId;
            this.Joints = new Dictionary<SkeletonJoint, KinectJoint>();

            var joints = Enum.GetValues(typeof(SkeletonJoint));

            foreach (var joint in joints)
            {
                if ((SkeletonJoint)joint != SkeletonJoint.Invalid)
                {
                    this.Joints.Add((SkeletonJoint)joint, new KinectJoint(joint.ToString()));
                }
            }
        }
    }
}