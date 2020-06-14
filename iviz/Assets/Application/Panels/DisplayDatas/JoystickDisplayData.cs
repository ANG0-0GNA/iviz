﻿using Iviz.App.Listeners;
using Iviz.Resources;
using UnityEngine;

namespace Iviz.App
{
    /// <summary>
    /// <see cref="JoystickPanelContents"/> 
    /// </summary>
    public class JoystickDisplayData : DisplayData
    {
        readonly JoystickController controller;
        readonly JoystickPanelContents panel;

        public override Resource.Module Module => Resource.Module.Joystick;
        public override DataPanelContents Panel => panel;
        public override IConfiguration Configuration => controller.Config;
        public override IController Controller => controller;

        public JoystickDisplayData(DisplayDataConstructor constructor) :
            base(constructor.DisplayList, constructor.Topic, constructor.Type)
        {
            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.Joystick) as JoystickPanelContents;

            controller = Resource.Listeners.Instantiate<JoystickController>();
            controller.DisplayData = this;
            if (constructor.Configuration != null)
            {
                controller.Config = (JoystickConfiguration)constructor.Configuration;
            }
            controller.Joystick = DisplayListPanel.Joystick;

            UpdateButtonText();
        }

        public override void Stop()
        {
            base.Stop();

            controller.Stop();
            Object.Destroy(controller.gameObject);
        }

        public override void SetupPanel()
        {
            panel.JoySender.Set(controller.RosSenderJoy);
            panel.TwistSender.Set(controller.RosSenderTwist);
            panel.SendJoy.Value = controller.PublishJoy;
            panel.SendTwist.Value = controller.PublishTwist;

            panel.MaxSpeed.Value = controller.MaxSpeed;
            panel.AttachToFrame.Value = controller.AttachToFrame;
            panel.XIsFront.Value = controller.XIsFront;

            panel.MaxSpeed.Interactable = controller.PublishTwist;
            panel.AttachToFrame.Interactable = controller.PublishTwist;


            panel.SendJoy.ValueChanged += f =>
            {
                controller.PublishJoy = f;
                panel.JoySender.Set(controller.RosSenderJoy);
            };
            panel.SendTwist.ValueChanged += f =>
            {
                controller.PublishTwist = f;
                panel.MaxSpeed.Interactable = f;
                panel.AttachToFrame.Interactable = f;
                panel.XIsFront.Interactable = f;
                panel.TwistSender.Set(controller.RosSenderTwist);
            };
            panel.MaxSpeed.ValueChanged += f =>
            {
                controller.MaxSpeed = f;
            };
            panel.AttachToFrame.ValueChanged += f =>
            {
                controller.AttachToFrame = f;
            };
            panel.XIsFront.ValueChanged += f =>
            {
                controller.XIsFront = f;
            };

            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                DisplayListPanel.RemoveDisplay(this);
            };
            panel.HideButton.Clicked += () =>
            {
                controller.Visible = !controller.Visible;
                panel.HideButton.State = controller.Visible;
                UpdateButtonText();
            };
        }

        public override void AddToState(StateConfiguration config)
        {
            config.Joystick = controller.Config;
        }
    }
}
