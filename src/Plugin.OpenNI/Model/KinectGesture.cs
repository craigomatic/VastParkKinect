using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI.Model
{
    public class KinectGesture
    {
        public string Name { get; private set; }

        /// <summary>
        /// Gets the progress.
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// Gets the position.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// Gets the final position.
        /// </summary>
        public Vector3 FinalPosition { get; private set; }

        public KinectGesture(string name, float progress, Vector3 position, Vector3 finalPosition)
        {
            this.Name = name;
            this.Progress = progress;
            this.Position = position;
            this.FinalPosition = finalPosition;
        }
    }
}
