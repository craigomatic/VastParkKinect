using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.PluginFramework;
using OpenNI;
using Plugin.OpenNI.Controllers;
using VastPark.FrameworkBase;
using Plugin.OpenNI.Model;
using VastPark.FrameworkBase.ComponentModel;
using System.Threading;
using VastPark.Imml.Proxy;
using VastPark.Data;
using VastPark.PluginFramework.Controllers;
using VastPark.Imml.Scene.Controls;

namespace Plugin.OpenNI
{
    public class OpenNiPlugin : PluginBase
    {
        #region Properties
        
        public string UserFound { get; set; }

        public string UserLost { get; set; }

        public bool DrawJoints { get; set; }

        public string StatusOutputTarget { get; set; }

        private bool _RenderVideo;

        public bool RenderVideo
        {
            get { return _RenderVideo; }
            set
            {
                _RenderVideo = value;

                if (_VideoController != null)
                {
                    _VideoController.Enabled = value;
                }
            }
        }

        private bool _RenderDepth;

        public bool RenderDepth
        {
            get { return _RenderDepth; }
            set
            {
                _RenderDepth = value;

                if (_DepthVideoController != null)
                {
                    _DepthVideoController.Enabled = value;
                }
            }
        }

        private bool _DetectGestures;

        public bool DetectGestures
        {
            get { return _DetectGestures; }
            set
            {
                _DetectGestures = value;

                if (_GestureController != null)
                {
                    _GestureController.Enabled = value;
                }
            }
        }

        public string VideoOutputTarget { get; set; }

        public string DepthVideoOutputTarget { get; set; }

        /// <summary>
        /// Gets a value indicating whether the underlying Kinect context is ready.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is context ready; otherwise, <c>false</c>.
        /// </value>
        public bool IsContextReady { get; private set; }

        #endregion

        private Context _KinectContext;
        
        private UserController _KinectController;
        private VideoController _VideoController;
        private DepthVideoController _DepthVideoController;
        private GestureController _GestureController;        

        public override void Load()
        {
            if (!string.IsNullOrEmpty(this.StatusOutputTarget) && base.ParkEngine.ContainsName(this.StatusOutputTarget))
            {
                //create a text element based log
                var statusText = base.ParkEngine.GetElementByName(this.StatusOutputTarget) as Text;
                _CreateLog(statusText);
            }
            else
            {
                //allow the base logger to be created instead
                base.Load();
            }

            ThreadPool.QueueUserWorkItem(w =>
            {
                this.LogProvider.Write("Initialising context...", LogLevel.Notice);

                var configByes = VastPark.FrameworkBase.IO.EmbeddedResource.GetBytes("Plugin.OpenNI.KinectConfig.xml", System.Reflection.Assembly.GetExecutingAssembly());

                var initFile = System.IO.Path.GetTempFileName();
                System.IO.File.WriteAllBytes(initFile, configByes);

                while (!this.IsContextReady)
                {
                    try
                    {
                        ScriptNode scriptNode = null;

                        _KinectContext = Context.CreateFromXmlFile(initFile, out scriptNode);

                        System.IO.File.Delete(initFile);

                        this.IsContextReady = true;
                    }
                    catch (StatusException ex)
                    {
                        //Trace.WriteLine("XnStatusException: " + ex.ToString());
                        //isInit = false;
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch (GeneralException ex)
                    {
                        return;
                    }
                }

                this.LogProvider.Write("Context initialised", LogLevel.Notice);

                var calibrationPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                calibrationPath = System.IO.Path.Combine(calibrationPath, "calibration.dat");

                _KinectController = new UserController(_KinectContext, calibrationPath) { Enabled = true };

                _KinectController.PoseDetected += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser, string>(kinectController_PoseDetected);
                _KinectController.SkeletonReady += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser>(kinectController_SkeletonReady);
                _KinectController.UserFound += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser>(kinectController_UserFound);
                _KinectController.UserLost += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser>(kinectController_UserLost);
                _KinectController.CalibrationStarted += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser>(_KinectController_CalibrationStarted);
                _KinectController.CalibrationEnded += new VastPark.FrameworkBase.ComponentModel.Event<KinectUser, bool>(_KinectController_CalibrationEnded);

                base.AddController(_KinectController);

                //video controller
                if (this.RenderVideo)
                {
                    if (string.IsNullOrEmpty(this.VideoOutputTarget) || !base.ParkEngine.ContainsName(this.VideoOutputTarget))
                    {
                        base.LogProvider.Write("RenderVideo is enabled without a defined VideoOutputTarget. Video will not be drawn.", LogLevel.Error);
                    }
                    else
                    {
                        _VideoController = new VideoController(_KinectContext, new TextureController(base.ParkEngine) { Width = 640, Height = 480 });
                        _VideoController.Enabled = this.RenderVideo;
                        _VideoController.TextureController.AddOutput((IVisibleElement)base.ParkEngine.GetElementByName(this.VideoOutputTarget), -1);

                        base.AddController(_VideoController);
                    }
                }

                //depth controller
                if (this.RenderDepth)
                {
                    if (string.IsNullOrEmpty(this.DepthVideoOutputTarget) || !base.ParkEngine.ContainsName(this.DepthVideoOutputTarget))
                    {
                        base.LogProvider.Write("RenderDepth is enabled without a defined DepthVideoOutputTarget. Depth video will not be drawn.", LogLevel.Error);
                    }
                    else
                    {
                        _DepthVideoController = new DepthVideoController(_KinectContext, new TextureController(base.ParkEngine) { Width = 640, Height = 480 });
                        _DepthVideoController.Enabled = this.RenderDepth;
                        _DepthVideoController.TextureController.AddOutput((IVisibleElement)base.ParkEngine.GetElementByName(this.DepthVideoOutputTarget), -1);

                        base.AddController(_DepthVideoController);
                    }
                }

                //gesture controller
                _GestureController = new GestureController(base.ParkEngine, base.LogProvider, _KinectContext);
                _GestureController.Enabled = this.DetectGestures;

                base.AddController(_GestureController);
            });
        }        
        
        public override void Update()
        {
            //update the context first
            if (this.IsContextReady)
            {
                _KinectContext.WaitAndUpdateAll();
            }

            base.Update();
        }

        public override void Dispose()
        {
            base.Dispose();

            try
            {
                _KinectContext.Dispose();
            }
            catch { }            
        }

        /// <summary>
        /// Adds a handler for the specified gesture.
        /// </summary>
        /// <param name="gesture">The gesture.</param>
        /// <param name="handler">The handler.</param>
        public void AddListener(string gesture, string handler)
        {
            _GestureController.AddListener(gesture, handler);
        }

        /// <summary>
        /// Links the specified user to a proxy element.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="element">The element.</param>
        public void Link(KinectUser user, VastPark.Imml.Proxy.ImmlElement element)
        {
            if (ElementProxyFactory.GetType(element) != VastPark.Data.ElementType.Model)
            {
                throw new Exception("Invalid element to link, must be of type Model");
            }

            var model = ElementProxyFactory.ExtractElement(element) as VastPark.Imml.Scene.Controls.Model;
            var modelController = new ModelController(user, model) { Enabled = true };
            
            base.AddController(modelController);

            this.LogProvider.Write(string.Format("User {0} is now associated with model {1}", user.UserId, model.Name), LogLevel.Notice);
        }

        /// <summary>
        /// Unlinks the specified user from the specified proxy element.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="element">The element.</param>
        public void Unlink(KinectUser user, VastPark.Imml.Proxy.ImmlElement element)
        {
            if (ElementProxyFactory.GetType(element) != VastPark.Data.ElementType.Model)
            {
                throw new Exception("Invalid element to unlink, must be of type Model");
            }

            var model = ElementProxyFactory.ExtractElement(element) as VastPark.Imml.Scene.Controls.Model;
            var modelController = base.Controllers.Where(c => c is ModelController && (c as ModelController).Model == model).FirstOrDefault();

            if (modelController != null)
            {
                base.RemoveController(modelController);
            }

            this.LogProvider.Write(string.Format("User {0} is no longer associated with model {1}", user.UserId, model.Name), LogLevel.Notice);
        }

        private void _CreateLog(Text statusText)
        {
            var filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            filePath = filePath.Remove(filePath.LastIndexOf('\\') + 1);
            filePath += string.Format("{0}.log", this.GetType().Name);

            //if file exists and is older than 48hrs, try to delete it
            if (System.IO.File.Exists(filePath))
            {
                var fileInfo = new System.IO.FileInfo(filePath);

                if (DateTime.UtcNow.Subtract(fileInfo.CreationTimeUtc) > TimeSpan.FromHours(48))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch { }
                }
            }

            base.LogProvider = new StatusTextLog(statusText, filePath, LogLevel.Notice);
        }

        void kinectController_UserLost(object sender, VastPark.FrameworkBase.ComponentModel.TypedEventArgs<KinectUser> e)
        {
            var toRemove = base.Controllers.Where(c => (c is DebugController) && (c as DebugController).User == e.Value).FirstOrDefault();

            if (toRemove != null)
            {
                base.RemoveController(toRemove);
            }

            this.LogProvider.Write(string.Format("Lost user: {0}", e.Value.UserId), LogLevel.Notice);

            if (!string.IsNullOrEmpty(this.UserLost) && base.ParkEngine.ContainsName(this.UserLost))
            {
                var toExecute = base.ParkEngine.GetElementByName(this.UserLost) as ITimelineExecutable;

                if (toExecute != null)
                {
                    toExecute.Execute(this, e.Value);
                }
            }
        }

        void kinectController_UserFound(object sender, TypedEventArgs<KinectUser> e)
        {
            if (this.DrawJoints)
            {
                var debugController = new DebugController(_KinectContext, e.Value, base.ParkEngine.RenderEngine) { Enabled = true };
                base.AddController(debugController);
            }

            this.LogProvider.Write(string.Format("Found user: {0}", e.Value.UserId), LogLevel.Notice);

            if (!string.IsNullOrEmpty(this.UserFound) && base.ParkEngine.ContainsName(this.UserFound))
            {
                var toExecute = base.ParkEngine.GetElementByName(this.UserFound) as ITimelineExecutable;

                if (toExecute != null)
                {
                    toExecute.Execute(this, e.Value);
                }
            }
        }

        void kinectController_SkeletonReady(object sender, TypedEventArgs<KinectUser> e)
        {
            this.LogProvider.Write("Skeleton is ready", LogLevel.Notice);
        }

        void kinectController_PoseDetected(object sender, TypedEventArgs<KinectUser, string> e)
        {
            this.LogProvider.Write(string.Format("Pose '{0}' detected for user: {1}", e.Value2, e.Value1.UserId), LogLevel.Notice);
        }

        void _KinectController_CalibrationEnded(object sender, TypedEventArgs<KinectUser, bool> e)
        {
            if (e.Value2)
            {
                this.LogProvider.Write("Calibration failed, searching for pose", LogLevel.Notice);
            }
            else
            {
                this.LogProvider.Write(string.Format("Calibration completed for user: {0}", e.Value1.UserId), LogLevel.Notice);
            }            
        }

        void _KinectController_CalibrationStarted(object sender, TypedEventArgs<KinectUser> e)
        {
            this.LogProvider.Write(string.Format("Calibration started for user: {0}", e.Value.UserId), LogLevel.Notice);
        }        
    }
}
