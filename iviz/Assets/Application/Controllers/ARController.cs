﻿using UnityEngine;
using Iviz.Roslib;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using Application.Displays;
using Iviz.App;
using Iviz.Resources;
using UnityEngine.XR.ARFoundation;
using Iviz.Displays;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace Iviz.Controllers
{
    [DataContract]
    public sealed class ARConfiguration : JsonToString, IConfiguration
    {
        [DataMember] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember] public Resource.Module Module => Resource.Module.AugmentedReality;
        [DataMember] public bool Visible { get; set; } = true;
        /* NonSerializable */ public float WorldScale { get; set; } = 1.0f;
        /* NonSerializable */ public SerializableVector3 WorldOffset { get; set; } = ARController.DefaultWorldOffset;
        /* NonSerializable */ public float WorldAngle { get; set; } = 0;
        [DataMember] public bool SearchMarker { get; set; } = false;
        [DataMember] public bool MarkerHorizontal { get; set; } = true;
        [DataMember] public int MarkerAngle { get; set; } = 0;
        [DataMember] public string MarkerFrame { get; set; } = "";
        [DataMember] public SerializableVector3 MarkerOffset { get; set; } = Vector3.zero;

        /* NonSerializable */ public bool ShowRootMarker { get; set; }
        /* NonSerializable */ public bool PinRootMarker { get; set; }
    }
    
    [DataContract]
    public sealed class ARSessionInfo : JsonToString
    {
        [DataMember] public float WorldScale { get; set; } = 1.0f;
        [DataMember] public SerializableVector3 WorldOffset { get; set; } = Vector3.zero;
        [DataMember] public float WorldAngle { get; set; } = 0;
        [DataMember] public bool ShowRootMarker { get; set; }
        [DataMember] public bool PinRootMarker { get; set; }
    }

    public sealed class ARController : MonoBehaviour, IController, IHasFrame, IAnchorProvider
    {
        public static readonly Vector3 DefaultWorldOffset = new Vector3(0.5f, 0, -0.2f);

        public static ARController Instance;
        
        static ARSessionInfo savedSessionInfo;
        
        [SerializeField] Camera ARCamera = null;
        [SerializeField] ARSessionOrigin ARSessionOrigin = null;
        [SerializeField] Light ARLight = null;
        [SerializeField] Canvas canvas = null;
        [SerializeField] Camera mainCamera = null;

        ARPlaneManager planeManager;
        ARTrackedImageManager tracker;
        ARRaycastManager raycaster;

        DisplayClickableNode node;
        ARMarkerResource resource;

        //readonly MeshToMarkerHelper helper = new MeshToMarkerHelper("ar");
        //DateTime lastMarkerUpdate = DateTime.MinValue;

        static Transform TFRoot => TFListener.RootFrame.transform;

        public IModuleData ModuleData { get; set; }

        readonly ARConfiguration config = new ARConfiguration();
        public ARConfiguration Config
        {
            get => config;
            set
            {
                Visible = value.Visible;
                //WorldOffset = value.WorldOffset;
                WorldScale = value.WorldScale;
                //PublishPose = value.PublishPose;
                //PublishPlanesAsMarkers = value.PublishMarkers;
                UseMarker = value.SearchMarker;
                MarkerHorizontal = value.MarkerHorizontal;
                MarkerAngle = value.MarkerAngle;
                MarkerFrame = value.MarkerFrame;
                MarkerOffset = value.MarkerOffset;
            }
        }

        public Vector3 WorldOffset
        {
            get => config.WorldOffset;
            set
            {
                config.WorldOffset = value;
                TFRoot.SetPose(RootPose);
            }
        }
        
        public float WorldAngle
        {
            get => config.WorldAngle;
            set
            {
                config.WorldAngle = value;
                TFRoot.SetPose(RootPose);
            }
        }

        public float WorldScale
        {
            get => config.WorldScale;
            set
            {
                config.WorldScale = value;
                TFRoot.localScale = value * Vector3.one;
            }
        }

        public bool Visible
        {
            get => config.Visible;
            set
            {
                config.Visible = value;
                resource.Visible = value && UseMarker;
                mainCamera.gameObject.SetActive(!value);
                ARCamera.gameObject.SetActive(value);
                //TFListener.MainLight.gameObject.SetActive(!value);
                ARLight.gameObject.SetActive(value);
                canvas.worldCamera = value ? ARCamera : mainCamera;
                TFListener.MainCamera = value ? ARCamera : mainCamera;

                TFRoot.SetPose(value ? RootPose : Pose.identity);

                ModuleListPanel.Instance.OnARModeChanged(value);
                foreach (var module in ModuleListPanel.Instance.ModuleDatas)
                {
                    module.OnARModeChanged(value);
                }
                
                TFListener.MapFrame.UpdateAnchor(null, value);
            }
        }

        /*
        private void AnchorManager_anchorsChanged(ARAnchorsChangedEventArgs obj)
        {
            if (obj.added.Count != 0)
            {
                Debug.Log("Added " + obj.added.Count + " anchors!");
                trackedObject = obj.added[0].gameObject;
            }
            if (obj.updated.Count != 0)
            {
                Debug.Log("Updated " + obj.updated.Count + " anchors!");
                trackedObject = obj.updated[0].gameObject;
            }
        }
        */

        /*
        public bool PublishPose
        {
            get => config.PublishPose;
            set
            {
                config.PublishPose = value;
                if (value && RosSenderHead == null)
                {
                    RosSenderHead = new RosSender<Msgs.GeometryMsgs.PoseStamped>(HeadPoseTopic);
                }
            }
        }
        */

        /*
        public bool PublishPlanesAsMarkers
        {
            get => config.PublishMarkers;
            set
            {
                config.PublishMarkers = value;
                planeManager.enabled = value;
                if (value && RosSenderMarkers == null)
                {
                    RosSenderMarkers = new RosSender<MarkerArray>(MarkersTopic);
                }
            }
        }
        */

        bool MarkerFound { get; set; }

        public bool UseMarker
        {
            get => config.SearchMarker;
            set
            {
                config.SearchMarker = value;
                tracker.enabled = value;
                resource.Visible = Visible && value;
                if (value)
                {
                    TFRoot.SetPose(RegisteredPose);
                    tracker.trackedImagesChanged += OnTrackedImagesChanged;
                }
                else
                {
                    WorldOffset = WorldOffset;
                    tracker.trackedImagesChanged -= OnTrackedImagesChanged;
                }
            }
        }

        public bool MarkerHorizontal
        {
            get => config.MarkerHorizontal;
            set
            {
                config.MarkerHorizontal = value;
                resource.Horizontal = value;
            }
        }

        public int MarkerAngle
        {
            get => config.MarkerAngle;
            set
            {
                config.MarkerAngle = value;
                resource.Angle = value; // deg
            }
        }

        public string MarkerFrame
        {
            get => config.MarkerFrame;
            set
            {
                config.MarkerFrame = value;
                node.AttachTo(config.MarkerFrame);
            }
        }

        public TFFrame Frame => node.Parent;

        public Vector3 MarkerOffset
        {
            get => config.MarkerOffset;
            set
            {
                config.MarkerOffset = value;
                resource.Offset = value.Ros2Unity();
            }
        }

        Pose RegisteredPose { get; set; } = Pose.identity;

        Pose RootPose
        {
            get
            {
                //Pose pose = RegisteredPose;
                Pose offsetPose = new Pose(
                    WorldOffset.Ros2Unity(),
                    Quaternion.AngleAxis(WorldAngle, Vector3.up)
                    );
                //Debug.Log("setting " + WorldOffset.Ros2Unity());
                //offsetPose.position += offsetPose.rotation * WorldOffset.Ros2Unity();
                return RegisteredPose.Multiply(offsetPose);
            }
        }

        public bool PinRootMarker
        {
            get => config.PinRootMarker;
            set => config.PinRootMarker = value;
        }
        
        public bool ShowRootMarker
        {
            get => config.ShowRootMarker;
            set => config.ShowRootMarker = value;
        }
        
        void Awake()
        {
            Instance = this;
            
            if (canvas == null)
            {
                canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
            }
            if (mainCamera == null)
            {
                mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
            }
            if (ARCamera == null)
            {
                ARCamera = GameObject.Find("AR Camera").GetComponent<Camera>();
            }
            if (ARSessionOrigin == null)
            {
                ARSessionOrigin = GameObject.Find("AR Session Origin").GetComponent<ARSessionOrigin>();
            }

            planeManager = ARSessionOrigin.GetComponent<ARPlaneManager>();
            planeManager.planesChanged += OnPlanesChanged;
            
            tracker = ARSessionOrigin.GetComponent<ARTrackedImageManager>();
            raycaster = ARSessionOrigin.GetComponent<ARRaycastManager>();

            var cameraManager = ARCamera.GetComponent<ARCameraManager>();
            cameraManager.frameReceived += args =>
            {
                UpdateLights(args.lightEstimation);
            };
            
            node = DisplayClickableNode.Instantiate("AR Node");
            resource = ResourcePool.GetOrCreate<ARMarkerResource>(Resource.Displays.ARMarkerResource);
            node.Target = resource;
            MarkerFound = false;

            Config = new ARConfiguration();
            //Visible = true;

            if (savedSessionInfo != null)
            {
                WorldAngle = savedSessionInfo.WorldAngle;
                WorldOffset = savedSessionInfo.WorldOffset;
                WorldScale = savedSessionInfo.WorldScale;
            }
        }

        bool forceAnchorRebuild;
        void OnPlanesChanged(ARPlanesChangedEventArgs obj)
        {
            forceAnchorRebuild = true;
            //Debug.Log("force rebuild");
        }

        void HideAnchors()
        {
            TFListener.MapFrame.UpdateAnchor(null);
        }

        public bool FindAnchor(in Vector3 position, out Vector3 anchor, out Vector3 normal)
        {
            List<ARRaycastHit> results = new List<ARRaycastHit>();
            Vector3 origin = position + 0.25f * Vector3.up;
            raycaster.Raycast(new Ray(origin, Vector3.down), results, TrackableType.PlaneWithinBounds);
            if (results.Count == 0)
            {
                anchor = position;
                normal = Vector3.zero;
                return false;
            }

            var hit = results[0];
            var plane = planeManager.GetPlane(hit.trackableId);
            anchor = hit.pose.position;
            normal = plane.normal;
            return true;
        }
        
        void UpdateAnchors()
        {
            
            //foreach (TFFrame frame in TFListener.Instance.Frames.Values)
            //{
            //    frame.UpdateAnchor(FindAnchorFn, forceAnchorRebuild);    
            //}
            
            Vector3? projection = TFListener.MapFrame.UpdateAnchor(this, forceAnchorRebuild);
            if (PinRootMarker && projection.HasValue)
            {
                TFListener.RootFrame.transform.position = projection.Value;
                ((ARModuleData)ModuleData).CopyControlMarkerPose();
            }
            forceAnchorRebuild = false;
        }

        void UpdateLights(ARLightEstimationData lightEstimation)
        {
            ARLight.intensity = 1;
            /*
            if (lightEstimation.averageBrightness.HasValue)
            {
                ARLight.intensity = lightEstimation.averageBrightness.Value;
            }
            */

            /*
            if (lightEstimation.averageColorTemperature.HasValue)
            {
                ARLight.colorTemperature = lightEstimation.averageColorTemperature.Value;
            } 
            */           
            
            /*
            if (lightEstimation.colorCorrection.HasValue)
            {
                ARLight.color = lightEstimation.colorCorrection.Value;
            }
            */

            if (lightEstimation.mainLightDirection.HasValue)
            {
                ARLight.transform.rotation = Quaternion.LookRotation(lightEstimation.mainLightDirection.Value);
            }

            if (lightEstimation.mainLightColor.HasValue)
            {
                ARLight.color = lightEstimation.mainLightColor.Value;
            }

            if (lightEstimation.ambientSphericalHarmonics.HasValue)
            {
                var sphericalHarmonics = lightEstimation.ambientSphericalHarmonics;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = sphericalHarmonics.Value;
            }            
        }
        
        //uint headSeq = 0;
        public void Update()
        {
            UpdateAnchors();
        }

        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs obj)
        {
            Pose? newPose = null;
            
            if (obj.added.Count != 0)
            {
                newPose = obj.added[0].transform.AsPose();
            }
            if (obj.updated.Count != 0)
            {
                newPose = obj.updated[0].transform.AsPose();
            }
            if (newPose == null)
            {
                return;
            }
            
            WorldOffset = Vector3.zero;
            WorldAngle = 0;

            Pose expectedPose = TFListener.RelativePose(resource.transform.AsPose());
            Pose registeredPose = newPose.Value.Multiply(expectedPose.Inverse());
            Quaternion corrected = new Quaternion(0, registeredPose.rotation.y, 0, registeredPose.rotation.w).normalized;

            //Debug.Log("Registration! " + registeredPose.rotation + " -> " + corrected);

            registeredPose.rotation = corrected;
            
            RegisteredPose = registeredPose;
            
            MarkerFound = true;
            TFRoot.SetPose(RootPose);
            
            ModuleData.ResetPanel();
        }

        public void Stop()
        {
            savedSessionInfo = new ARSessionInfo()
            {
                WorldAngle = WorldAngle,
                WorldOffset = WorldOffset,
                WorldScale = WorldScale
            };
            
            Visible = false;
            //RosSenderHead?.Stop();
            //RosSenderMarkers?.Stop();

            WorldScale = 1;
            TFRoot.SetPose(Pose.identity);

            Instance = null;
        }

        void IController.Reset()
        {
        }
    }
}
