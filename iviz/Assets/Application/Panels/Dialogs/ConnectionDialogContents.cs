﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Iviz.App
{
    public class ConnectionDialogContents : MonoBehaviour, IDialogPanelContents
    {
        public InputFieldWidget MasterUri;
        public InputFieldWidget MyUri;
        public InputFieldWidget MyId;
        public TrashButtonWidget RefreshMyUri;
        public TrashButtonWidget RefreshMyId;
        public TrashButtonWidget Close;
        public LineLog LineLog;
        //public Text Text;

        public bool Active
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public void ClearSubscribers()
        {
            MasterUri.ClearSubscribers();
            MyUri.ClearSubscribers();
            MyId.ClearSubscribers();
            RefreshMyUri.ClearSubscribers();
            RefreshMyId.ClearSubscribers();
            Close.ClearSubscribers();
        }
    }
}
