﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iviz.App;
using Iviz.Core;
using Iviz.Displays;
using Iviz.Msgs.VisualizationMsgs;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace Iviz.Controllers
{
    public sealed class InteractiveMarkerObject : FrameNode
    {
        const string WarnStr = "<b>Warning:</b> ";
        const string ErrorStr = "<color=red>Error:</color> ";

        readonly Dictionary<string, InteractiveMarkerControlObject> controls =
            new Dictionary<string, InteractiveMarkerControlObject>();

        readonly HashSet<string> controlsToDelete = new HashSet<string>();
        readonly StringBuilder description = new StringBuilder();
        int numWarnings;
        int numErrors;

        MenuEntryList menuEntries;

        InteractiveMarkerListener listener;
        TextMarkerResource text;
        GameObject controlNode;

        string rosId;

        Pose? cachedPose;
        bool poseUpdateEnabled = true;

        internal bool PoseUpdateEnabled
        {
            get => poseUpdateEnabled;
            set
            {
                poseUpdateEnabled = value;
                if (poseUpdateEnabled && cachedPose != null)
                {
                    controlNode.transform.SetLocalPose(cachedPose.Value);
                    cachedPose = null;
                }
            }
        }

        public bool DescriptionVisible
        {
            get => text.Visible;
            set => text.Visible = value;
        }

        Pose LocalPose
        {
            get => transform.AsLocalPose();
            set
            {
                if (poseUpdateEnabled)
                {
                    controlNode.transform.SetLocalPose(value);
                }
                else
                {
                    cachedPose = value;
                }
            }
        }

        bool visible;

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;

                foreach (var control in controls.Values)
                {
                    control.Visible = value;
                }
            }
        }


        void Awake()
        {
            controlNode = new GameObject("[ControlNode]");
            controlNode.transform.parent = transform;

            text = ResourcePool.GetOrCreateDisplay<TextMarkerResource>(controlNode.transform);
            text.BillboardEnabled = true;
            text.BillboardOffset = Vector3.up * 0.1f;
            text.ElementSize = 0.1f;
            text.Visible = false;
            text.VisibleOnTop = true;
        }

        internal void Initialize(InteractiveMarkerListener newListener, string realId)
        {
            listener = newListener;
            rosId = realId;
        }

        public void Set([NotNull] InteractiveMarker msg)
        {
            name = msg.Name;
            numWarnings = 0;
            numErrors = 0;

            description.Clear();
            description.Append("<color=blue><b>**** InteractiveMarker '").Append(msg.Name).Append("'</b></color>")
                .AppendLine();
            string msgDescription = msg.Description.Length != 0
                ? msg.Description.Replace("\t", "\\t").Replace("\n", "\\n")
                : "[]";
            description.Append("Description: ").Append(msgDescription).AppendLine();

            text.Text = msg.Description.Length != 0 ? msg.Description : msg.Name;

            if (Mathf.Approximately(msg.Scale, 0))
            {
                transform.localScale = Vector3.one;
                description.Append("Scale: 0 <i>(1)</i>").AppendLine();
            }
            else
            {
                transform.localScale = msg.Scale * Vector3.one;
                description.Append("Scale: ").Append(msg.Scale).AppendLine();
            }

            LocalPose = msg.Pose.Ros2Unity();

            description.Append("Attached to: <i>").Append(msg.Header.FrameId).Append("</i>").AppendLine();
            AttachTo(msg.Header.FrameId);

            controlsToDelete.Clear();
            foreach (string controlId in controls.Keys)
            {
                controlsToDelete.Add(controlId);
            }

            int numUnnamed = 0;
            foreach (InteractiveMarkerControl controlMsg in msg.Controls)
            {
                string controlId = controlMsg.Name.Length != 0 ? controlMsg.Name : $"[Unnamed-{(numUnnamed++)}]";

                if (controls.TryGetValue(controlId, out var existingControl))
                {
                    existingControl.Set(controlMsg);
                    controlsToDelete.Remove(controlId);
                    continue;
                }

                InteractiveMarkerControlObject newControl = CreateControlObject();
                newControl.Initialize(this, controlMsg.Name);
                newControl.transform.SetParentLocal(controlNode.transform);
                newControl.Visible = Visible;
                controls[controlId] = newControl;

                newControl.Set(controlMsg);
                controlsToDelete.Remove(controlId);
            }

            if (numUnnamed > 1)
            {
                description.Append(WarnStr).Append(numUnnamed).Append(" controls have empty ids").AppendLine();
                numWarnings++;
            }

            foreach (string controlId in controlsToDelete)
            {
                InteractiveMarkerControlObject control = controls[controlId];
                DeleteControlObject(control);
                controls.Remove(controlId);
            }


            // update the dimensions of the controls
            IEnumerable<(Bounds? bounds, Transform transform)> innerBounds = controls.Values
                .Select(marker => (marker.Bounds, marker.transform));

            Bounds? totalBounds =
                UnityUtils.CombineBounds(
                    innerBounds.Select(tuple => UnityUtils.TransformBound(tuple.bounds, tuple.transform))
                );

            foreach (var control in controls.Values)
            {
                control.UpdateControlBounds(totalBounds);
            }

            if (msg.MenuEntries.Length != 0)
            {
                menuEntries = new MenuEntryList(msg.MenuEntries, description, out int newNumErrors);
                numErrors += newNumErrors;

                description.Append("+ ").Append(menuEntries.Count).Append(" menu entries").AppendLine();

                IControlMarker controlMarker =
                    controls.Values.FirstOrDefault(control => control.Control != null)?.Control;
                if (controlMarker != null)
                {
                    controlMarker.EnableMenu = true;
                }
            }
        }

        internal void ShowMenu(Vector3 unityPositionHint)
        {
            ModuleListPanel.Instance.ShowMenu(menuEntries, OnMenuClick, unityPositionHint);
        }

        public void Set(in Iviz.Msgs.GeometryMsgs.Pose rosPose)
        {
            //Debug.Log("Received: " + rosPose.Ros2Unity() + "  ---   Actual:" + controlNode.transform.AsLocalPose());
            LocalPose = rosPose.Ros2Unity();
        }

        internal void OnMouseEvent(string rosControlId, in Vector3? point, MouseEventType type)
        {
            if (this == null)
            {
                return; // destroyed while interacting
            }

            listener?.OnInteractiveControlObjectMouseEvent(rosId, rosControlId, controlNode.transform.AsLocalPose(),
                point, type);
        }

        internal void OnMoved(string rosControlId)
        {
            if (this == null)
            {
                return; // destroyed while interacting
            }

            listener?.OnInteractiveControlObjectMoved(rosId, rosControlId, controlNode.transform.AsLocalPose());
        }

        void OnMenuClick(uint entryId)
        {
            if (this == null)
            {
                return; // destroyed while interacting
            }

            listener?.OnInteractiveControlObjectMenuSelect(rosId, "", entryId, controlNode.transform.AsLocalPose());
        }

        public override void Stop()
        {
            base.Stop();
            foreach (var controlObject in controls.Values)
            {
                DeleteControlObject(controlObject);
            }

            controls.Clear();
            controlsToDelete.Clear();

            ResourcePool.DisposeDisplay(text);

            Destroy(controlNode.gameObject);
        }

        public void GenerateLog([NotNull] StringBuilder baseDescription)
        {
            baseDescription.Append(description);

            foreach (var control in controls.Values)
            {
                control.GenerateLog(baseDescription);
            }
        }

        public void GetErrorCount(out int totalErrors, out int totalWarnings)
        {
            totalErrors = numErrors;
            totalWarnings = numWarnings;

            foreach (var control in controls.Values)
            {
                control.GetErrorCount(out int newNumErrors, out int newNumWarnings);
                totalErrors += newNumErrors;
                totalWarnings += newNumWarnings;
            }
        }

        static void DeleteControlObject([NotNull] InteractiveMarkerControlObject control)
        {
            control.Stop();
            Destroy(control.gameObject);
        }

        static InteractiveMarkerControlObject CreateControlObject()
        {
            GameObject gameObject = new GameObject("InteractiveMarkerControlObject");
            return gameObject.AddComponent<InteractiveMarkerControlObject>();
        }
    }
}