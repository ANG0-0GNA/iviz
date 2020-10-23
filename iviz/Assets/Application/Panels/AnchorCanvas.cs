﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Iviz.App
{

    public class AnchorCanvas : MonoBehaviour
    {
        [SerializeField] AnchorToggleButton hideGui = null;
        [SerializeField] AnchorToggleButton arSet = null;
        [SerializeField] Button unlock  = null;
        [SerializeField] AnchorToggleButton pinMarker = null;
        [SerializeField] AnchorToggleButton showMarker = null;

        public AnchorToggleButton HideGui => hideGui;
        public AnchorToggleButton ArSet => arSet;
        public Button Unlock => unlock;
        public AnchorToggleButton PinMarker => pinMarker;
        public AnchorToggleButton ShowMarker => showMarker;
    }
}
 