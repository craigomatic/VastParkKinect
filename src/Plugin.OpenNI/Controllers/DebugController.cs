using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.PluginFramework.Controllers;
using Plugin.OpenNI.Model;
using VastPark.Common;
using VastPark.FrameworkBase.Drawing;
using OpenNI;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI.Controllers
{
    /// <summary>
    /// Draws debug information for a KinectUser
    /// </summary>
    public class DebugController : IController
    {
        public KinectUser User { get; private set; }

        public IRenderEngine RenderEngine { get; private set; }

        public bool Enabled { get; set; }

        private Dictionary<SkeletonJoint, ILine> _DebugLines;
        private DepthGenerator _DepthGenerator;

        public DebugController(Context context, KinectUser user, IRenderEngine renderEngine)
        {
            _DepthGenerator = new DepthGenerator(context);

            this.User = user;
            this.RenderEngine = renderEngine;

            _DebugLines = new Dictionary<SkeletonJoint, ILine>();
        }

        public void Update()
        {
            foreach (var joint in this.User.Joints)
            {
                if (!_DebugLines.ContainsKey(joint.Key))
                {
                    var line = this.RenderEngine.CreateLine();

                    //up/down line
                    line.AddSegment(new Vector3(joint.Value.Position.X, joint.Value.Position.Y + 1, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                    line.AddSegment(new Vector3(joint.Value.Position.X, joint.Value.Position.Y - 1, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));

                    //across ways line
                    line.AddSegment(new Vector3(joint.Value.Position.X + 1, joint.Value.Position.Y, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                    line.AddSegment(new Vector3(joint.Value.Position.X - 1, joint.Value.Position.Y, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));

                    _DebugLines.Add(joint.Key, line);
                }
                else
                {
                    _DebugLines[joint.Key].UpdateSegment(0, new Vector3(joint.Value.Position.X, joint.Value.Position.Y + 1, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                    _DebugLines[joint.Key].UpdateSegment(1, new Vector3(joint.Value.Position.X, joint.Value.Position.Y - 1, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                    _DebugLines[joint.Key].UpdateSegment(2, new Vector3(joint.Value.Position.X + 1, joint.Value.Position.Y, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                    _DebugLines[joint.Key].UpdateSegment(3, new Vector3(joint.Value.Position.X - 1, joint.Value.Position.Y, joint.Value.Position.Z).FromRealToProjective(_DepthGenerator));
                }
            }                            
        }

        public void Dispose()
        {
            foreach (var line in _DebugLines.Values)
            {
                line.Dispose();
            }

            _DebugLines.Clear();

            _DepthGenerator.Dispose();
        }
    }
}
