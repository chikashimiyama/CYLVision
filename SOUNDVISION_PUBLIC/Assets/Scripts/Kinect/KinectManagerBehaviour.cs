﻿using System;
using System.IO;
using System.Linq;
using Windows.Kinect;
using UnityEngine;
using UnityEngine.Events;

namespace cylvester
{
    [Serializable] public class UnityDepthCameraEvent : UnityEvent<Texture2D>{ }
    [Serializable] public class UnitySkeletonEvent : UnityEvent<Body, int>{ }
    
    public class KinectManagerBehaviour : MonoBehaviour
    {
        [SerializeField] private bool depth;
        [SerializeField] public UnityDepthCameraEvent depthFrameReceived;

        [SerializeField] private bool skeleton;
        [SerializeField] public UnitySkeletonEvent skeletonDataReceived;

        [SerializeField, Range(1, 6)] private int numberOfBodiesTobeTracked = 2;
        
        private KinectSensor sensor_;
        private DepthFrameReader depthFrameReader_;
        private BodyFrameReader bodyFrameReader_;
        private BodyIndexFrameReader bodyIndexFrameReader_;

        private ushort [] depthData_;
        private Texture2D depthTexture_;

        //QUICK CODE FROM FRIEDRICH - SORRY FOR EVERYTHING!!!
        //private byte[] depthData8bit_;
        //private Texture2D depthTexture8bit_;
        //public ushort front;
        //public ushort back;
        //FK CODE ENDS

        private Body[] bodies_;
        
        private EventHandler<DepthFrameArrivedEventArgs> onDepthFrameArrived_;
        private EventHandler<BodyFrameArrivedEventArgs> onBodyFrameArrived_;
        private EventHandler<BodyIndexFrameArrivedEventArgs> onBodyIndexFrameArrived_;
        private BodyHolder trackedBodies_;
        
        private void Start()
        {
            sensor_ = KinectSensor.GetDefault();
            if (sensor_ == null)
                throw new IOException("cannot find Kinect Sensor ");
            
            InitDepthCamera();
            InitSkeletonTracking();

            if (!sensor_.IsOpen)
            {
                sensor_.Open();
            }
        }

        private void InitDepthCamera()
        {
            
            depthFrameReader_ = sensor_.DepthFrameSource.OpenReader();
            var frameDesc = sensor_.DepthFrameSource.FrameDescription;
            depthData_ = new ushort[frameDesc.LengthInPixels];
            depthTexture_ = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.R16, false);

            //FRIEDRICH QUICK CODE FROM HERE
            //depthData8bit_ = new byte[frameDesc.LengthInPixels];
            //depthTexture8bit_ = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.Alpha8, false); 
            //END FRIEDRICH

            onDepthFrameArrived_ = (frameReader, eventArgs) =>
            {
                if(!depth)
                    return;
                
                using (var depthFrame = eventArgs.FrameReference.AcquireFrame())
                {
                    if (depthFrame == null) 
                        return;
                    
                    depthFrame.CopyFrameDataToArray(depthData_);
                    unsafe
                    {
                        fixed (ushort* irDataPtr = depthData_)
                        {
                            depthTexture_.LoadRawTextureData((IntPtr) irDataPtr, sizeof(ushort) * depthData_.Length);
                        }
                    }

                    //SUPER DUPER QUICK FRIEDRICH CODE

                    //float distanceFrontBack = back - front;
                    //calculation should be: 
                    //result = incoming data - front 
                    //result = result * 255/distanceFrontBack

                    //for (int i = 0; i < depthData_.Length; i++)
                    //{
                    //    float value = (float)(depthData_[i] - front);
                    //    value = value * 255.0f / distanceFrontBack;

                    //    depthData8bit_[i] = Convert.ToByte(value);
                    //}
                    //depthTexture8bit_.Apply();

                    //END SUPER QUICK FRIEDRICH CODE

                    depthTexture_.Apply();
                }
                depthFrameReceived.Invoke(depthTexture_);

            };
            depthFrameReader_.FrameArrived += onDepthFrameArrived_;
        }

        private void InitSkeletonTracking()
        {
            bodies_ = new Body[6];
            trackedBodies_ = new BodyHolder(numberOfBodiesTobeTracked);
            InitBodyFrameReader();
        }

        private void InitBodyFrameReader()
        {
            bodyFrameReader_ = sensor_.BodyFrameSource.OpenReader();
            onBodyFrameArrived_ = (frameReader, eventArgs) =>
            {
                if(!skeleton)
                    return;

                using (var bodyFrame = eventArgs.FrameReference.AcquireFrame())
                {
                    if (bodyFrame == null)
                        return;
                    
                    bodyFrame.GetAndRefreshBodyData(bodies_);
                    foreach (var body in bodies_.Where(t => t.IsTracked))
                    {
                        if (body.IsTracked)
                        {
                            if (trackedBodies_.Exist(body))
                            {
                                var idNumber = trackedBodies_.IndexOf(body);
                                if (idNumber.HasValue)
                                    skeletonDataReceived.Invoke(body, idNumber.Value);
                            }
                            else
                            {
                                if (trackedBodies_.Add(body))
                                {
                                    var idNumber = trackedBodies_.IndexOf(body);
                                    if (idNumber.HasValue)
                                        skeletonDataReceived.Invoke(body, idNumber.Value);
                                }
                            }
                        }
                    }

                    foreach (var body in bodies_.Where(t => !t.IsTracked && trackedBodies_.Exist(t)))
                    {
                        trackedBodies_.Remove(body);
                    }
                }
            };
            bodyFrameReader_.FrameArrived += onBodyFrameArrived_;
        }
        
        private void OnDestroy()
        {
            depthFrameReader_.FrameArrived -= onDepthFrameArrived_;
            bodyFrameReader_.FrameArrived -= onBodyFrameArrived_;
        }
    }
}


