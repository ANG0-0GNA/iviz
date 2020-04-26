﻿using System.Collections.Generic;
using UnityEngine;

namespace Iviz.App
{
    public abstract class DataPanelContents : MonoBehaviour
    {
        public static DataPanelContents AddTo(GameObject o, Resource.Module resource)
        {
            switch (resource)
            {
                case Resource.Module.TF: return o.AddComponent<TFPanelContents>();
                case Resource.Module.PointCloud: return o.AddComponent<PointCloudPanelContents>();
                case Resource.Module.Grid: return o.AddComponent<GridPanelContents>();
                case Resource.Module.Image: return o.AddComponent<ImagePanelContents>();
                case Resource.Module.Robot: return o.AddComponent<RobotPanelContents>();
                case Resource.Module.Marker: return o.AddComponent<MarkerPanelContents>();
                case Resource.Module.InteractiveMarker: return o.AddComponent<InteractiveMarkerPanelContents>();
                case Resource.Module.JointState: return o.AddComponent<JointStatePanelContents>();
                case Resource.Module.DepthImageProjector: return o.AddComponent<DepthImageProjectorPanelContents>();
                default: return o.AddComponent<DefaultPanelContents>();
            }
        }

        protected Widget[] Widgets { get; set; }

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public virtual void ClearSubscribers()
        {
            Widgets?.ForEach(w => w.ClearSubscribers());
        }
    }
    public abstract class ListenerPanelContents : DataPanelContents
    {
        public SectionTitleWidget Stats { get; protected set; }
    }
}