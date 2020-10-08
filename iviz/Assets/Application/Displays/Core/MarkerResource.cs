﻿using UnityEngine;

namespace Iviz.Displays
{
    public abstract class MarkerResource : MonoBehaviour, IDisplay
    {
        protected BoxCollider BoxCollider { get; set; }

        public Bounds Bounds => BoxCollider == null ? new Bounds() : new Bounds(BoxCollider.center, BoxCollider.size);
        public Bounds WorldBounds => BoxCollider == null ? new Bounds() : BoxCollider.bounds;

        public virtual string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }

        bool colliderEnabled = true;
        public bool ColliderEnabled
        {
            get => colliderEnabled;
            set
            {
                colliderEnabled = value;
                if (BoxCollider != null)
                {
                    BoxCollider.enabled = value;
                }
            }
        }

        public Transform Parent
        {
            get => transform.parent;
            set => transform.parent = value;
        }

        public virtual bool Visible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public virtual Vector3 WorldScale => transform.lossyScale;

        public virtual Pose WorldPose => transform.AsPose();

        public virtual int Layer
        {
            get => gameObject.layer;
            set => gameObject.layer = value;
        }

        protected virtual void Awake()
        {
            if (BoxCollider == null)
            {
                BoxCollider = GetComponent<BoxCollider>();
            }

            ColliderEnabled = ColliderEnabled;
        }

        public virtual void Suspend()
        {
            Layer = 0;
        }
    }
}