﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Iviz.App.Displays;
using Iviz.Displays;
using Iviz.Msgs.NavMsgs;
using Iviz.Msgs.SensorMsgs;
using Iviz.Resources;
using Iviz.RoslibSharp;
using UnityEngine;

namespace Iviz.App.Listeners
{
    [DataContract]
    public class OccupancyGridConfiguration : JsonToString, IConfiguration
    {
        [DataMember] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember] public Resource.Module Module => Resource.Module.PointCloud;
        [DataMember] public bool Visible { get; set; } = true;
        [DataMember] public string Topic { get; set; } = "";
        [DataMember] public Resource.ColormapId Colormap { get; set; } = Resource.ColormapId.gray;
        [DataMember] public bool FlipColors { get; set; } = true;
        [DataMember] public float ScaleZ { get; set; } = 1.0f;
    }

    public class OccupancyGridListener : TopicListener
    {
        DisplayClickableNode node;
        OccupancyGridResource grid;

        public override DisplayData DisplayData { get; set; }

        readonly OccupancyGridConfiguration config = new OccupancyGridConfiguration();
        public OccupancyGridConfiguration Config
        {
            get => config;
            set
            {
                config.Topic = value.Topic;
                Visible = value.Visible;
                Colormap = value.Colormap;
                FlipColors = value.FlipColors;
            }
        }

        public bool Visible
        {
            get => config.Visible;
            set
            {
                config.Visible = value;
                grid.Visible = value;
            }
        }

        public Resource.ColormapId Colormap
        {
            get => config.Colormap;
            set
            {
                config.Colormap = value;
                grid.Colormap = value;
            }
        }

        public bool FlipColors
        {
            get => config.FlipColors;
            set
            {
                config.FlipColors = value;
                grid.IntensityBounds = value ? new Vector2(1, 0) : new Vector2(0, 1);
            }
        }

        public float ScaleZ
        {
            get => config.ScaleZ;
            set
            {
                config.ScaleZ = value;
                node.transform.localScale = new Vector3(1, value, 1);
            }
        }

        void Awake()
        {
            node = DisplayClickableNode.Instantiate("Node");
            grid = ResourcePool.GetOrCreate<OccupancyGridResource>(Resource.Markers.OccupancyGridResource, node.transform);

            Config = new OccupancyGridConfiguration();
        }

        public override void StartListening()
        {
            base.StartListening();
            Listener = new RosListener<OccupancyGrid>(config.Topic, Handler);
            name = "OccupancyGrid:" + config.Topic;
            node.SetName($"[{config.Topic}]");
        }

        void Handler(OccupancyGrid msg)
        {
            if (msg.Data.Length != msg.Info.Width * msg.Info.Height)
            {
                Logger.Debug($"OccupancyGrid: Size {msg.Info.Width}x{msg.Info.Height} but data length {msg.Data.Length}");
                return;
            }
            if (float.IsNaN(msg.Info.Resolution))
            {
                Logger.Debug($"OccupancyGrid: NaN in header!");
                return;
            }
            if (!msg.Info.Origin.ValidateNaN())
            {
                Logger.Debug($"OccupancyGrid: NaN in origin!");
                return;
            }

            node.AttachTo(msg.Header.FrameId, msg.Header.Stamp.ToDateTime());

            Pose origin = msg.Info.Origin.Ros2Unity();

            grid.transform.SetLocalPose(origin);
            grid.NumCellsX = (int)msg.Info.Width;
            grid.NumCellsY = (int)msg.Info.Height;
            grid.CellSize = msg.Info.Resolution;

            origin.position += new Vector3(grid.NumCellsX, grid.NumCellsY, 0).Ros2Unity() * (grid.CellSize / 2f);
            grid.SetOccupancy(msg.Data);
        }


        public override void Stop()
        {
            base.Stop();
            ResourcePool.Dispose(Resource.Markers.OccupancyGridResource, grid.gameObject);
            node.Stop();
            Destroy(node);
        }
    }
}


