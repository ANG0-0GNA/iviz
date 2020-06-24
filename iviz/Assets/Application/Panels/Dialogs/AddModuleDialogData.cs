﻿using System;
using System.Collections.Generic;
using System.Linq;
using Iviz.Resources;

namespace Iviz.App
{
    public class AddModuleDialogData : DialogData
    {
        static readonly List<Tuple<string, Resource.Module>> Modules = new List<Tuple<string, Resource.Module>>()
        {
            Tuple.Create("<b>Robot</b>\nA robot object", Resource.Module.Robot),
            Tuple.Create("<b>Grid</b>\nA reference plane", Resource.Module.Grid),
            Tuple.Create("<b>DepthProjector</b>\nPoint cloud generator for depth images", Resource.Module.DepthImageProjector),
            Tuple.Create("<b>AR</b>\nManager for augmented reality", Resource.Module.AR),
            Tuple.Create("<b>Joystick</b>\nOn-screen joystick", Resource.Module.Joystick),
        };

        DialogItemList itemList;
        public override IDialogPanelContents Panel => itemList;

        public override void Initialize(DisplayListPanel panel)
        {
            base.Initialize(panel);
            itemList = (DialogItemList)DialogPanelManager.GetPanelByType(DialogPanelType.ItemList);
        }

        public override void SetupPanel()
        {
            itemList.Title = "Available Modules";
            itemList.Items = Modules.Select(x => x.Item1);
            itemList.ItemClicked += OnItemClicked;
            itemList.CloseClicked += OnCloseClicked;

            bool hasAR = ModuleListPanel.ModuleDatas.Any(x => x.Module == Resource.Module.AR);
            itemList[3].Interactable = !hasAR;

            bool hasJoystick = ModuleListPanel.ModuleDatas.Any(x => x.Module == Resource.Module.Joystick);
            itemList[4].Interactable = !hasJoystick;
        }

        void OnCloseClicked()
        {
            Close();
        }

        void OnItemClicked(int index, string _)
        {
            ModuleListPanel.CreateModule(Modules[index].Item2);
            Close();
        }

        void Close()
        {
            DialogPanelManager.HidePanelFor(this);
        }

        public override void CleanupPanel()
        {
            base.CleanupPanel();
            itemList[3].Interactable = true;
            itemList[4].Interactable = true;
        }

        /*
        public override void Cleanup()
        {
            base.Cleanup();
        }
        */

    }
}