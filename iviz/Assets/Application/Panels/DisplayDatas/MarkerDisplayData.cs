﻿using Iviz.App.Listeners;
using Iviz.Resources;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Iviz.App
{
    public class MarkerDisplayData : ListenerDisplayData
    {
        readonly MarkerListener listener;
        readonly MarkerPanelContents panel;

        public override DataPanelContents Panel => panel;
        protected override TopicListener Listener => listener;
        public override Resource.Module Module => Resource.Module.Marker;

        public override IConfiguration Configuration => listener.Config;

        public MarkerDisplayData(DisplayDataConstructor constructor) :
            base(constructor.DisplayList,
                ((MarkerConfiguration)constructor.Configuration)?.Topic ?? constructor.Topic,
                ((MarkerConfiguration)constructor.Configuration)?.Type ?? constructor.Type)
        {
            GameObject listenerObject = Resource.Listeners.Marker.Instantiate();
            listenerObject.name = "Marker:" + Topic;

            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.Marker) as MarkerPanelContents;
            listener = listenerObject.GetComponent<MarkerListener>();
            listener.DisplayData = this;
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
            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                DisplayListPanel.RemoveDisplay(this);
            };
        }

        /*
        public override JToken Serialize()
        {
            return JToken.FromObject(listener.Config);
        }
        */

        public override void AddToState(StateConfiguration config)
        {
            config.Markers.Add(listener.Config);
        }
    }
}
