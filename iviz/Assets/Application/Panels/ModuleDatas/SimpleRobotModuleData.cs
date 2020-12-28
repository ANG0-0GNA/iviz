﻿using System.Collections.Generic;
using System.Linq;
using Iviz.Controllers;
using Iviz.Core;
using Iviz.Resources;
using Iviz.Ros;
using Iviz.Roslib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Iviz.App
{
    /// <summary>
    /// <see cref="SimpleRobotPanelContents"/> 
    /// </summary>
    public sealed class SimpleRobotModuleData : ModuleData
    {
        const string ParamSuffix = "_description";

        [NotNull] readonly SimpleRobotPanelContents panel;

        public override DataPanelContents Panel => panel;
        public override Resource.ModuleType ModuleType => Resource.ModuleType.Robot;
        public override IConfiguration Configuration => Robot.Config;
        public override IController Controller => Robot;

        [NotNull] public SimpleRobotController Robot { get; }

        static readonly string[] NoneStr = {"<color=#b0b0b0ff><i><none></i></color>"};

        public SimpleRobotModuleData([NotNull] ModuleDataConstructor constructor) :
            base(constructor.Topic, constructor.Type)
        {
            Robot = new SimpleRobotController(this);
            if (constructor.Configuration != null)
            {
                Robot.Config = (SimpleRobotConfiguration) constructor.Configuration;
            }

            panel = DataPanelManager.GetPanelByResourceType<SimpleRobotPanelContents>(Resource.ModuleType.Robot);
            UpdateModuleButton();

            ConnectionManager.Connection.ConnectionStateChanged += OnConnectionStateChanged;
        }

        void OnConnectionStateChanged(ConnectionState state)
        {
            if (state != ConnectionState.Connected)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Robot.SourceParameter))
            {
                Robot.TryLoadFromSourceParameter(Robot.SourceParameter);
            }

            panel.HelpText.Label = Robot.HelpText;
            UpdateModuleButton();
        }

        public override void Stop()
        {
            base.Stop();
            Robot.StopController();
            ConnectionManager.Connection.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        public override void SetupPanel()
        {
            panel.Frame.Owner = Robot;
            panel.SourceParam.Value = Robot.SourceParameter;
            panel.HelpText.Label = Robot.HelpText;

            panel.SourceParam.Hints = GetParameterHints();
            panel.SavedRobotName.Options = GetSavedRobots();

            panel.FramePrefix.Value = Robot.FramePrefix;
            panel.FrameSuffix.Value = Robot.FrameSuffix;
            panel.AttachToTf.Value = Robot.AttachedToTf;
            panel.HideButton.State = Robot.Visible;

            panel.OcclusionOnlyMode.Value = Robot.RenderAsOcclusionOnly;
            panel.Tint.Value = Robot.Tint;
            panel.Alpha.Value = Robot.Tint.a;
            panel.Metallic.Value = Robot.Metallic;
            panel.Smoothness.Value = Robot.Smoothness;

            panel.Save.Value = IsRobotSaved;
            panel.Save.Interactable = !string.IsNullOrEmpty(Robot.Robot?.Name);

            panel.Tint.ValueChanged += f =>
                Robot.Tint = f.WithAlpha(panel.Alpha.Value);
            panel.Alpha.ValueChanged += f =>
                Robot.Tint = panel.Tint.Value.WithAlpha(f);
            panel.Metallic.ValueChanged += f => Robot.Metallic = f;
            panel.Smoothness.ValueChanged += f => Robot.Smoothness = f;
            panel.OcclusionOnlyMode.ValueChanged += f => Robot.RenderAsOcclusionOnly = f;
            panel.SavedRobotName.ValueChanged += (i, name) =>
            {
                Robot.TryLoadSavedRobot(i == 0 ? null : name);
                panel.SourceParam.Value = "";
                panel.Save.Value = IsRobotSaved;

                panel.HelpText.Label = Robot.HelpText;
                UpdateModuleButton();

                panel.Save.Interactable =
                    !string.IsNullOrEmpty(Robot.Robot?.Name) &&
                    !Resource.Internal.ContainsRobot(name);
            };
            panel.SourceParam.EndEdit += f =>
            {
                Robot.TryLoadFromSourceParameter(f);
                panel.SavedRobotName.Index = 0;
                panel.Save.Value = IsRobotSaved;

                panel.HelpText.Label = Robot.HelpText;
                UpdateModuleButton();

                panel.Save.Interactable = !string.IsNullOrEmpty(Robot.Robot?.Name);
            };
            panel.AttachToTf.ValueChanged += f => 
                Robot.AttachedToTf = f;
            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                ModuleListPanel.RemoveModule(this);
            };
            panel.FramePrefix.EndEdit += f => Robot.FramePrefix = f;
            panel.FrameSuffix.EndEdit += f => Robot.FrameSuffix = f;
            panel.HideButton.Clicked += () =>
            {
                Robot.Visible = !Robot.Visible;
                panel.HideButton.State = Robot.Visible;
                UpdateModuleButton();
            };
            panel.Save.ValueChanged += f =>
            {
                if (string.IsNullOrEmpty(Robot?.Robot?.Name) || string.IsNullOrEmpty(Robot.Robot.Description))
                {
                    return;
                }

                if (f)
                {
                    Resource.External.AddRobotResource(Robot.Robot.Name, Robot.Robot.Description);
                }
                else
                {
                    Resource.External.RemoveRobotResource(Robot.Robot.Name);
                }
            };
        }

        public override void UpdatePanel()
        {
            base.UpdatePanel();
            panel.SourceParam.Hints = GetParameterHints();
        }

        static IEnumerable<string> GetParameterCandidates() =>
            ConnectionManager.Connection.GetSystemParameterList().Where(x => x.HasSuffix(ParamSuffix));

        static IEnumerable<string> GetSavedRobots() => NoneStr.Concat(Resource.GetRobotNames());

        static IEnumerable<string> GetParameterHints() => GetParameterCandidates();

        bool IsRobotSaved => Robot.Robot?.Name != null && Resource.ContainsRobot(Robot.Robot.Name);

        protected override void UpdateModuleButton()
        {
            ButtonText =
                $"{Resource.Font.Split(Robot.Name, ModuleListPanel.ModuleDataCaptionWidth)}\n<b>{ModuleType}</b>";
        }

        public override void UpdateConfiguration(string configAsJson, IEnumerable<string> fields)
        {
            var config = JsonConvert.DeserializeObject<SimpleRobotConfiguration>(configAsJson);
            bool hasRobotName = false;
            bool hasSourceParameter = false;

            foreach (string field in fields)
            {
                switch (field)
                {
                    case nameof(SimpleRobotConfiguration.Visible):
                        Robot.Visible = config.Visible;
                        break;
                    case nameof(SimpleRobotConfiguration.SourceParameter):
                        if (config.SourceParameter == Robot.Config.SourceParameter)
                        {
                            break;
                        }

                        hasSourceParameter = true;
                        break;
                    case nameof(SimpleRobotConfiguration.SavedRobotName):
                        if (config.SavedRobotName == Robot.Config.SavedRobotName)
                        {
                            break;
                        }

                        hasRobotName = true;
                        break;
                    case nameof(SimpleRobotConfiguration.FramePrefix):
                        Robot.FramePrefix = config.FramePrefix;
                        break;
                    case nameof(SimpleRobotConfiguration.FrameSuffix):
                        Robot.FrameSuffix = config.FrameSuffix;
                        break;
                    case nameof(SimpleRobotConfiguration.AttachedToTf):
                        Robot.AttachedToTf = config.AttachedToTf;
                        break;
                    case nameof(SimpleRobotConfiguration.RenderAsOcclusionOnly):
                        Robot.RenderAsOcclusionOnly = config.RenderAsOcclusionOnly;
                        break;
                    case nameof(SimpleRobotConfiguration.Tint):
                        Robot.Tint = config.Tint;
                        break;
                    case nameof(SimpleRobotConfiguration.Metallic):
                        Robot.Metallic = config.Metallic;
                        break;
                    case nameof(SimpleRobotConfiguration.Smoothness):
                        Robot.Smoothness = config.Smoothness;
                        break;
                    default:
                        Core.Logger.Warn($"{this}: Unknown field '{field}'");
                        break;
                }
            }

            if (hasRobotName || hasSourceParameter)
            {
                if (!hasRobotName)
                {
                    config.SavedRobotName = "";
                }

                if (!hasSourceParameter)
                {
                    config.SourceParameter = "";
                }

                Robot.ProcessRobotSource(config.SavedRobotName, config.SourceParameter);

                if (IsSelected)
                {
                    panel.HelpText.Label = Robot.HelpText;
                }

                UpdateModuleButton();
            }

            ResetPanel();
        }

        public override void AddToState(StateConfiguration config)
        {
            config.SimpleRobots.Add(Robot.Config);
        }
    }
}