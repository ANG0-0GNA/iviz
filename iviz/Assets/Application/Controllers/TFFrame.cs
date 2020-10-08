﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Iviz.App;
using Iviz.Displays;
using Iviz.Resources;
using UnityEngine;

namespace Iviz.Controllers
{
    public sealed class TFFrame : ClickableNode, IRecyclable
    {
        const int MaxPoseMagnitude = 1000;
        const int Layer = 9;

        [SerializeField] string id;
        [SerializeField] Vector3 rosPosition;

        readonly Dictionary<string, TFFrame> children = new Dictionary<string, TFFrame>();
        readonly HashSet<DisplayNode> listeners = new HashSet<DisplayNode>();
        readonly Timeline timeline = new Timeline();

        Pose pose;

        bool acceptsParents = true;
        float alpha = 1;
        float labelSize = 1.0f;
        bool labelVisible;
        bool trailVisible;

        bool visible;
        bool forceVisible;

        AxisFrameResource axis;
        GameObject labelObject;
        Billboard labelObjectBillboard;
        TextMarkerResource labelObjectText;
        LineConnector parentConnector;
        TrailResource trail;

        public ReadOnlyDictionary<string, TFFrame> Children =>
            new ReadOnlyDictionary<string, TFFrame>(children);

        public string Id
        {
            get => id;
            set
            {
                id = value;
                labelObjectText.Text = id;
                trail.Name = "[Trail:" + id + "]";
            }
        }

        public override bool Selected
        {
            get => base.Selected;
            set
            {
                base.Selected = value;
                labelObject.SetActive(value || LabelVisible);
            }
        }

        public float Alpha
        {
            get => alpha;
            set
            {
                alpha = value;
                axis.ColorX = new Color(axis.ColorX.r, axis.ColorX.g, axis.ColorX.b, alpha);
                axis.ColorY = new Color(axis.ColorY.r, axis.ColorY.g, axis.ColorY.b, alpha);
                axis.ColorZ = new Color(axis.ColorZ.r, axis.ColorZ.g, axis.ColorZ.b, alpha);
            }
        }

        public override Bounds Bounds => axis.Bounds;
        public override Bounds WorldBounds => axis.WorldBounds;
        public override Vector3 BoundsScale => axis.WorldScale;

        public bool ColliderEnabled
        {
            get => axis.ColliderEnabled;
            set => axis.ColliderEnabled = value;
        }

        bool ForceVisible
        {
            get => forceVisible;
            set
            {
                forceVisible = value;
                Visible = Visible;
                TrailVisible = TrailVisible;
            }
        }

        public bool ForceInvisible { get; set; }

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                axis.Visible = (value || ForceVisible) && !ForceInvisible;
                TrailVisible = TrailVisible;
            }
        }

        public bool LabelVisible
        {
            get => labelVisible;
            set
            {
                labelVisible = value;
                labelObject.SetActive(!ForceInvisible && (value || Selected) && (ForceVisible || Visible));
            }
        }

        public float LabelSize
        {
            get => labelSize;
            set
            {
                labelSize = value;
                labelObject.transform.localScale = 0.5f * value * FrameSize * Vector3.one;
            }
        }

        public bool ConnectorVisible
        {
            get => parentConnector.gameObject.activeSelf;
            set => parentConnector.gameObject.SetActive(value);
        }

        public float FrameSize
        {
            get => axis.AxisLength;
            set
            {
                axis.AxisLength = value;
                parentConnector.LineWidth = FrameSize / 20;
                labelObjectBillboard.offset = 1.5f * FrameSize * Vector3.up;
                LabelSize = LabelSize;
            }
        }

        public bool TrailVisible
        {
            get => trailVisible;
            set
            {
                trailVisible = value;
                trail.Visible = value && (Visible || ForceVisible);
                if (value)
                {
                    trail.DataSource = () => transform.position;
                }
                else
                {
                    trail.DataSource = null;
                }
            }
        }

        public bool AcceptsParents
        {
            get => acceptsParents;
            set
            {
                acceptsParents = value;
                if (!acceptsParents)
                {
                    Parent = TFListener.RootFrame;
                }
            }
        }

        public override TFFrame Parent
        {
            get => base.Parent;
            set
            {
                if (!SetParent(value))
                {
                    Logger.Error($"TFFrame: Failed to set '{value.Id}' as a parent to {Id}");
                }
            }
        }

        public Pose WorldPose => TFListener.RelativePose(transform.AsPose());

        public Pose AbsolutePose => transform.AsPose();

        bool HasNoListeners => !listeners.Any();

        bool IsChildless => !children.Any();

        public override string Name => Id;

        public override Pose BoundsPose => transform.AsPose();

        void Awake()
        {
            labelObjectText = ResourcePool.GetOrCreateDisplay<TextMarkerResource>(transform);
            labelObject = labelObjectText.gameObject;
            labelObject.SetActive(false);
            labelObject.name = "[Label]";
            labelObject.transform.localScale = 0.5f * Vector3.one;
            labelObjectBillboard = labelObject.GetComponent<Billboard>();

            parentConnector = ResourcePool.GetOrCreate(Resource.Displays.LineConnector, transform)
                .GetComponent<LineConnector>();
            parentConnector.A = transform;


            // TFListener.BaseFrame may not exist yet
            var parent = transform.parent;
            parentConnector.B = parent != null ? parent : TFListener.RootFrame?.transform;

            parentConnector.name = "[Connector]";

            axis = ResourcePool.GetOrCreateDisplay<AxisFrameResource>(transform);

            if (Settings.IsHololens)
            {
                axis.ColliderEnabled = false;
            }

            axis.ColliderEnabled = true;
            axis.Layer = Layer;

            axis.name = "[Axis]";

            FrameSize = 0.125f;

            parentConnector.gameObject.SetActive(false);

            UsesBoundaryBox = false;

            trail = ResourcePool.GetOrCreateDisplay<TrailResource>(transform);
            trail.TimeWindowInMs = 5000;
            trail.Color = Color.yellow;
            TrailVisible = false;
        }

        public void SplitForRecycle()
        {
            ResourcePool.DisposeDisplay(axis);
            ResourcePool.DisposeDisplay(labelObjectText);
            ResourcePool.Dispose(Resource.Displays.LineConnector, parentConnector.gameObject);
            ResourcePool.DisposeDisplay(trail);

            axis = null;
            labelObject = null;
            parentConnector = null;
            trail = null;
        }

        public void AddListener(DisplayNode display)
        {
            listeners.Add(display);
        }

        public void RemoveListener(DisplayNode display)
        {
            if (HasNoListeners)
            {
                return;
            }

            listeners.Remove(display);
            CheckIfDead();
        }

        void AddChild(TFFrame frame)
        {
            //Debug.Log(Id + " has new child " + frame);
            children.Add(frame.Id, frame);
        }

        void RemoveChild(TFFrame frame)
        {
            if (IsChildless)
            {
                return;
            }

            //Debug.Log(Id + " loses child " + frame);
            children.Remove(frame.Id);
        }

        void CheckIfDead()
        {
            if (HasNoListeners && IsChildless)
            {
                TFListener.Instance.MarkAsDead(this);
            }
        }

        public bool SetParent(TFFrame newParent)
        {
            if (!AcceptsParents &&
                newParent != TFListener.RootFrame &&
                newParent != TFListener.UnityFrame &&
                newParent != null)
            {
                return false;
            }

            if (newParent == Parent)
            {
                return true;
            }

            if (newParent == this)
            {
                return false;
            }

            if (newParent != null && newParent.IsChildOf(this))
            {
                newParent.CheckIfDead();
                return false;
            }

            if (Parent != null)
            {
                Parent.RemoveChild(this);
            }

            base.Parent = newParent;
            if (Parent != null)
            {
                Parent.AddChild(this);
            }

            var parent = transform.parent;
            parentConnector.B = parent != null ? parent : TFListener.RootFrame.transform;

            return true;
        }

        bool IsChildOf(TFFrame frame)
        {
            if (Parent == null)
            {
                return false;
            }

            return Parent == frame || Parent.IsChildOf(frame);
        }

        public void SetPose(in TimeSpan time, in Pose newPose)
        {
            pose = newPose;
            rosPosition = pose.position.Unity2Ros();

            if (newPose.position.sqrMagnitude > MaxPoseMagnitude * MaxPoseMagnitude)
            {
                return; // lel
            }

            transform.SetLocalPose(newPose);
            LogPose(time);
        }

        void LogPose(in TimeSpan time)
        {
            if (listeners.Count != 0)
            {
                timeline.Add(time, transform.AsPose());
            }

            foreach (var child in children.Values)
            {
                child.LogPose(time);
            }
        }

        public Pose LookupPose(in TimeSpan time)
        {
            return timeline.Count == 0 ? pose : timeline.Lookup(time);
        }

        public override void Stop()
        {
            base.Stop();
            Id = "";
            trail.Name = "[Trail:In Trash]";
            timeline.Clear();
            axis.Suspend();
            trail.Suspend();
            TrailVisible = false;
        }

        protected override void OnDoubleClick()
        {
            TFListener.GuiCamera.Select(this);
            ModuleListPanel.Instance.ShowFrame(this);
        }
    }
}