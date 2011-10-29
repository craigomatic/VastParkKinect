using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VastPark.PluginFramework.Controllers;
using OpenNI;
using System.Drawing;
using VastPark.FrameworkBase.ComponentModel;
using Plugin.OpenNI.Model;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI.Controllers
{
    public class UserController : IController
    {
        public event Event<KinectUser> CalibrationStarted;
        public event Event<KinectUser, bool> CalibrationEnded;

        public event Event<KinectUser> UserFound;
        public event Event<KinectUser> UserLost;

        public event Event<KinectUser> SkeletonReady;
        public event Event<KinectUser, string> PoseDetected;

        public bool Enabled { get; set; }

        public Context Context { get; private set; }

        private UserGenerator _UserGenerator;
        private SkeletonCapability _SkeletonCapability;
        private PoseDetectionCapability _PoseDetectionCapability;
        private string _CalibrationPose;        

        private object _UserLock;
        private object _SkeletonLock;

        public Dictionary<int, KinectUser> Users { get; private set; }

        public string CalibrationPath { get; private set; }        

        public UserController(Context context, string calibrationPath)
        {
            this.Context = context;
            this.CalibrationPath = calibrationPath;
            this.Users = new Dictionary<int, KinectUser>();
            _UserLock = new object();
            _SkeletonLock = new object();

            _UserGenerator = new UserGenerator(Context);
            _SkeletonCapability = _UserGenerator.SkeletonCapability;
            _PoseDetectionCapability = _UserGenerator.PoseDetectionCapability;

            _CalibrationPose = _SkeletonCapability.CalibrationPose;

            _UserGenerator.NewUser += new EventHandler<NewUserEventArgs>(_UserGenerator_NewUser);
            _UserGenerator.LostUser += new EventHandler<UserLostEventArgs>(_UserGenerator_LostUser);
            _PoseDetectionCapability.PoseDetected += new EventHandler<PoseDetectedEventArgs>(PoseDetectionCapability_PoseDetected);
            _SkeletonCapability.CalibrationComplete += new EventHandler<CalibrationProgressEventArgs>(_SkeletonCapability_CalibrationComplete);

            _SkeletonCapability.SetSkeletonProfile(SkeletonProfile.All);
            _UserGenerator.StartGenerating();            
		}

        void _SkeletonCapability_CalibrationComplete(object sender, CalibrationProgressEventArgs e)
        {
            var user = _FindUser(e.ID);

            if (e.Status == CalibrationStatus.Pose)
            {
                _SkeletonCapability.StartTracking(e.ID);
                _SkeletonCapability.SaveCalibrationDataToFile(e.ID, this.CalibrationPath);

                if (user != null)
                {
                    user.IsCalibrated = true;
                    SkeletonReady.Raise(this, user);
                }
            }
            else
            {
                _PoseDetectionCapability.StartPoseDetection(_CalibrationPose, e.ID);
            }

            CalibrationEnded.Raise(this, user, e.Status == CalibrationStatus.Pose);
        }

        void PoseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
        {
            KinectUser user = _FindUser(e.ID);

            if (user != null)
            {
                PoseDetected.Raise(this, user, e.Pose);
            }

            if (_SkeletonCapability.IsCalibrating(e.ID))
            {
                return;
            }

            _PoseDetectionCapability.StopPoseDetection(e.ID);
            _SkeletonCapability.RequestCalibration(e.ID, true);

            CalibrationStarted.Raise(this, user);
        }        

        void _UserGenerator_LostUser(object sender, UserLostEventArgs e)
        {
            KinectUser user = _FindUser(e.ID);

            _PoseDetectionCapability.StopPoseDetection(e.ID);

            lock (_UserLock)
            {
                if (this.Users.ContainsKey(e.ID))
                {
                    this.Users.Remove(e.ID);
                }
            }

            if (user != null)
            {
                UserLost.Raise(this, user);
            }
        }

        void _UserGenerator_NewUser(object sender, NewUserEventArgs e)
        {
            if(_FindUser(e.ID) == null)
            {
                //new user
                var user = new KinectUser(e.ID);

                lock (_UserLock)
                {
                    this.Users.Add(e.ID, user);
                }

                UserFound.Raise(this, user);

                //use the first lot of calibration data loaded for all users if available               
                if (System.IO.File.Exists(this.CalibrationPath))
                {
                    try
                    {
                        lock (_SkeletonLock)
                        {
                            _SkeletonCapability.LoadCalibrationDataFromFile(e.ID, this.CalibrationPath);
                            _SkeletonCapability.StartTracking(e.ID);

                            user.IsCalibrated = true;

                            SkeletonReady.Raise(this, user);
                        }
                    }
                    catch(StatusException)
                    {
                        //need to detect the pose
                        _PoseDetectionCapability.StartPoseDetection(_CalibrationPose, e.ID);
                    }
                }
                else
                {
                    //need to detect the pose
                    _PoseDetectionCapability.StartPoseDetection(_CalibrationPose, e.ID);
                }
            }
        }

        private KinectUser _FindUser(int id)
        {
            lock (_UserLock)
            {
                if (this.Users.ContainsKey(id))
                {
                    return this.Users[id];
                }
            }

            return null;
        }
        
        public void Update()
        {
            _UpdateJoints();
        }

        private void _UpdateJoints()
        {
            lock (_UserLock)
            {
                foreach (var user in this.Users)
                {
                    lock (_SkeletonLock)
                    {
                        if (_SkeletonCapability.IsTracking(user.Key))
                        {                            
                            foreach (var joint in user.Value.Joints.Keys)
                            {
                                var kinectJoint = user.Value.Joints[joint];
                                kinectJoint.IsActive = _SkeletonCapability.IsJointActive((SkeletonJoint)joint);
                                kinectJoint.IsAvailable = _SkeletonCapability.IsJointAvailable((SkeletonJoint)joint);

                                if (kinectJoint.IsAvailable)
                                {
                                    var jointTransformation = _SkeletonCapability.GetSkeletonJoint(user.Key, (SkeletonJoint)joint);

                                    kinectJoint.Position = jointTransformation.Position.Position.ToVector3();
                                    kinectJoint.PositionConfidence = jointTransformation.Position.Confidence;

                                    var rotationMatrix = Matrix.Identity;

                                    rotationMatrix.Right = new Vector3(jointTransformation.Orientation.X1, jointTransformation.Orientation.X2, jointTransformation.Orientation.X3);
                                    rotationMatrix.Up = new Vector3(jointTransformation.Orientation.Y1, jointTransformation.Orientation.Y2, jointTransformation.Orientation.Y3);
                                    rotationMatrix.Forward = new Vector3(jointTransformation.Orientation.Z1, jointTransformation.Orientation.Z2, jointTransformation.Orientation.Z3);

                                    kinectJoint.RotationMatrix = rotationMatrix;
                                    kinectJoint.RotationConfidence = jointTransformation.Orientation.Confidence;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _UserGenerator.NewUser -= new EventHandler<NewUserEventArgs>(_UserGenerator_NewUser);
            _UserGenerator.LostUser -= new EventHandler<UserLostEventArgs>(_UserGenerator_LostUser);
            _PoseDetectionCapability.PoseDetected -= new EventHandler<PoseDetectedEventArgs>(PoseDetectionCapability_PoseDetected);
            _SkeletonCapability.CalibrationComplete -= new EventHandler<CalibrationProgressEventArgs>(_SkeletonCapability_CalibrationComplete);

            lock (_UserLock)
            {
                foreach (var user in this.Users.Values)
                {
                    if (_SkeletonCapability.IsTracking(user.UserId))
                    {
                        _SkeletonCapability.StopTracking(user.UserId);
                    }
                }

                this.Users.Clear();
            }
            
            _UserGenerator.Dispose();
        }
    }
}
