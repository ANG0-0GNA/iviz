﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iviz.Core;
using Iviz.Displays;
using Iviz.Msgs.VisualizationMsgs;
using Iviz.Resources;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Iviz.Controllers
{
    public sealed class InteractiveMarkerControlObject : MonoBehaviour
    {
        const string WarnStr = "<b>Warning:</b> ";
        const string ErrorStr = "<color=red>Error:</color> ";

        readonly StringBuilder description = new StringBuilder();
        readonly Dictionary<string, MarkerObject> markers = new Dictionary<string, MarkerObject>();

        [CanBeNull] IControlMarker control;
        GameObject markerNode;
        //bool markerIsInteractive;

        string rosId;

        InteractiveMarkerObject interactiveMarkerObject;

        public Bounds? Bounds { get; private set; }

        void Awake()
        {
            markerNode = new GameObject("[MarkerNode]");
            markerNode.transform.SetParentLocal(transform);
            markerNode.AddComponent<Billboard>().enabled = false;
        }

        internal void Initialize(InteractiveMarkerObject newIMarkerObject, string realId)
        {
            interactiveMarkerObject = newIMarkerObject;
            rosId = realId;
        }

        public void Set([NotNull] InteractiveMarkerControl msg)
        {
            name = $"[ControlObject '{msg.Name}']";

            description.Clear();
            description.Append("<color=navy><b>** Control '").Append(msg.Name).Append("'</b></color>").AppendLine();

            string msgDescription = msg.Description.Length != 0
                ? msg.Description.Replace("\t", "\\t").Replace("\n", "\\n")
                : "[]";
            description.Append("Description: ").Append(msgDescription).AppendLine();

            transform.localRotation = msg.Orientation.Ros2Unity();

            InteractionMode interactionMode = (InteractionMode) msg.InteractionMode;
            OrientationMode orientationMode = (OrientationMode) msg.OrientationMode;
            description.Append("InteractionMode: ").Append(EnumToString(interactionMode)).AppendLine();
            description.Append("OrientationMode: ").Append(EnumToString(orientationMode)).AppendLine();

            UpdateMarkers(msg.Markers);
            Bounds = RecalculateBounds();

            UpdateInteractionMode(interactionMode, orientationMode, msg.IndependentMarkerOrientation);

            if (markers.Count == 0)
            {
                description.Append("Markers: Empty").AppendLine();
            }
            else
            {
                description.Append("Markers: ").Append(markers.Count).AppendLine();
            }
        }

        [NotNull]
        IControlMarker EnsureControlDisplayExists()
        {
            if (control != null)
            {
                return control;
            }

            control = ResourcePool.GetOrCreate(Resource.Displays.InteractiveControl, transform)
                          .GetComponent<IControlMarker>()
                      ?? throw new InvalidOperationException("Control marker has no control component!");

            control.TargetTransform = transform.parent;

            control.Moved += (in Pose _) =>
            {
                if (interactiveMarkerObject != null)
                {
                    interactiveMarkerObject.OnMoved(rosId);
                }
            };

            // disable external updates while dragging
            control.PointerDown += () =>
            {
                interactiveMarkerObject.PoseUpdateEnabled = false;
                interactiveMarkerObject.OnMouseEvent(rosId, null, MouseEventType.Down);
            };
            control.PointerUp += () =>
            {
                interactiveMarkerObject.PoseUpdateEnabled = true;
                interactiveMarkerObject.OnMouseEvent(rosId, null, MouseEventType.Up);

                if (control.InteractionMode == InteractionModeType.ClickOnly)
                {
                    interactiveMarkerObject.OnMouseEvent(rosId, null, MouseEventType.Click);
                }
            };

            return control;
        }

        void DisposeControlDisplay()
        {
            if (control == null)
            {
                return;
            }

            control.Suspend();
            ResourcePool.Dispose(Resource.Displays.InteractiveControl, ((MonoBehaviour) control).gameObject);
            control = null;
        }

        void UpdateInteractionMode(InteractionMode interactionMode, OrientationMode orientationMode,
            bool independentMarkerOrientation)
        {
            //bool clickable = interactionMode == InteractionMode.Button;
            //markers.Values.ForEach(marker => marker.Clickable = clickable);

            if (interactionMode < 0 || interactionMode > InteractionMode.MoveRotate3D)
            {
                description.Append(ErrorStr).Append("Unknown interaction mode ").Append((int) interactionMode)
                    .AppendLine();
                //markerIsInteractive = false;
                DisposeControlDisplay();
            }
            else if (interactionMode == InteractionMode.None)
            {
                //markerIsInteractive = false;
                DisposeControlDisplay();
            }
            else
            {
                //markerIsInteractive = true;
                IControlMarker mControl = EnsureControlDisplayExists();
                switch (interactionMode)
                {
                    case InteractionMode.Menu:
                    case InteractionMode.Button:
                        mControl.InteractionMode = InteractionModeType.ClickOnly;
                        break;
                    case InteractionMode.MoveAxis:
                        mControl.InteractionMode = InteractionModeType.MoveAxisX;
                        break;
                    case InteractionMode.MovePlane:
                        mControl.InteractionMode = InteractionModeType.MovePlaneYZ;
                        break;
                    case InteractionMode.RotateAxis:
                        mControl.InteractionMode = InteractionModeType.RotateAxisX;
                        break;
                    case InteractionMode.MoveRotate:
                        mControl.InteractionMode = InteractionModeType.MovePlaneYZ_RotateAxisX;
                        break;
                    case InteractionMode.Move3D:
                        mControl.InteractionMode = InteractionModeType.Move3D;
                        break;
                    case InteractionMode.Rotate3D:
                        mControl.InteractionMode = InteractionModeType.Rotate3D;
                        break;
                    case InteractionMode.MoveRotate3D:
                        mControl.InteractionMode = InteractionModeType.MoveRotate3D;
                        break;
                }
            }
            
            if (orientationMode < 0 || orientationMode > OrientationMode.ViewFacing)
            {
                description.Append(ErrorStr).Append("Unknown orientation mode ").Append((int) orientationMode)
                    .AppendLine();
                markerNode.GetComponent<Billboard>().enabled = false;
                markerNode.transform.localRotation = Quaternion.identity;

                if (control != null)
                {
                    control.PointsToCamera = false;
                    control.KeepAbsoluteRotation = false;
                }

                return;
            }

            switch (orientationMode)
            {
                case OrientationMode.ViewFacing:
                    markerNode.GetComponent<Billboard>().enabled = true;
                    break;
                case OrientationMode.Inherit:
                case OrientationMode.Fixed:
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
                case OrientationMode.ViewFacing:
                    control.KeepAbsoluteRotation = false;
                    control.PointsToCamera = true;
                    control.HandlesPointToCamera = !independentMarkerOrientation;
                    break;
                case OrientationMode.Inherit:
                    control.PointsToCamera = false;
                    control.KeepAbsoluteRotation = false;
                    break;
                case OrientationMode.Fixed:
                    control.PointsToCamera = false;
                    control.KeepAbsoluteRotation = true;
                    break;
            }
        }

        void UpdateMarkers([NotNull] Marker[] msg)
        {
            int numUnnamed = 0;

            foreach (Marker marker in msg)
            {
                string markerId = marker.Ns.Length == 0 && marker.Id == 0
                    ? $"[Unnamed-{(numUnnamed++)}]"
                    : MarkerListener.IdFromMessage(marker);
                switch (marker.Action)
                {
                    case Marker.ADD:
                        MarkerObject markerObject;
                        if (markers.TryGetValue(markerId, out MarkerObject existingMarker))
                        {
                            markerObject = existingMarker;
                        }
                        else
                        {
                            markerObject = CreateMarkerObject();
                            //markerObject.MouseEvent += OnMarkerClicked;
                            markers[markerId] = markerObject;
                        }

                        markerObject.Set(marker);
                        if (marker.Header.FrameId.Length == 0)
                        {
                            markerObject.transform.SetParentLocal(markerNode.transform);
                        }

                        break;
                    case Marker.DELETE:
                        if (markers.TryGetValue(markerId, out MarkerObject markerToDelete))
                        {
                            DeleteMarkerObject(markerToDelete);
                            markers.Remove(markerId);
                        }

                        break;
                }

                if (numUnnamed > 1)
                {
                    description.Append(WarnStr).Append(numUnnamed).Append(" imarkers have empty ids").AppendLine();
                }
            }
        }

        public void Stop()
        {
            markers.Values.ForEach(DeleteMarkerObject);
            markers.Clear();

            if (control != null)
            {
                control.Suspend();
                ResourcePool.Dispose(Resource.Displays.InteractiveControl, ((MonoBehaviour) control).gameObject);
            }
        }

        public void GenerateLog([NotNull] StringBuilder baseDescription)
        {
            baseDescription.Append(description);

            foreach (var marker in markers.Values)
            {
                marker.GenerateLog(baseDescription);
            }
        }

        static void DeleteMarkerObject([NotNull] MarkerObject marker)
        {
            marker.Stop();
            Destroy(marker.gameObject);
        }

        static MarkerObject CreateMarkerObject()
        {
            GameObject gameObject = new GameObject("MarkerObject");
            return gameObject.AddComponent<MarkerObject>();
        }

        [NotNull]
        static string EnumToString(InteractionMode mode)
        {
            switch (mode)
            {
                case InteractionMode.None:
                    return "None";
                case InteractionMode.Menu:
                    return "Menu";
                case InteractionMode.Button:
                    return "Button";
                case InteractionMode.MoveAxis:
                    return "MoveAxis";
                case InteractionMode.MovePlane:
                    return "MovePlane";
                case InteractionMode.RotateAxis:
                    return "RotateAxis";
                case InteractionMode.MoveRotate:
                    return "MoveRotate";
                case InteractionMode.Move3D:
                    return "Move3D";
                case InteractionMode.Rotate3D:
                    return "Rotate3D";
                case InteractionMode.MoveRotate3D:
                    return "MoveRotate3D";
                default:
                    return $"Unknown";
            }
        }

        [NotNull]
        static string EnumToString(OrientationMode mode)
        {
            switch (mode)
            {
                case OrientationMode.Inherit:
                    return "Inherit";
                case OrientationMode.Fixed:
                    return "Fixed";
                case OrientationMode.ViewFacing:
                    return "ViewFacing";
                default:
                    return $"Unknown";
            }
        }

        Bounds? RecalculateBounds()
        {
            var innerBounds = markers.Values.Select(marker => marker.Bounds);
            return UnityUtils.CombineBounds(innerBounds);
        }

        public void UpdateControlBounds(Bounds? bounds)
        {
            if (control != null)
            {
                control.Bounds = bounds;
            }
        }

        enum InteractionMode
        {
            None = 0,
            Menu = 1,
            Button = 2,
            MoveAxis = 3,
            MovePlane = 4,
            RotateAxis = 5,
            MoveRotate = 6,
            Move3D = 7,
            Rotate3D = 8,
            MoveRotate3D = 9
        }

        enum OrientationMode
        {
            Inherit = 0,
            Fixed = 1,
            ViewFacing = 2
        }
    }
}