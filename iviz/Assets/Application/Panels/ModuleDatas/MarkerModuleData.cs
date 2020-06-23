﻿using Iviz.App.Listeners;
using Iviz.Resources;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Iviz.App
{
    /// <summary>
    /// <see cref="MarkerPanelContents"/> 
    /// </summary>
    public class MarkerModuleData : ListenerModuleData
    {
        readonly MarkerListener listener;
        readonly MarkerPanelContents panel;

        public override DataPanelContents Panel => panel;
        protected override ListenerController Listener => listener;
        public override Resource.Module Module => Resource.Module.Marker;

        public override IConfiguration Configuration => listener.Config;

        public MarkerModuleData(ModuleDataConstructor constructor) :
            base(constructor.ModuleList,
                constructor.GetConfiguration<MarkerConfiguration>()?.Topic ?? constructor.Topic,
                constructor.GetConfiguration<MarkerConfiguration>()?.Type ?? constructor.Type)
        {
            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.Marker) as MarkerPanelContents;
            listener = Instantiate<MarkerListener>();
            listener.name = "Marker:" + Topic;
            listener.ModuleData = this;
            if (constructor.Configuration == null)
            {
                listener.Config.Topic = Topic;
                listener.Config.Type = Type;
            }
            else
            {
                listener.Config = (MarkerConfiguration)constructor.Configuration;
            }
            listener.StartListening();
            UpdateButtonText();
        }

        public override void SetupPanel()
        {
            panel.Listener.RosListener = listener.Listener;

            panel.OcclusionOnlyMode.Value = listener.RenderAsOcclusionOnly;
            panel.Tint.Value = listener.Tint;
            panel.Alpha.Value = listener.Tint.a;

            panel.Tint.ValueChanged += f =>
            {
                Color color = f;
                color.a = panel.Alpha.Value;
                listener.Tint = color;
            };
            panel.Alpha.ValueChanged += f =>
            {
                Color color = panel.Tint.Value;
                color.a = f;
                listener.Tint = color;
            };
            panel.OcclusionOnlyMode.ValueChanged += f =>
            {
                listener.RenderAsOcclusionOnly = f;
            };

            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                ModuleListPanel.RemoveModule(this);
            };
        }

        public override void AddToState(StateConfiguration config)
        {
            config.Markers.Add(listener.Config);
        }
    }
}
