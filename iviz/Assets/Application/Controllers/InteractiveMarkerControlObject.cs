﻿using UnityEngine;
using System.Collections.Generic;
using Iviz.Core;
using Iviz.Displays;
using Iviz.Msgs.VisualizationMsgs;
using Iviz.Resources;

namespace Iviz.Controllers
{
    public sealed class InteractiveMarkerControlObject : MonoBehaviour
    {
        //string Description { get; set; }
        //string Id { get; set; }

        readonly Dictionary<string, MarkerObject> markers = new Dictionary<string, MarkerObject>();
        readonly HashSet<string> markersToDelete = new HashSet<string>();

        bool markerIsInteractive;
        InteractiveControl control;
        GameObject markerNode;

        public delegate void MouseEventAction(in Pose pose, in Vector3 point, MouseEventType type);

        public event MouseEventAction MouseEvent;
        public event InteractiveControl.MovedAction Moved;

        void Awake()
        {
            markerNode = new GameObject("[MarkerNode]");
            markerNode.transform.SetParent(transform, false);
            markerNode.AddComponent<Billboard>().enabled = false;
        }
        
        public void Set(InteractiveMarkerControl msg)
        {
            //Description = msg.Description;
            name = msg.Name;
            //Id = msg.Name;

            transform.localRotation = msg.Orientation.Ros2Unity();
            //Debug.Log("--------------");
            //Debug.Log(transform.localRotation);
            //Debug.Log(transform.rotation);

            UpdateMarkers(msg.Markers);
            UpdateInteractionMode(msg.InteractionMode, msg.OrientationMode, msg.IndependentMarkerOrientation);
        }

        void EnsureControlDisplayExists()
        {
            if (control != null)
            {
                return;
            }

            control = ResourcePool.GetOrCreate<InteractiveControl>(Resource.Displays.InteractiveControl, transform);
            control.TargetTransform = transform.parent;
            control.Moved += (in Pose pose) => Moved?.Invoke(transform.AsPose());
        }

        void UpdateInteractionMode(int interactionMode, int orientationMode, bool independentMarkerOrientation)
        {
            switch (interactionMode)
            {
                case InteractiveMarkerControl.NONE:
                    markerIsInteractive = false;
                    markers.Values.ForEach(marker => marker.Clickable = false);
                    break;
                case InteractiveMarkerControl.MENU:
                case InteractiveMarkerControl.BUTTON:
                    markerIsInteractive = true;
                    markers.Values.ForEach(marker => marker.Clickable = true);
                    break;
                case InteractiveMarkerControl.MOVE_AXIS:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.MoveAxisX;
                    break;
                case InteractiveMarkerControl.MOVE_PLANE:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.MovePlaneYZ;
                    break;
                case InteractiveMarkerControl.ROTATE_AXIS:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.RotateAxisX;
                    break;
                case InteractiveMarkerControl.MOVE_ROTATE:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.MovePlaneYZ_RotateAxisX;
                    break;
                case InteractiveMarkerControl.MOVE_3D:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.Move3D;
                    break;
                case InteractiveMarkerControl.ROTATE_3D:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.Rotate3D;
                    break;
                case InteractiveMarkerControl.MOVE_ROTATE_3D:
                    EnsureControlDisplayExists();
                    control.InteractionMode = InteractiveControl.InteractionModeType.MoveRotate3D;
                    break;
            }

            switch (orientationMode)
            {
                case InteractiveMarkerControl.VIEW_FACING:
                    markerNode.GetComponent<Billboard>().enabled = true;
                    break;
                case InteractiveMarkerControl.INHERIT:
                case InteractiveMarkerControl.FIXED:
                    markerNode.GetComponent<Billboard>().enabled = false;
                    markerNode.transform.localRotation = Quaternion.identity;
                    break;
            }

            if (control is null)
            {
                return;
            }

            switch (orientationMode)
            {
                case InteractiveMarkerControl.VIEW_FACING:
                    control.KeepAbsoluteRotation = false;
                    control.PointsToCamera = true;
                    control.CameraPivotIsParent = !independentMarkerOrientation;
                    break;
                case InteractiveMarkerControl.INHERIT:
                    control.PointsToCamera = false;
                    control.KeepAbsoluteRotation = false;
                    break;
                case InteractiveMarkerControl.FIXED:
                    control.PointsToCamera = false;
                    control.KeepAbsoluteRotation = true;
                    break;
            }
        }

        void UpdateMarkers(Marker[] msg)
        {
            markersToDelete.Clear();
            foreach (string id in markers.Keys)
            {
                markersToDelete.Add(id);
            }

            foreach (Marker marker in msg)
            {
                string id = MarkerListener.IdFromMessage(marker);
                switch (marker.Action)
                {
                    case Marker.ADD:
                        if (!markers.TryGetValue(id, out MarkerObject markerToAdd))
                        {
                            markerToAdd = CreateMarkerObject();
                            markerToAdd.MouseEvent += OnMarkerClicked;
                            markers[id] = markerToAdd;
                        }
                        markerToAdd.Set(marker);
                        if (marker.Header.FrameId.Length == 0)
                        {
                            markerToAdd.transform.SetParentLocal(markerNode.transform);
                        }

                        markersToDelete.Remove(id);
                        break;
                    case Marker.DELETE:
                        if (markers.TryGetValue(id, out MarkerObject markerToDelete))
                        {
                            DeleteMarkerObject(markerToDelete);
                            markers.Remove(id);
                            markersToDelete.Remove(id);
                        }

                        break;
                }
            }

            foreach (string id in markersToDelete)
            {
                MarkerObject markerToDelete = markers[id];
                DeleteMarkerObject(markerToDelete);
                markers.Remove(id);
            }
        }

        void OnMarkerClicked(in Vector3 point, MouseEventType type)
        {
            if (!markerIsInteractive)
            {
                return;
            }

            MouseEvent?.Invoke(transform.AsPose(), point, type);
        }        
        
        public void Stop()
        {
            markers.Values.ForEach(DeleteMarkerObject);
            markers.Clear();
            markersToDelete.Clear();

            if (!(control is null))
            {
                control.Stop();
                ResourcePool.Dispose(Resource.Displays.InteractiveControl, control.gameObject);
            }

            MouseEvent = null;
            Moved = null;
        }

        static void DeleteMarkerObject(MarkerObject marker)
        {
            marker.Stop();
            Destroy(marker.gameObject);
        }

        static MarkerObject CreateMarkerObject()
        {
            GameObject gameObject = new GameObject("MarkerObject");
            return gameObject.AddComponent<MarkerObject>();
        }
    }
}