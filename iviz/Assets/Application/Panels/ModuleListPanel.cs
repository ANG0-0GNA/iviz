﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Iviz.Controllers;
using Iviz.Core;
using Iviz.Displays;
using Iviz.Resources;
using Iviz.Ros;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Iviz.Core.Logger;

namespace Iviz.App
{
    public sealed class ModuleListPanel : MonoBehaviour
    {
        const float YOffset = 2;

        public const int ModuleDataCaptionWidth = 200;

        static readonly Color ConnectedColor = new Color(0.6f, 1f, 0.5f, 0.4f);
        static readonly Color ConnectedOwnMasterColor = new Color(0.4f, 0.95f, 1f, 0.4f);
        static readonly Color DisconnectedColor = new Color(0.9f, 0.95f, 1f, 0.4f);

        [SerializeField] DataLabelWidget masterUriStr = null;
        [SerializeField] TrashButtonWidget masterUriButton = null;
        [SerializeField] TrashButtonWidget connectButton = null;
        [SerializeField] TrashButtonWidget stopButton = null;
        [SerializeField] Image topPanel = null;
        [SerializeField] Button save = null;
        [SerializeField] Button load = null;
        [SerializeField] Image status = null;

        [SerializeField] AnchorCanvas anchorCanvas = null;
        [SerializeField] GameObject contentObject = null;
        [SerializeField] DataPanelManager dataPanelManager = null;
        [SerializeField] DialogPanelManager dialogPanelManager = null;
        [SerializeField] Button addDisplayByTopic = null;
        [SerializeField] Button addDisplay = null;
        [SerializeField] Button showTfTree = null;
        [SerializeField] Button resetAll = null;

        [SerializeField] Sprite connectedSprite = null;
        [SerializeField] Sprite connectingSprite = null;
        [SerializeField] Sprite disconnectedSprite = null;
        [SerializeField] Sprite questionSprite = null;

        [SerializeField] Text bottomTime = null;
        [SerializeField] Text bottomFps = null;
        [SerializeField] Text bottomBandwidth = null;

        [SerializeField] Joystick joystick = null;

        [ItemNotNull] readonly List<GameObject> buttons = new List<GameObject>();
        [ItemNotNull] readonly List<ModuleData> moduleDatas = new List<ModuleData>();
        [ItemNotNull] readonly HashSet<string> topicsWithModule = new HashSet<string>();

        int frameCounter;
        float buttonHeight;

        Canvas parentCanvas;
        DialogData availableModules;
        DialogData availableTopics;
        ConnectionDialogData connectionData;
        ImageDialogData imageData;
        LoadConfigDialogData loadConfigData;
        SaveConfigDialogData saveConfigData;
        TfDialogData tfTreeData;
        MarkerDialogData markerData;

        ControllerService controllerService;

        public ModuleListPanel()
        {
            ModuleDatas = moduleDatas.AsReadOnly();
        }

        public bool AllGuiVisible
        {
            get => parentCanvas.gameObject.activeSelf;
            set => parentCanvas.gameObject.SetActive(value);
        }

        public static ModuleListPanel Instance { get; private set; }
        public static AnchorCanvas AnchorCanvas => Instance.anchorCanvas;
        AnchorToggleButton HideGuiButton => anchorCanvas.HideGui;
        public AnchorToggleButton ShowRootMarkerButton => anchorCanvas.ShowMarker;
        public AnchorToggleButton PinControlButton => anchorCanvas.PinMarker;
        public Button UnlockButton => anchorCanvas.Unlock;
        public DataPanelManager DataPanelManager => dataPanelManager;
        public DialogPanelManager DialogPanelManager => dialogPanelManager;
        public Joystick Joystick => joystick;
        [NotNull] public IReadOnlyCollection<ModuleData> ModuleDatas { get; }
        [NotNull] TfModuleData TfData => (TfModuleData) moduleDatas[0];
        [NotNull] public IEnumerable<string> DisplayedTopics => topicsWithModule;

        public bool UnlockButtonVisible
        {
            get => UnlockButton.gameObject.activeSelf;
            set => UnlockButton.gameObject.SetActive(value);
        }

        bool KeepReconnecting
        {
            get => ConnectionManager.Connection.KeepReconnecting;
            set
            {
                ConnectionManager.Connection.KeepReconnecting = value;
                status.enabled = value;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            parentCanvas = transform.parent.parent.GetComponentInParent<Canvas>();

            buttonHeight = Resource.Widgets.DisplayButton.Object.GetComponent<RectTransform>().rect.height;

            availableModules = new AddModuleDialogData(this);
            availableTopics = new AddTopicDialogData(this);

            imageData = new ImageDialogData(this);
            tfTreeData = new TfDialogData(this);
            loadConfigData = new LoadConfigDialogData(this);
            saveConfigData = new SaveConfigDialogData(this);
            markerData = new MarkerDialogData(this);
            
            connectionData = new ConnectionDialogData(this);

            Directory.CreateDirectory(Settings.SavedFolder);
            LoadSimpleConfiguration();

            Logger.Internal("<b>Welcome to iviz</b>");

            CreateModule(Resource.Module.TF, TfListener.DefaultTopic);
            CreateModule(Resource.Module.Grid);

            if (Settings.IsHololens)
            {
                ARController controller = (ARController) CreateModule(Resource.Module.AugmentedReality).Controller;
                controller.Visible = true;
            }

            save.onClick.AddListener(saveConfigData.Show);
            load.onClick.AddListener(loadConfigData.Show);

            HideGuiButton.Clicked += OnHideGuiButtonClick;
            HideGuiButton.State = true;

            addDisplayByTopic.onClick.AddListener(availableTopics.Show);
            addDisplay.onClick.AddListener(availableModules.Show);
            showTfTree.onClick.AddListener(tfTreeData.Show);
            resetAll.onClick.AddListener(ResetAllModules);


            masterUriStr.Label = connectionData.MasterUri + " →";
            masterUriButton.Clicked += () =>
            {
                connectionData.Show();
            };

            ConnectionManager.Connection.MasterUri = connectionData.MasterUri;
            ConnectionManager.Connection.MyUri = connectionData.MyUri;
            ConnectionManager.Connection.MyId = connectionData.MyId;
            KeepReconnecting = false;

            connectionData.MasterUriChanged += uri =>
            {
                ConnectionManager.Connection.MasterUri = uri;
                KeepReconnecting = false;
                if (uri == null)
                {
                    Logger.Internal("Failed to set master uri.");
                    masterUriStr.Label = "(?) →";
                }
                else if (RosServerManager.IsActive)
                {
                    Logger.Internal($"Changing master uri to local master '{uri}'");
                    masterUriStr.Label = "Master Mode\n" + uri + " →";
                }
                else
                {
                    Logger.Internal($"Changing master uri to '{uri}'");
                    masterUriStr.Label = uri + " →";
                }
            };
            connectionData.MyIdChanged += id =>
            {
                if (id == null)
                {
                    Logger.Internal("Failed to set caller id.");
                    Logger.Internal("* First character must be /");
                    Logger.Internal("* Second character must be alpha [a-z A-Z]");
                    Logger.Internal("* Remaining characters must be alpha, digits, _ or /");
                    return;
                }

                ConnectionManager.Connection.MyId = id;
                KeepReconnecting = false;
                Logger.Internal($"Changing caller id to '{id}'");
            };
            connectionData.MyUriChanged += uri =>
            {
                ConnectionManager.Connection.MyUri = uri;
                KeepReconnecting = false;
                Logger.Internal(
                    uri == null ? "Failed to set caller uri." : $"Changing caller uri to '{uri}'"
                );
            };
            stopButton.Clicked += () =>
            {
                Logger.Internal(
                    ConnectionManager.IsConnected
                        ? "Disconnection requested."
                        : "Disconnection requested (but already disconnected)."
                );
                KeepReconnecting = false;
                ConnectionManager.Connection.Disconnect();
            };
            connectButton.Clicked += () =>
            {
                Logger.Internal(
                    ConnectionManager.IsConnected ? "Reconnection requested." : "Connection requested."
                );
                ConnectionManager.Connection.Disconnect();
                KeepReconnecting = true;
            };

            connectionData.MasterActiveChanged += _ =>
            {
                ConnectionManager.Connection.Disconnect();
            };

            ConnectionManager.Connection.ConnectionStateChanged += OnConnectionStateChanged;
            ARController.ARModeChanged += OnARModeChanged;
            GameThread.LateEverySecond += UpdateFpsStats;
            GameThread.EveryFrame += UpdateFpsCounter;
            UpdateFpsStats();

            controllerService = new ControllerService();
        }

        void OnConnectionStateChanged(ConnectionState state)
        {
            status.rectTransform.localRotation = Quaternion.identity;

            if (ConnectionManager.Connection.MasterUri == null ||
                ConnectionManager.Connection.MyUri == null ||
                ConnectionManager.Connection.MyId == null)
            {
                status.sprite = questionSprite;
                return;
            }

            switch (state)
            {
                case ConnectionState.Connected:
                    GameThread.EverySecond -= RotateSprite;
                    status.sprite = connectedSprite;
                    topPanel.color = RosServerManager.IsActive ? ConnectedOwnMasterColor : ConnectedColor;
                    SaveSimpleConfiguration();
                    break;
                case ConnectionState.Disconnected:
                    GameThread.EverySecond -= RotateSprite;
                    status.sprite = disconnectedSprite;
                    topPanel.color = DisconnectedColor;
                    break;
                case ConnectionState.Connecting:
                    status.sprite = connectingSprite;
                    GameThread.EverySecond += RotateSprite;
                    break;
            }
        }

        void RotateSprite()
        {
            status.rectTransform.Rotate(new Vector3(0, 0, 10.0f), Space.Self);
        }

        void OnHideGuiButtonClick()
        {
            AllGuiVisible = !AllGuiVisible;
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void SaveStateConfiguration([NotNull] string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            StateConfiguration config = new StateConfiguration
            {
                MasterUri = connectionData.MasterUri,
                MyUri = connectionData.MyUri,
                MyId = connectionData.MyId,
                Entries = moduleDatas.Select(x => x.Configuration.Id).ToList()
            };
            foreach (var moduleData in moduleDatas)
            {
                moduleData.AddToState(config);
            }

            try
            {
                Logger.Internal("Saving config file...");
                string text = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText($"{Settings.SavedFolder}/{file}", text);
                Logger.Internal("Done.");
            }
            catch (Exception e) when
                (e is IOException || e is SecurityException || e is JsonException)
            {
                Logger.Error(e);
                Logger.Internal("Error:", e);
                return;
            }

            Logger.Debug("DisplayListPanel: Writing config to " + Settings.SavedFolder + "/" + file);
        }

        public void LoadStateConfiguration([NotNull] string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Logger.Debug("DisplayListPanel: Reading config from " + Settings.SavedFolder + "/" + file);
            string text;
            try
            {
                Logger.Internal("Loading config file...");
                text = File.ReadAllText(Settings.SavedFolder + "/" + file);
                Logger.Internal("Done.");
            }
            catch (FileNotFoundException)
            {
                Logger.Internal("Error: No config file found.");
                return;
            }
            catch (Exception e) when
                (e is IOException || e is SecurityException || e is JsonException)
            {
                Logger.Error(e);
                Logger.Internal("Error:", e);
                return;
            }

            while (moduleDatas.Count > 1)
            {
                // TODO: refine this
                RemoveModule(1);
            }

            StateConfiguration stateConfig = JsonConvert.DeserializeObject<StateConfiguration>(text);

            connectionData.MasterUri = stateConfig.MasterUri;
            connectionData.MyUri = stateConfig.MyUri;
            connectionData.MyId = stateConfig.MyId;

            TfData.UpdateConfiguration(stateConfig.Tf);

            var configurations = stateConfig.CreateListOfEntries()
                .SelectMany(config => config)
                .Where(config => config != null);

            foreach (var config in configurations)
            {
                CreateModule(config.Module, configuration: config);
            }

            if (connectionData.MasterUri != null &&
                connectionData.MyUri != null &&
                connectionData.MyId != null)
            {
                KeepReconnecting = true;
            }
        }

        void LoadSimpleConfiguration()
        {
            string path = Settings.SimpleConfigurationPath;
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string text = File.ReadAllText(path);
                ConnectionConfiguration config = JsonConvert.DeserializeObject<ConnectionConfiguration>(text);
                connectionData.MasterUri = config.MasterUri;
                connectionData.MyUri = config.MyUri;
                connectionData.MyId = config.MyId;
            }
            catch (Exception e) when
                (e is IOException || e is SecurityException || e is JsonException)
            {
                //Debug.Log(e);
            }
        }

        void SaveSimpleConfiguration()
        {
            try
            {
                ConnectionConfiguration config = new ConnectionConfiguration
                {
                    MasterUri = connectionData.MasterUri,
                    MyUri = connectionData.MyUri,
                    MyId = connectionData.MyId
                };

                string text = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(Settings.SimpleConfigurationPath, text);
            }
            catch (Exception e) when
                (e is IOException || e is SecurityException || e is JsonException)
            {
                //Debug.Log(e);
            }
        }

        void ResetAllModules()
        {
            foreach (ModuleData m in moduleDatas)
            {
                m.ResetController();
            }
        }

        [NotNull]
        public ModuleData CreateModule(Resource.Module resource, [NotNull] string topic = "",
            [NotNull] string type = "",
            [CanBeNull] IConfiguration configuration = null)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ModuleDataConstructor constructor =
                new ModuleDataConstructor(resource, this, topic, type, configuration);

            ModuleData moduleData = ModuleData.CreateFromResource(constructor);

            moduleDatas.Add(moduleData);
            CreateButtonObject(moduleData);
            
            return moduleData;
        }

        // TODO: move graphics out of this
        void CreateButtonObject([NotNull] ModuleData moduleData)
        {
            GameObject buttonObject =
                ResourcePool.GetOrCreate(Resource.Widgets.DisplayButton, contentObject.transform, false);

            int size = buttons.Count;
            float y = 2 * YOffset + size * (buttonHeight + YOffset);

            ((RectTransform) buttonObject.transform).anchoredPosition = new Vector2(0, -y);

            Text buttonObjectText = buttonObject.GetComponentInChildren<Text>();
            buttonObjectText.text = moduleData.ButtonText;
            buttonObject.name = $"Button:{moduleData.Module}";
            buttonObject.SetActive(true);
            buttons.Add(buttonObject);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(moduleData.ToggleShowPanel);
            ((RectTransform) contentObject.transform).sizeDelta = new Vector2(0, y + buttonHeight + YOffset);
        }

        [NotNull]
        public ModuleData CreateModuleForTopic([NotNull] string topic, [NotNull] string type)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!Resource.ResourceByRosMessageType.TryGetValue(type, out Resource.Module resource))
            {
                throw new ArgumentException(nameof(type));
            }

            return CreateModule(resource, topic, type);
        }

        public void RemoveModule([NotNull] ModuleData entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            RemoveModule(moduleDatas.IndexOf(entry));
        }

        void RemoveModule(int index)
        {
            topicsWithModule.Remove(moduleDatas[index].Topic);
            moduleDatas[index].Stop();
            moduleDatas.RemoveAt(index);

            RemoveButton(index);
        }

        // TODO: move graphics out of this
        void RemoveButton(int index)
        {
            GameObject displayButton = buttons[index];
            buttons.RemoveAt(index);

            displayButton.GetComponent<Button>().onClick.RemoveAllListeners();
            ResourcePool.Dispose(Resource.Widgets.DisplayButton, displayButton);

            int i;
            for (i = index; i < buttons.Count; i++)
            {
                GameObject buttonObject = buttons[i];
                float y = 2 * YOffset + i * (buttonHeight + YOffset);
                ((RectTransform) buttonObject.transform).anchoredPosition = new Vector3(0, -y);
            }

            ((RectTransform) contentObject.transform).sizeDelta =
                new Vector2(0, 2 * YOffset + i * (buttonHeight + YOffset));
        }

        public void UpdateModuleButton([NotNull] ModuleData entry, [NotNull] string content)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            int index = moduleDatas.IndexOf(entry);
            if (index == -1)
            {
                return;
            }

            GameObject buttonObject = buttons[index];
            Text text = buttonObject.GetComponentInChildren<Text>();
            text.text = content;
            int lineBreaks = content.Count(x => x == '\n');
            switch (lineBreaks)
            {
                case 2:
                    text.fontSize = 11;
                    break;
                case 3:
                    text.fontSize = 10;
                    break;
                default:
                    text.fontSize = 12;
                    break;
            }
        }

        public void RegisterDisplayedTopic([NotNull] string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            topicsWithModule.Add(topic);
        }

        public void ShowImageDialog([NotNull] IImageDialogListener caller)
        {
            imageData.Show(caller ?? throw new ArgumentNullException(nameof(caller)));
        }
        
        public void ShowMarkerDialog([NotNull] IMarkerDialogListener caller)
        {
            markerData.Show(caller ?? throw new ArgumentNullException(nameof(caller)));
        }

        public void ShowFrame([NotNull] TfFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(nameof(frame));
            }

            tfTreeData.Show(frame);
        }

        void UpdateFpsStats()
        {
            bottomTime.text = $"<b>{DateTime.Now.ToString("HH:mm:ss")}</b>";
            bottomFps.text = $"<b>{frameCounter.ToString()} FPS</b>";
            frameCounter = 0;

            var (downB, upB) = ConnectionManager.CollectBandwidthReport();
            int downKb = downB / 1000;
            int upKb = upB / 1000;
            bottomBandwidth.text = $"<b>↓{downKb.ToString("N0")}kB/s ↑{upKb.ToString("N0")}kB/s</b>";
        }

        void UpdateFpsCounter()
        {
            frameCounter++;
        }

        void OnARModeChanged(bool value)
        {
            PinControlButton.Visible = value;
            ShowRootMarkerButton.Visible = value;

            foreach (var module in ModuleDatas)
            {
                module.OnARModeChanged(value);
            }
        }
    }
}