using System;
using Iviz.App;
using Iviz.App.Listeners;
using Iviz.Displays;
using UnityEngine;

namespace Iviz.Displays
{
    public sealed class InteractiveControl : MonoBehaviour
    {
        public enum Orientation
        {
            Inherit,
            Fixed,
            ViewFacing
        }

        public enum InteractionModeType
        {
            Disabled,
            
            MoveAxisX,
            MovePlaneYZ,
            RotateAxisX,
            MovePlaneYZ_RotateAxisX,
            Frame,

            Move3D,
            Rotate3D,
            MoveRotate3D
        }

        [SerializeField] MeshMarkerResource arrowPX = null;
        [SerializeField] MeshMarkerResource arrowMX = null;
        [SerializeField] MeshMarkerResource arrowPY = null;
        [SerializeField] MeshMarkerResource arrowMY = null;
        [SerializeField] MeshMarkerResource arrowPZ = null;
        [SerializeField] MeshMarkerResource arrowMZ = null;

        [SerializeField] MeshMarkerResource ringX = null;
        [SerializeField] MeshMarkerResource ringY = null;
        [SerializeField] MeshMarkerResource ringZ = null;

        [SerializeField] MeshMarkerResource ringXPlane = null;
        [SerializeField] MeshMarkerResource ringZPlane = null;

        MeshMarkerResource[] allResources;

        public delegate void MovedAction(in Pose pose);

        public event MovedAction Moved;

        Transform targetTransform;

        public Transform TargetTransform
        {
            get => targetTransform;
            set
            {
                targetTransform = value;
                foreach (MeshMarkerResource resource in allResources)
                {
                    resource.GetComponent<IDraggable>().TargetTransform = value;
                }
            }
        }

        bool pointsToCamera;

        public bool PointsToCamera
        {
            get => pointsToCamera;
            set
            {
                if (value == pointsToCamera)
                {
                    return;
                }

                pointsToCamera = value;
                if (value)
                {
                    GameThread.LateEveryFrame += RotateToCamera;
                }
                else
                {
                    GameThread.LateEveryFrame -= RotateToCamera;
                    CameraPivotIsParent = false;
                    transform.localRotation = Quaternion.identity;
                }

                foreach (MeshMarkerResource resource in allResources)
                {
                    var draggable = resource.GetComponent<DraggableRotation>();
                    if (!(draggable is null))
                    {
                        draggable.DoesRotationReset = value;
                    }
                }
            }
        }

        bool keepAbsoluteRotation;

        public bool KeepAbsoluteRotation
        {
            get => keepAbsoluteRotation;
            set
            {
                keepAbsoluteRotation = value;
                if (value)
                {
                    GameThread.LateEveryFrame += RotateBackToFixed;
                }
                else
                {
                    GameThread.LateEveryFrame -= RotateBackToFixed;
                    transform.localRotation = Quaternion.identity;
                }

                foreach (MeshMarkerResource resource in allResources)
                {
                    var draggable = resource.GetComponent<DraggableRotation>();
                    if (!(draggable is null))
                    {
                        draggable.DoesRotationReset = value;
                    }
                }
            }
        }

        bool cameraPivotIsParent;

        public bool CameraPivotIsParent
        {
            get => cameraPivotIsParent;
            set
            {
                cameraPivotIsParent = value;
                if (!value)
                {
                    transform.localRotation = Quaternion.identity;
                }
            }
        }

        InteractionModeType interactionMode;

        public InteractionModeType InteractionMode
        {
            get => interactionMode;
            set
            {
                interactionMode = value;

                foreach (MeshMarkerResource resource in allResources)
                {
                    resource.Visible = false;
                }

                switch (InteractionMode)
                {
                    case InteractionModeType.Disabled:
                        break;
                    case InteractionModeType.MoveAxisX:
                        arrowPX.Visible = true;
                        arrowMX.Visible = true;
                        break;
                    case InteractionModeType.MovePlaneYZ:
                        arrowPY.Visible = true;
                        arrowMY.Visible = true;
                        arrowPZ.Visible = true;
                        arrowMZ.Visible = true;
                        ringXPlane.Visible = true;
                        break;
                    case InteractionModeType.RotateAxisX:
                        ringX.Visible = true;
                        break;
                    case InteractionModeType.MovePlaneYZ_RotateAxisX:
                        arrowPY.Visible = true;
                        arrowMY.Visible = true;
                        arrowPZ.Visible = true;
                        arrowMZ.Visible = true;
                        ringX.Visible = true;
                        ringXPlane.Visible = true;
                        break;
                    case InteractionModeType.Frame:
                        arrowPX.Visible = true;
                        arrowMX.Visible = true;
                        arrowPY.Visible = true;
                        arrowMY.Visible = true;
                        arrowPZ.Visible = true;
                        arrowMZ.Visible = true;
                        ringZ.Visible = true;
                        ringZPlane.Visible = true;
                        break;
                    case InteractionModeType.Move3D:
                        arrowPX.Visible = true;
                        arrowMX.Visible = true;
                        arrowPY.Visible = true;
                        arrowMY.Visible = true;
                        arrowPZ.Visible = true;
                        arrowMZ.Visible = true;
                        ringZPlane.Visible = true;
                        break;
                    case InteractionModeType.Rotate3D:
                        ringX.Visible = true;
                        ringY.Visible = true;
                        ringZ.Visible = true;
                        break;
                    case InteractionModeType.MoveRotate3D:
                        arrowPX.Visible = true;
                        arrowMX.Visible = true;
                        arrowPY.Visible = true;
                        arrowMY.Visible = true;
                        arrowPZ.Visible = true;
                        arrowMZ.Visible = true;
                        ringX.Visible = true;
                        ringY.Visible = true;
                        ringZ.Visible = true;
                        break;
                }
            }
        }

        void OnMoved(in Pose pose)
        {
            //Debug.Log(pose);
            Moved?.Invoke(pose);
        }

        public void Stop()
        {
            PointsToCamera = false;
            KeepAbsoluteRotation = false;
        }

        void Awake()
        {
            allResources = new[]
                {arrowPX, arrowMX, arrowPY, arrowMY, arrowPZ, arrowMZ, ringX, ringY, ringZ, ringXPlane, ringZPlane};

            foreach (MeshMarkerResource resource in allResources)
            {
                resource.GetComponent<IDraggable>().Moved += OnMoved;
            }

            InteractionMode = InteractionModeType.MovePlaneYZ;
            if (TargetTransform is null)
            {
                TargetTransform = transform.parent ?? transform;
            }
        }

        void RotateToCamera()
        {
            Vector3 cameraForward = TFListener.MainCamera.transform.forward;
            if (CameraPivotIsParent)
            {
                transform.parent.LookAt(transform.parent.position + cameraForward);
            }
            else
            {
                transform.LookAt(transform.position + cameraForward);
            }
        }

        void RotateBackToFixed()
        {
            transform.rotation = TFListener.RootFrame.transform.rotation;
        }
    }
}