﻿using Iviz.App.Listeners;
using Iviz.Resources;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Iviz.App
{
    public class TFDisplayData : DisplayableListenerData
    {
        readonly TFListener listener;
        readonly TFPanelContents panel;

        protected override TopicListener Listener => listener;

        public override DataPanelContents Panel => panel;
        public override Resource.Module Module => Resource.Module.TF;
        public override IConfiguration Configuration => listener.Config;

        public TFDisplayData(DisplayDataConstructor constructor) :
            base(constructor.DisplayList, ((TFConfiguration)constructor.Configuration)?.Topic ?? constructor.Topic, constructor.Type)
        {
            GameObject listenerObject = Resource.Listeners.TF.Instantiate();
            listenerObject.name = "TF";

            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.TF) as TFPanelContents;
            listener = listenerObject.GetComponent<TFListener>();
            if (constructor.Configuration != null)
            {
                listener.Config = (TFConfiguration)constructor.Configuration;
            }
            else
            {
                listener.Config.Topic = Topic;
            }
            listener.StartListening();
            UpdateButtonText();
        }

        public void UpdateConfiguration(TFConfiguration configuration)
        {
            listener.Config = configuration;
        }

        /*
        public override DisplayData Initialize(DisplayListPanel displayList, string topic, string type)
        {
            base.Initialize(displayList, topic, type);
            GameObject listenerObject = ResourcePool.GetOrCreate(Resource.Listeners.TF);
            listenerObject.name = "TF";

            listener = listenerObject.GetComponent<TFListener>();
            listener.Config.Topic = Topic;

            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.TF) as TFPanelContents;

            return this;
        }

        public override void Start()
        {
            base.Start();
            listener.StartListening();
        }
        */

        public override void SetupPanel()
        {
            panel.ShowAxes.Value = listener.AxisVisible;
            panel.FrameSize.Value = listener.AxisSize;
            panel.ShowAxes.Value = listener.AxisVisible;
            panel.FrameLabelSize.Value = listener.AxisLabelSize;
            panel.FrameLabelSize.Interactable = listener.AxisLabelVisible;
            panel.ConnectToParent.Value = listener.ParentConnectorVisible;
            panel.ShowAllFrames.Value = listener.ShowAllFrames;

            panel.ShowAxes.ValueChanged += f =>
            {
                listener.AxisVisible = f;
            };
            panel.ShowFrameLabels.ValueChanged += f =>
            {
                listener.AxisLabelVisible = f;
                panel.FrameLabelSize.Interactable = f;
            };
            panel.FrameSize.ValueChanged += f =>
            {
                listener.AxisSize = f;
            };
            panel.FrameLabelSize.ValueChanged += f =>
            {
                listener.AxisLabelSize = f;
            };
            panel.ConnectToParent.ValueChanged += f =>
            {
                listener.ParentConnectorVisible = f;
            };
            panel.ShowAllFrames.ValueChanged += f =>
            {
                listener.ShowAllFrames = f;
            };
        }

        /*
        public override DisplayData Deserialize(JToken j)
        {
            listener.Config = j.ToObject<TFConfiguration>();
            Topic = listener.Config.Topic;
            return this;
        }

        public override JToken Serialize()
        {
            return JToken.FromObject(listener.Config);
        }
        */

        public override void AddToState(StateConfiguration config)
        {
            config.Tf = listener.Config;
        }
    }
}
