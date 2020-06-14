﻿using System;
using Iviz.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Iviz.App
{
    public class InputFieldWidget : MonoBehaviour, IWidget
    {
        [SerializeField] Text label = null;
        [SerializeField] InputField text = null;
        [SerializeField] Text placeholder = null;
        [SerializeField] Image textImage = null;

        public string Label
        {
            get => label.text;
            set
            {
                label.text = value;
                name = "InputField:" + value;
            }
        }
        public string Value
        {
            get => text.text;
            set
            {
                text.text = value;
            }
        }

        public string Placeholder
        {
            get => placeholder.text;
            set
            {
                placeholder.text = value;
            }
        }

        public InputField.ContentType ContentType
        {
            get => text.contentType;
            set
            {
                text.contentType = value;
            }
        }

        public bool Interactable
        {
            get => text.interactable;
            set
            {
                text.interactable = value;
                textImage.raycastTarget = value;
                label.color = value ? Resource.Colors.EnabledFontColor : Resource.Colors.DisabledFontColor;
            }
        }

        public event Action<string> ValueChanged;
        public event Action<string> EndEdit;

        public void OnValueChanged(string f)
        {
            ValueChanged?.Invoke(f);
        }

        public void OnEndEdit(string f)
        {
            EndEdit?.Invoke(f);
        }

        public void ClearSubscribers()
        {
            ValueChanged = null;
            EndEdit = null;
        }

        public InputFieldWidget SetInteractable(bool f)
        {
            Interactable = f;
            return this;
        }

        public InputFieldWidget SetValue(string f)
        {
            Value = f;
            return this;
        }

        public InputFieldWidget SetPlaceholder(string f)
        {
            Placeholder = f;
            return this;
        }

        public InputFieldWidget SetContentType(InputField.ContentType contentType)
        {
            ContentType = contentType;
            return this;
        }

        public InputFieldWidget SetLabel(string f)
        {
            Label = f;
            return this;
        }

        public InputFieldWidget SubscribeValueChanged(Action<string> f)
        {
            ValueChanged += f;
            return this;
        }

        public InputFieldWidget SubscribeEndEdit(Action<string> f)
        {
            EndEdit += f;
            return this;
        }
    }
}