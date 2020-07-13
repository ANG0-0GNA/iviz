﻿namespace Iviz.App
{
    /// <summary>
    /// <see cref="TFModuleData"/> 
    /// </summary>
    public sealed class TFPanelContents : ListenerPanelContents
    {
        public ListenerWidget ListenerStatic { get; protected set; }
        public FrameWidget Frame { get; protected set; }
        public ToggleWidget ShowAxes { get; private set; }
        public ToggleWidget ShowFrameLabels { get; private set; }
        public SliderWidget FrameSize { get; private set; }
        public SliderWidget FrameLabelSize { get; private set; }
        public ToggleWidget ConnectToParent { get; private set; }
        public ToggleWidget KeepOnlyUsedFrames { get; private set; }
        public SenderWidget Sender { get; private set; }

        void Awake()
        {
            DataPanelWidgets p = GetComponent<DataPanelWidgets>();
            p.AddHeadTitleWidget("TF");
            Listener = p.AddListener();
            ListenerStatic = p.AddListener();
            Frame = p.AddFrame();
            KeepOnlyUsedFrames = p.AddToggle("Keep Only Used Frames");
            ShowAxes = p.AddToggle("Show Frames");
            ShowFrameLabels = p.AddToggle("Show Frame Names");
            ConnectToParent = p.AddToggle("Connect Children to Parents");
            FrameSize = p.AddSlider("Frame Size").SetMinValue(0.01f).SetMaxValue(0.5f).SetNumberOfSteps(49);
            FrameLabelSize = p.AddSlider("Frame Names Size").SetMinValue(0.01f).SetMaxValue(0.5f).SetNumberOfSteps(49);
            Sender = p.AddSender();
            p.UpdateSize();
            gameObject.SetActive(false);
        }
    }
}