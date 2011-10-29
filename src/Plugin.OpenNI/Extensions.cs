using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenNILib = OpenNI;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this OpenNILib.Point3D point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }

        public static Vector3 FromRealToProjective(this Vector3 vector, OpenNILib.DepthGenerator depthGenerator)
        {
            var projectiveVector = depthGenerator.ConvertRealWorldToProjective(new OpenNILib.Point3D(vector.X, vector.Y, vector.Z));
            return projectiveVector.ToVector3();
        }
    }
}
