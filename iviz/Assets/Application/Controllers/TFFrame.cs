﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Iviz.Displays;
using Iviz.Resources;
using System;
using System.Collections.ObjectModel;
using Application.Displays;
using Iviz.App;

namespace Iviz.Controllers
{
    public sealed class TFFrame : ClickableNode, IRecyclable
    {
        const int MaxPoseMagnitude = 1000;
        const int Layer = 9;

        readonly Timeline timeline = new Timeline();
        TrailResource trail;
        AnchorLine anchor;

        [SerializeField] string id;

        public string Id
        {
            get => id;
            set
            {
                id = value;
                labelObjectText.text = id;
                trail.Name = "[Trail:" + id + "]";
                anchor.Name = "[Anchor:" + id + "]";
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

        GameObject labelObject;
        TextMesh labelObjectText;
        LineConnector parentConnector;
        BoxCollider boxCollider;
        AxisFrameResource axis;

        readonly HashSet<DisplayNode> listeners = new HashSet<DisplayNode>();
        readonly Dictionary<string, TFFrame> children = new Dictionary<string, TFFrame>();

        public ReadOnlyDictionary<string, TFFrame> Children =>
            new ReadOnlyDictionary<string, TFFrame>(children);

        public override Bounds Bounds => new Bounds(boxCollider.center, boxCollider.size);
        public override Bounds WorldBounds => boxCollider.bounds;
        public override Vector3 BoundsScale => Vector3.one;

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

        bool forceVisible;

        public bool ForceVisible
        {
            get => forceVisible;
            set
            {
                forceVisible = value;
                Visible = Visible;
                AnchorVisible = AnchorVisible;
                TrailVisible = TrailVisible;
            }
        }

        public bool ForceInvisible { get; set; }

        bool visible;

        public bool Visible
        {
            get => visible;
            set
            {
                visible = value;
                axis.Visible = (value || ForceVisible) && !ForceInvisible;
                TrailVisible = TrailVisible;
                AnchorVisible = AnchorVisible;
            }
        }

        bool anchorVisible;

        bool AnchorVisible
        {
            get => anchorVisible;
            set
            {
                anchorVisible = value;
                anchor.Visible = value && (Visible || ForceVisible);
            }
        }

        bool labelVisible;

        public bool LabelVisible
        {
            get => labelVisible;
            set
            {
                labelVisible = value;
                labelObject.SetActive((value || Selected) && (ForceVisible || Visible));
            }
        }

        public float LabelSize
        {
            get => labelObject.transform.localScale.x;
            set => labelObject.transform.localScale = value * Vector3.one;
        }

        public bool ConnectorVisible
        {
            get => parentConnector.gameObject.activeSelf;
            set => parentConnector.gameObject.SetActive(value);
        }

        public float AxisLength
        {
            get => axis.AxisLength;
            set
            {
                axis.AxisLength = value;
                parentConnector.LineWidth = AxisLength / 20;
            }
        }

        bool trailVisible;

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

        bool acceptsParents = true;

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

        public bool SetParent(TFFrame newParent)
        {
            if (!AcceptsParents &&
                newParent != TFListener.RootFrame &&
                newParent != TFListener.UnityFrame &&
                !(newParent is null))
            {
                return false;
            }

            //Debug.Log("a child " + Id + " parent " + newParent?.Id);
            if (newParent == Parent)
            {
                return true;
            }

            //Debug.Log("b child " + Id + " parent " + newParent?.Id);
            if (newParent == this)
            {
                //Logger.Error($"TFFrame: Cannot set '{newParent.Id}' as a parent to itself!");
                return false;
            }

            //Debug.Log("c child " + Id + " parent " + newParent?.Id);
            if (!(newParent is null) && newParent.IsChildOf(this))
            {
                //Logger.Error($"TFFrame: Cannot set '{newParent.Id}' as parent to '{Id}' because it causes a cycle!");
                newParent.CheckIfDead();
                return false;
            }

            /*
            if (!timeline.Empty && time < timeline.LastTime)
            {
                return true; //??
            }
            */
            //Debug.Log("d child " + Id + " parent " + newParent?.Id);
            Parent?.RemoveChild(this);
            //Debug.Log("3 child " + Id + " parent " + newParent?.Id);
            base.Parent = newParent;
            //Debug.Log("2 child " + Id + " parent " + newParent?.Id);
            Parent?.AddChild(this);
            //Debug.Log("1 child " + Id + " parent " + newParent?.Id);

            parentConnector.B = transform.parent ?? TFListener.RootFrame.transform;

            return true;
        }

        bool IsChildOf(TFFrame frame)
        {
            if (Parent is null)
            {
                return false;
            }

            return Parent == frame || Parent.IsChildOf(frame);
        }


        Pose pose;

        public Pose WorldPose => TFListener.RelativePose(transform.AsPose());

        public Pose AbsolutePose => transform.AsPose();

        [SerializeField] Vector3 debugRosPosition;

        public void SetPose(in TimeSpan time, in Pose newPose)
        {
            pose = newPose;
            debugRosPosition = pose.position.Unity2Ros();

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

            foreach (TFFrame child in children.Values)
            {
                child.LogPose(time);
            }

            //Debug.Log(timeline.Count + " " + (timeline.LastTime - timeline.FirstTime).Milliseconds);
        }

        public Pose GetPose(in TimeSpan time)
        {
            return timeline.Count == 0 ? pose : timeline.Get(time);
        }

        bool HasNoListeners => !listeners.Any();

        bool IsChildless => !children.Any();

        public override string Name => Id;

        public override Pose BoundsPose => transform.AsPose();

        void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();

            labelObject = ResourcePool.GetOrCreate(Resource.Displays.Text, transform);
            labelObject.gameObject.SetActive(false);
            labelObjectText = labelObject.GetComponent<TextMesh>();
            labelObject.name = "[Label]";

            parentConnector = ResourcePool.GetOrCreate(Resource.Displays.LineConnector, transform)
                .GetComponent<LineConnector>();
            parentConnector.A = transform;
            parentConnector.B =
                transform.parent != null
                    ? transform.parent
                    : TFListener.RootFrame?.transform; // TFListener.BaseFrame may not exist yet
            parentConnector.name = "[Connector]";

            axis = ResourcePool.GetOrCreate<AxisFrameResource>(Resource.Displays.AxisFrame, transform);
            axis.Layer = Layer;
            axis.name = "[Axis]";

            AxisLength = 0.125f;

            parentConnector.gameObject.SetActive(false);

            UsesBoundaryBox = false;

            trail = ResourcePool.GetOrCreate<TrailResource>(Resource.Displays.Trail);
            trail.TimeWindowInMs = 5000;
            trail.Color = Color.yellow;
            TrailVisible = false;

            anchor = ResourcePool.GetOrCreate<AnchorLine>(Resource.Displays.AnchorLine,
                TFListener.UnityFrame?.transform);
            anchor.Visible = false;
        }

        public void SplitForRecycle()
        {
            ResourcePool.Dispose(Resource.Displays.AxisFrame, axis.gameObject);
            ResourcePool.Dispose(Resource.Displays.Text, labelObject);
            ResourcePool.Dispose(Resource.Displays.LineConnector, parentConnector.gameObject);
            ResourcePool.Dispose(Resource.Displays.Trail, trail.gameObject);
            axis = null;
            labelObject = null;
            parentConnector = null;
            trail = null;
        }

        public override void Stop()
        {
            base.Stop();
            Id = "";
            trail.Name = "[Trail:In Trash]";
            timeline.Clear();
            axis.Stop();
            trail.Stop();
            TrailVisible = false;
            anchor.Visible = false;
            anchor.Name = "[Anchor:In Trash]";
        }

        public void UpdateAnchor(IAnchorProvider anchorProvider, bool forceRebuild = false)
        {
            //Debug.Log("lel");
            if (anchorProvider is null)
            {
                AnchorVisible = forceRebuild;
                return;
            }

            //Debug.Log("was here");
            AnchorVisible = true;
            anchor.AnchorProvider = anchorProvider;
            anchor.SetPosition(transform.position, forceRebuild);
        }

        protected override void OnDoubleClick()
        {
            TFListener.GuiManager.Select(this);
            ModuleListPanel.Instance.ShowFrame(this);
        }
    }
}