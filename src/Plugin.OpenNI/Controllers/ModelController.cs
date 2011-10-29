using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plugin.OpenNI;
using Plugin.OpenNI.Model;
using VastPark.PluginFramework.Controllers;
using VastPark.Data;
using VastPark.FrameworkBase.Math;

namespace Plugin.OpenNI.Controllers
{
    /// <summary>
    /// Synchronises a KinectUser with a Model
    /// </summary>
    public class ModelController : IController
    {
        public KinectUser User { get; private set; }

        public VastPark.Imml.Scene.Controls.Model Model { get; private set; }

        public bool EnableBlending { get; set; }

        public ModelController(KinectUser user, VastPark.Imml.Scene.Controls.Model model)
        {
            this.User = user;
            this.Model = model;
            this.EnableBlending = true;
        }

        public bool Enabled { get; set; }

        public void Update()
        {
            if (!this.User.IsCalibrated)
            {
                return;
            }

            //TODO: Currently only supports rotation, also look at supporting position so that the model can move

            foreach (var joint in this.User.Joints.Values)
            {
                var bone = _FindBone(joint.Name);

                if (bone != null && joint.RotationConfidence == 1f) //must be 100% confident of the rotation
                {
                    Vector3 oldTranslation;
                    Quaternion oldQuaternion;
                    Vector3 oldScale;

                    bone.RelativeMatrix.Decompose(out oldScale, out oldQuaternion, out oldTranslation);

                    //invert for right hand coordinate system
                    oldQuaternion.I *= -1f;
                    oldQuaternion.J *= -1f;
                    oldQuaternion.K *= 1f;
                    oldQuaternion.W *= 1f;

                    oldScale.X *= 1;
                    oldScale.Y *= 1;
                    oldScale.Z *= -1;

                    oldTranslation.X *= 1f;
                    oldTranslation.Y *= 1f;
                    oldTranslation.Z *= -1f;

                    var scaleMatrix = Matrix.Scaling(oldScale);// this.EnableBlending ? Matrix.Scaling(Vector3.Lerp(oldScale, scale, groupController.Weight)) : Matrix.Scaling(scale);
                    //var rotationMatrix = this.EnableBlending ? Matrix.RotationQuaternion(Quaternion.Slerp(oldQuaternion, new Quaternion(, groupController.Weight)) : Matrix.RotationQuaternion(rotation);
                    //var rotationMatrix = Matrix.RotationQuaternion(oldQuaternion);// this.EnableBlending ? Matrix.RotationQuaternion(Quaternion.Slerp(oldQuaternion, join, groupController.Weight)) : Matrix.RotationQuaternion(rotation);
                    //var translationMatrix = this.EnableBlending ? Matrix.CreateTranslation(Vector3.Lerp(oldTranslation, kinectTranslation, 1)) : Matrix.CreateTranslation(kinectTranslation);

                    bone.RelativeMatrix = Matrix.Multiply(Matrix.Multiply(scaleMatrix, joint.RotationMatrix), Matrix.CreateTranslation(oldTranslation));
                    this.Model.UpdateBone(bone);
                }
            }
        }

        private BoneNode _FindBone(string joint)
        {
            var bones = this.Model.GetBones();

            switch (joint)
            {
                case "Head":
                    return bones["Bone_Head"];
                case "Neck":
                    return bones["Bone_Neck"];
                //case "RightCollar":
                //    return bones["Bone_Clavicle_R"];
                //case "RightShoulder":
                //    return bones["Bone_UpperArm_R"];
                //case "RightElbow":
                //    return bones["Bone_ForeArm_R"];
                ////case "RightWrist":
                ////    return bones[""];
                //case "RightHand":
                //    return bones["Bone_Hand_R"];
                //case "RightFingertip":
                //    return bones["Bone_Finger_01_R"];
                //case "LeftCollar":
                //    return bones["Bone_Clavicle_L"];
                //case "LeftShoulder":
                //    return bones["Bone_UpperArm_L"];
                //case "LeftElbow":
                //    return bones["Bone_ForeArm_L"];
                ////case "LeftWrist":
                ////    return bones[""];
                //case "LeftHand":
                //    return bones["Bone_Hand_L"];
                //case "LeftFingertip":
                //    return bones["Bone_Finger_01_L"];

                //    //legs
                //case "LeftHip":
                //    return bones["Bone_Thigh_L"];
                //case "LeftKnee":
                //    return bones["Bone_Calf_L"];
                ////case "LeftAnkle":
                ////    return bones["Bone_Thigh_L"];
                //case "LeftFoot":
                //    return bones["Bone_Foot_L"];

            }

            return null;
        }

        public void Dispose()
        {
            
        }
    }
}
