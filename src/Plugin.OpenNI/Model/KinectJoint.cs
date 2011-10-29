using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI.Model
{
    public class KinectJoint
    {
        public bool IsAvailable { get; set; }

        public bool IsActive { get; set; }

        public Vector3 Position { get; set; }

        public float PositionConfidence { get; set; }

        public Matrix RotationMatrix { get; set; }

        public float RotationConfidence { get; set; }

        /// <summary>
        /// Gets or sets the name of the joint.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        public KinectJoint(string name)
        {
            this.Name = name;
        }
    }
}
