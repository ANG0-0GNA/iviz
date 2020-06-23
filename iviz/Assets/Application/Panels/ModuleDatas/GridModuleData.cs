﻿using Iviz.App.Displays;
using Iviz.App.Listeners;
using Iviz.Displays;
using Iviz.Resources;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Iviz.App
{
    /// <summary>
    /// <see cref="GridPanelContents"/> 
    /// </summary>
    public class GridModuleData : ModuleData
    {
        readonly Listeners.GridController controller;
        readonly GridPanelContents panel;

        public override Resource.Module Module => Resource.Module.Grid;
        public override DataPanelContents Panel => panel;
        public override IConfiguration Configuration => controller.Config;
        public override IController Controller => controller;

        public GridModuleData(ModuleDataConstructor constructor) :
            base(constructor.ModuleList, constructor.Topic, constructor.Type)
        {
            panel = DataPanelManager.GetPanelByResourceType(Resource.Module.Grid) as GridPanelContents;

            controller = Instantiate<Listeners.GridController>();
            controller.ModuleData = this;
            if (constructor.Configuration != null)
            {
                controller.Config = (GridConfiguration)constructor.Configuration;
            }

            UpdateButtonText();
        }

        public override void Stop()
        {
            base.Stop();

            controller.Stop();
            Object.Destroy(controller.gameObject);
        }

        const float InteriorColorFactor = 0.5f;

        public override void SetupPanel()
        {
            panel.LineWidth.Value = controller.GridLineWidth;
            panel.NumberOfCells.Value = controller.NumberOfGridCells;
            panel.CellSize.Value = controller.GridCellSize;
            panel.Orientation.Index = (int)controller.Orientation;
            panel.ColorPicker.Value = controller.InteriorColor;
            panel.ShowInterior.Value = controller.ShowInterior;
            panel.HideButton.State = controller.Visible;
            panel.Offset.Value = controller.Offset;
            panel.FollowCamera.Value = controller.FollowCamera;

            panel.LineWidth.ValueChanged += f =>
            {
                controller.GridLineWidth = f;
            };
            panel.NumberOfCells.ValueChanged += f =>
            {
                controller.NumberOfGridCells = (int)f;
            };
            panel.CellSize.ValueChanged += f =>
            {
                controller.GridCellSize = f;
            };
            panel.Orientation.ValueChanged += (i, _) =>
            {
                controller.Orientation = (GridOrientation)i;
            };
            panel.ColorPicker.ValueChanged += f =>
            {
                controller.GridColor = f * InteriorColorFactor;
                controller.InteriorColor = f;
            };
            panel.ShowInterior.ValueChanged += f =>
            {
                controller.ShowInterior = f;
            };
            panel.CloseButton.Clicked += () =>
            {
                DataPanelManager.HideSelectedPanel();
                ModuleListPanel.RemoveModule(this);
            };
            panel.Offset.ValueChanged += f =>
            {
                controller.Offset = f;
            };
            panel.HideButton.Clicked += () =>
            {
                controller.Visible = !controller.Visible;
                panel.HideButton.State = controller.Visible;
                UpdateButtonText();
            };
            panel.FollowCamera.ValueChanged += f =>
            {
                controller.FollowCamera = f;
            };
        }

        public override void AddToState(StateConfiguration config)
        {
            config.Grids.Add(controller.Config);
        }
    }
}
