﻿using Iviz.Controllers;
using Iviz.Resources;
using UnityEngine;

namespace Iviz.App
{
    /// <summary>
    /// <see cref="RobotPanelContents"/> 
    /// </summary>

    public sealed class SimpleRobotModuleData : ModuleData
    {
        readonly SimpleRobotPanelContents panel;

        readonly SimpleRobotController robot;

        public override DataPanelContents Panel => panel;
        public override Resource.Module Module => Resource.Module.SimpleRobot;
        public override IConfiguration Configuration => robot.Config;
        public override IController Controller => robot;

        public SimpleRobotModuleData(ModuleDataConstructor constructor) :
        base(constructor.ModuleList, constructor.Topic, constructor.Type)
        {
            robot = new SimpleRobotController(this);
            if (constructor.Configuration != null)
            {
                robot.Config = (SimpleRobotConfiguration)constructor.Configuration;
            }
            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.SimpleRobot) as SimpleRobotPanelContents;
            UpdateModuleButton();
        }

        public override void Stop()
        {
            base.Stop();
            robot.Stop();
        }

        public override void SetupPanel()
        {
            panel.Frame.Owner = robot;
            //panel.ResourceType.Value = robot.RobotResource;
            panel.FramePrefix.Value = robot.FramePrefix;
            panel.FrameSuffix.Value = robot.FrameSuffix;
            panel.AttachToTf.Value = robot.AttachToTf;
            panel.HideButton.State = robot.Visible;

            panel.OcclusionOnlyMode.Value = robot.RenderAsOcclusionOnly;
            panel.Tint.Value = robot.Tint;
            panel.Alpha.Value = robot.Tint.a;

            panel.Tint.ValueChanged += f =>
            {
                Color color = f;
                color.a = panel.Alpha.Value;
                robot.Tint = color;
            };
            panel.Alpha.ValueChanged += f =>
            {
                Color color = panel.Tint.Value;
                color.a = f;
                robot.Tint = color;
            };
            panel.OcclusionOnlyMode.ValueChanged += f =>
            {
                robot.RenderAsOcclusionOnly = f;
            };

            /*
            panel.ResourceType.ValueChanged += (_, f) =>
            {
                robot.RobotResource = f;
                UpdateModuleButton();
            };
            */
            panel.AttachToTf.ValueChanged += f =>
            {
                robot.AttachToTf = f;
            }; 
            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                ModuleListPanel.RemoveModule(this);
            };
            panel.FramePrefix.EndEdit += f =>
            {
                robot.FramePrefix = f;
            };
            panel.FrameSuffix.EndEdit += f =>
            {
                robot.FrameSuffix = f;
            };
            panel.HideButton.Clicked += () =>
            {
                robot.Visible = !robot.Visible;
                panel.HideButton.State = robot.Visible;
                UpdateModuleButton();
            };
        }

        protected override void UpdateModuleButton()
        {
            ButtonText = $"{Resource.Font.Split(robot.Name, ModuleListPanel.ModuleDataCaptionWidth)}\n<b>{Module}</b>";
        }

        public override void AddToState(StateConfiguration config)
        {
            //config.Robots.Add(robot.Config);
        }
    }
}
