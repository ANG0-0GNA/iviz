﻿using System.Collections.Generic;

namespace Iviz.App
{
    public class JointStatePanelContents : ListenerPanelContents
    {
        public DataLabelWidget Topic { get; private set; }
        public DropdownWidget Robot { get; private set; }
        public InputFieldWidget JointPrefix { get; private set; }
        public InputFieldWidget JointSuffix { get; private set; }
        public SliderWidget TrimFromEnd { get; private set; }
        public TrashButtonWidget CloseButton { get; private set; }

        void Start()
        {
            DataPanelWidgets p = GetComponent<DataPanelWidgets>();
            p.AddHeadTitleWidget("JointState");
            Stats = p.AddSectionTitleWidget("Off | 0 Hz | 0 - 0 ms");
            Topic = p.AddDataLabel("");
            Robot = p.AddDropdown("Robot");
            JointPrefix = p.AddInputField("Joint Prefix").SetPlaceholder("<none>");
            JointSuffix = p.AddInputField("Joint Suffix").SetPlaceholder("<none>");
            TrimFromEnd = p.AddSlider("Trim End Characters").SetIntegerOnly(true).SetMinValue(0).SetMaxValue(10);
            CloseButton = p.AddTrashButton();
            p.UpdateSize();
            gameObject.SetActive(false);

            Topic.label.alignment = UnityEngine.TextAnchor.UpperLeft;

            Widgets = new Widget[] { JointPrefix, JointSuffix, TrimFromEnd, CloseButton, Robot };
        }
    }
}