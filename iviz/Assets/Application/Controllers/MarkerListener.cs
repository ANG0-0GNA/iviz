﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Iviz.Displays;
using Iviz.Msgs.VisualizationMsgs;
using Iviz.Resources;
using Iviz.Roslib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Iviz.Controllers
{
    [DataContract]
    public sealed class MarkerConfiguration : JsonToString, IConfiguration
    {
        [DataMember] public string Topic { get; set; } = "";
        [DataMember] public string Type { get; set; } = "";
        [DataMember] public bool RenderAsOcclusionOnly { get; set; }
        [DataMember] public SerializableColor Tint { get; set; } = Color.white;
        [DataMember] public Guid Id { get; set; } = Guid.NewGuid();
        [DataMember] public Resource.Module Module => Resource.Module.Marker;
        [DataMember] public bool Visible { get; set; } = true;
    }

    public sealed class MarkerListener : ListenerController
    {
        readonly MarkerConfiguration config = new MarkerConfiguration();
        readonly Dictionary<string, MarkerObject> markers = new Dictionary<string, MarkerObject>();

        public MarkerListener(IModuleData moduleData)
        {
            ModuleData = moduleData;
        }

        public override IModuleData ModuleData { get; }
        public override TfFrame Frame => TFListener.MapFrame;

        public MarkerConfiguration Config
        {
            get => config;
            set
            {
                config.Topic = value.Topic;
                config.Type = value.Type;
                RenderAsOcclusionOnly = value.RenderAsOcclusionOnly;
                Tint = value.Tint;
                Visible = value.Visible;
            }
        }

        public bool RenderAsOcclusionOnly
        {
            get => config.RenderAsOcclusionOnly;
            set
            {
                config.RenderAsOcclusionOnly = value;

                foreach (var marker in markers.Values)
                {
                    marker.OcclusionOnly = value;
                }
            }
        }

        public Color Tint
        {
            get => config.Tint;
            set
            {
                config.Tint = value;

                foreach (var marker in markers.Values)
                {
                    marker.Tint = value;
                }
            }
        }

        public bool Visible
        {
            get => config.Visible;
            set
            {
                config.Visible = value;

                foreach (var marker in markers.Values)
                {
                    marker.Visible = value;
                }
            }
        }

        public override void StartListening()
        {
            switch (config.Type)
            {
                case Marker.RosMessageType:
                    Listener = new RosListener<Marker>(config.Topic, Handler);
                    break;
                case MarkerArray.RosMessageType:
                    Listener = new RosListener<MarkerArray>(config.Topic, Handler);
                    break;
            }

            GameThread.EverySecond += CheckDeadMarkers;
        }

        void CheckDeadMarkers()
        {
            DateTime now = DateTime.Now;
            var deadEntries = markers
                .Where(entry => entry.Value.ExpirationTime < now)
                .ToArray();
            foreach (var entry in deadEntries)
            {
                markers.Remove(entry.Key);
                DeleteMarkerObject(entry.Value);
            }
        }

        public override void StopController()
        {
            base.StopController();
            DestroyAllMarkers();

            GameThread.EverySecond -= CheckDeadMarkers;
        }

        public override void ResetController()
        {
            base.ResetController();
            DestroyAllMarkers();
        }

        void DestroyAllMarkers()
        {
            foreach (var marker in markers.Values)
            {
                DeleteMarkerObject(marker);
            }

            markers.Clear();
        }

        void Handler(MarkerArray msg)
        {
            foreach (var marker in msg.Markers)
            {
                Handler(marker);
            }
        }

        void Handler(Marker msg)
        {
            var id = IdFromMessage(msg);
            switch (msg.Action)
            {
                case Marker.ADD:
                    if (msg.Pose.HasNaN())
                    {
                        Logger.Debug("MarkerListener: NaN in pose!");
                        return;
                    }

                    if (msg.Scale.HasNaN())
                    {
                        Logger.Debug("MarkerListener: NaN in scale!");
                        return;
                    }

                    if (!markers.TryGetValue(id, out var markerToAdd))
                    {
                        markerToAdd = CreateMarkerObject();
                        markerToAdd.ModuleData = ModuleData;
                        markerToAdd.Parent = TFListener.ListenersFrame;
                        markerToAdd.OcclusionOnly = RenderAsOcclusionOnly;
                        markerToAdd.Tint = Tint;
                        markerToAdd.Visible = Visible;
                        markers[id] = markerToAdd;
                    }

                    markerToAdd.Set(msg);
                    break;
                case Marker.DELETE:
                    if (markers.TryGetValue(id, out var markerToDelete))
                    {
                        DeleteMarkerObject(markerToDelete);
                        markers.Remove(id);
                    }

                    break;
            }
        }

        public static string IdFromMessage(Marker marker)
        {
            return $"[{marker.Ns}] {marker.Id}";
        }

        static void DeleteMarkerObject(MarkerObject markerToDelete)
        {
            markerToDelete.Stop();
            Object.Destroy(markerToDelete.gameObject);
        }

        static MarkerObject CreateMarkerObject()
        {
            var gameObject = new GameObject("MarkerObject");
            return gameObject.AddComponent<MarkerObject>();
        }
    }
}