﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Iviz.Core;
using Iviz.Msgs.IvizMsgs;
using Iviz.Resources;
using Iviz.Roslib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Logger = Iviz.Core.Logger;

namespace Iviz.Displays
{
    public class ExternalResourceManager
    {
        const string ModelServiceName = "/iviz/get_model_resource";
        const string TextureServiceName = "/iviz/get_model_texture";
        const string FileServiceName = "/iviz/get_file";
        const string SceneServiceName = "/iviz/get_sdf";

        const string StrMissingFileRemoving = "ExternalResourceManager: Missing file '{0}'. Removing.";
        const string StrServiceFailedWithMessage = "ExternalResourceManager: Call Service failed with message '{0}'";

        const string StrCallServiceFailed =
            "ExternalResourceManager: Call Service failed! Are you sure iviz is connected and the Iviz.Model.Service program is running?";

        const string StrResourceFailedWithError = "ExternalResourceManager: Loading resource {0} failed with error {1}";

        [DataContract]
        public class ResourceFiles
        {
            [DataMember] int Version { get; set; }
            [DataMember] public Dictionary<Uri, string> Models { get; set; }
            [DataMember] public Dictionary<Uri, string> Textures { get; set; }
            [DataMember] public Dictionary<Uri, string> Scenes { get; set; }
            [DataMember] public Dictionary<string, string> RobotDescriptions { get; set; }

            public ResourceFiles()
            {
                Models = new Dictionary<Uri, string>();
                Textures = new Dictionary<Uri, string>();
                Scenes = new Dictionary<Uri, string>();
                RobotDescriptions = new Dictionary<string, string>();
            }
        }

        readonly ResourceFiles resourceFiles = new ResourceFiles();

        readonly Dictionary<Uri, Info<GameObject>> loadedModels =
            new Dictionary<Uri, Info<GameObject>>();

        readonly Dictionary<Uri, Info<Texture2D>> loadedTextures =
            new Dictionary<Uri, Info<Texture2D>>();

        readonly Dictionary<Uri, Info<GameObject>> loadedScenes =
            new Dictionary<Uri, Info<GameObject>>();

        public GameObject Node { get; }

        readonly Model modelGenerator = new Model();
        readonly Scene sceneGenerator = new Scene();

        [NotNull]
        public ReadOnlyCollection<Uri> GetListOfModels() =>
            new ReadOnlyCollection<Uri>(resourceFiles.Models.Keys.ToList());

        public ExternalResourceManager(bool createNode = true)
        {
            if (createNode)
            {
                Node = new GameObject("External Resources");
                Node.SetActive(false);
            }

            try
            {
                Directory.CreateDirectory(Settings.ResourcesPath);
                Directory.CreateDirectory(Settings.SavedRobotsPath);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            if (!File.Exists(Settings.ResourcesFilePath))
            {
                Debug.Log("ExternalResourceManager: Failed to find file " + Settings.ResourcesFilePath);
                return;
            }

            Debug.Log("ExternalResourceManager: Using resource file " + Settings.ResourcesFilePath);

            try
            {
                string text = File.ReadAllText(Settings.ResourcesFilePath);
                resourceFiles = JsonConvert.DeserializeObject<ResourceFiles>(text);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        void WriteResourceFile()
        {
            try
            {
                File.WriteAllText(Settings.ResourcesFilePath,
                    JsonConvert.SerializeObject(resourceFiles, Formatting.Indented));
            }
            catch (IOException e)
            {
                Logger.Warn("ExternalResourceManager: Failed to write resource file! " + e);
            }
        }

        [NotNull]
        public static string SanitizeForFilename([NotNull] string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return Uri.EscapeDataString(input);
        }

        [NotNull]
        public IEnumerable<string> GetRobotNames() => resourceFiles.RobotDescriptions.Keys.ToArray();

        public bool ContainsRobot([NotNull] string robotName)
        {
            if (robotName == null)
            {
                throw new ArgumentNullException(nameof(robotName));
            }

            return resourceFiles.RobotDescriptions.ContainsKey(robotName);
        }

        [ContractAnnotation("=> false, robotDescription:null; => true, robotDescription:notnull")]
        public bool TryGetRobot([NotNull] string robotName, out string robotDescription)
        {
            if (string.IsNullOrEmpty(robotName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(robotName));
            }

            robotDescription = null;
            if (!resourceFiles.RobotDescriptions.TryGetValue(robotName, out string localPath))
            {
                return false;
            }

            string absolutePath = $"{Settings.SavedRobotsPath}/{localPath}";
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarningFormat(StrMissingFileRemoving, localPath);
                resourceFiles.RobotDescriptions.Remove(robotName);
                WriteResourceFile();
                return false;
            }

            try
            {
                robotDescription = File.ReadAllText(absolutePath);
                return true;
            }
            catch (IOException e)
            {
                Debug.Log("ExternalResourceManager: Failed to read robot '" + robotName + "' : " + e);
                return false;
            }
        }

        public void AddRobot([NotNull] string robotName, [NotNull] string robotDescription)
        {
            if (string.IsNullOrEmpty(robotName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(robotName));
            }

            if (string.IsNullOrEmpty(robotDescription))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(robotDescription));
            }

            string localPath = SanitizeForFilename(robotName);

            File.WriteAllText($"{Settings.SavedRobotsPath}/{localPath}", robotDescription);
            Debug.Log($"Saving to {Settings.SavedRobotsPath}/{localPath}");
            Logger.Internal($"Added robot <i>{robotName}</i> to cache");

            resourceFiles.RobotDescriptions[robotName] = localPath;
            WriteResourceFile();
        }

        public void RemoveRobot([NotNull] string robotName)
        {
            if (string.IsNullOrEmpty(robotName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(robotName));
            }

            if (!resourceFiles.RobotDescriptions.TryGetValue(robotName, out string localPath))
            {
                return;
            }

            string absolutePath = $"{Settings.ResourcesPath}/{localPath}";
            try
            {
                File.Delete(absolutePath);
                Logger.Debug($"Removing '{localPath}'");
            }
            catch (IOException)
            {
                Logger.Warn("ExternalResourceManager: Failed to delete robot file '" + localPath + "'");
            }

            Logger.Internal($"Removed robot <i>{robotName}</i> from cache");
            resourceFiles.RobotDescriptions.Remove(robotName);
            WriteResourceFile();
        }

        [ContractAnnotation("=> false, resource:null; => true, resource:notnull")]
        public bool TryGet([NotNull] Uri uri, out Info<GameObject> resource,
            [CanBeNull] IExternalServiceProvider provider)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            Debug.Log("ExternalResourceManager: Requesting " + uri);

            string uriPath = Uri.UnescapeDataString(uri.AbsolutePath);
            string fileType = Path.GetExtension(uriPath).ToUpperInvariant();

            if (fileType == ".SDF" || fileType == ".WORLD")
            {
                resource = TryGetScene(uri, provider);
            }
            else
            {
                resource = TryGetModel(uri, provider);
            }

            if (resource is null)
            {
                Debug.Log("ExternalResourceManager: Resource is null!");
            }

            return resource != null;
        }

        Info<GameObject> TryGetModel([NotNull] Uri uri, [CanBeNull] IExternalServiceProvider provider)
        {
            if (loadedModels.TryGetValue(uri, out Info<GameObject> resource))
            {
                return resource;
            }

            if (resourceFiles.Models.TryGetValue(uri, out string localPath))
            {
                if (File.Exists($"{Settings.ResourcesPath}/{localPath}"))
                {
                    return LoadLocalModel(uri, localPath, provider);
                }

                Debug.LogWarningFormat(StrMissingFileRemoving, localPath);
                resourceFiles.Models.Remove(uri);
                WriteResourceFile();
            }

            GetModelResource msg = new GetModelResource
            {
                Request =
                {
                    Uri = uri.ToString()
                }
            };

            if (provider != null &&
                provider.CallService(ModelServiceName, msg) &&
                msg.Response.Success)
            {
                return ProcessModelResponse(uri, msg.Response, provider);
            }

            if (!string.IsNullOrWhiteSpace(msg.Response.Message))
            {
                Debug.LogWarningFormat(StrServiceFailedWithMessage, msg.Response.Message);
            }
            else
            {
                Debug.Log(StrCallServiceFailed);
            }

            return null;
        }

        Info<GameObject> TryGetScene([NotNull] Uri uri, [CanBeNull] IExternalServiceProvider provider)
        {
            if (loadedScenes.TryGetValue(uri, out Info<GameObject> resource))
            {
                return resource;
            }

            if (resourceFiles.Scenes.TryGetValue(uri, out string localPath))
            {
                if (File.Exists($"{Settings.ResourcesPath}/{localPath}"))
                {
                    return LoadLocalScene(uri, localPath, provider);
                }

                Debug.LogWarningFormat(StrMissingFileRemoving, localPath);
                resourceFiles.Scenes.Remove(uri);
                WriteResourceFile();
            }

            GetSdf msg = new GetSdf
            {
                Request =
                {
                    Uri = uri.ToString()
                }
            };

            if (provider != null &&
                provider.CallService(SceneServiceName, msg) &&
                msg.Response.Success)
            {
                return ProcessSceneResponse(uri, msg.Response, provider);
            }

            if (!string.IsNullOrWhiteSpace(msg.Response.Message))
            {
                Debug.LogWarningFormat(StrServiceFailedWithMessage, msg.Response.Message);
            }
            else
            {
                Debug.Log(StrCallServiceFailed);
            }

            return null;
        }

        [ContractAnnotation("=> false, resource:null; => true, resource:notnull")]
        public bool TryGet([NotNull] Uri uri, out Info<Texture2D> resource,
            [CanBeNull] IExternalServiceProvider provider)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (loadedTextures.TryGetValue(uri, out resource))
            {
                return true;
            }

            if (resourceFiles.Textures.TryGetValue(uri, out string localPath))
            {
                if (File.Exists($"{Settings.ResourcesPath}/{localPath}"))
                {
                    resource = LoadLocalTexture(uri, localPath);
                    return resource != null;
                }

                Debug.LogWarningFormat(StrMissingFileRemoving, localPath);
                resourceFiles.Textures.Remove(uri);
                WriteResourceFile();
            }

            GetModelTexture msg = new GetModelTexture()
            {
                Request =
                {
                    Uri = uri.ToString()
                }
            };

            if (provider != null &&
                provider.CallService(TextureServiceName, msg) &&
                msg.Response.Success)
            {
                resource = ProcessTextureResponse(uri, msg.Response);
                return resource != null;
            }

            if (!string.IsNullOrWhiteSpace(msg.Response.Message))
            {
                Debug.LogWarningFormat(StrServiceFailedWithMessage, msg.Response.Message);
            }
            else
            {
                Debug.Log(StrCallServiceFailed);
            }

            return false;
        }

        [CanBeNull]
        Info<GameObject> LoadLocalModel([NotNull] Uri uri, [NotNull] string localPath,
            [CanBeNull] IExternalServiceProvider provider)
        {
            byte[] buffer;

            try
            {
                buffer = File.ReadAllBytes($"{Settings.ResourcesPath}/{localPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat(StrResourceFailedWithError, uri, e);
                return null;
            }

            Model msg = Msgs.Buffer.Deserialize(modelGenerator, buffer, buffer.Length);
            GameObject obj = CreateModelObject(uri, msg, provider);

            Info<GameObject> resource = new Info<GameObject>(uri.ToString(), obj);
            loadedModels[uri] = resource;

            return resource;
        }

        [CanBeNull]
        Info<Texture2D> LoadLocalTexture([NotNull] Uri uri, [NotNull] string localPath)
        {
            byte[] buffer;

            try
            {
                buffer = File.ReadAllBytes($"{Settings.ResourcesPath}/{localPath}");
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return null;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
            texture.LoadImage(buffer);
            if (!UnityEngine.Application.isEditor)
            {
                texture.Compress(true);
            }

            texture.name = uri.ToString();

            Info<Texture2D> resource = new Info<Texture2D>(uri.ToString(), texture);
            loadedTextures[uri] = resource;

            return resource;
        }

        [CanBeNull]
        Info<GameObject> LoadLocalScene([NotNull] Uri uri, [NotNull] string localPath,
            [CanBeNull] IExternalServiceProvider provider)
        {
            byte[] buffer;

            try
            {
                buffer = File.ReadAllBytes($"{Settings.ResourcesPath}/{localPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat(StrResourceFailedWithError, uri, e);
                return null;
            }

            Scene msg = Msgs.Buffer.Deserialize(sceneGenerator, buffer, buffer.Length);
            GameObject obj = CreateSceneNode(msg, provider);

            Info<GameObject> resource = new Info<GameObject>(uri.ToString(), obj);
            loadedScenes[uri] = resource;

            return resource;
        }

        [CanBeNull]
        Info<GameObject> ProcessModelResponse([NotNull] Uri uri, [NotNull] GetModelResourceResponse msg,
            [CanBeNull] IExternalServiceProvider provider)
        {
            try
            {
                GameObject obj = CreateModelObject(uri, msg.Model, provider);

                Info<GameObject> info = new Info<GameObject>(uri.ToString(), obj);
                loadedModels[uri] = info;

                string localPath = SanitizeForFilename(uri.ToString());

                byte[] buffer = new byte[msg.Model.RosMessageLength];
                Msgs.Buffer.Serialize(msg.Model, buffer);
                File.WriteAllBytes($"{Settings.ResourcesPath}/{localPath}", buffer);
                Debug.Log($"Saving to {Settings.ResourcesPath}/{localPath}");
                Logger.Internal($"Added external model <i>{uri}</i>");

                resourceFiles.Models[uri] = localPath;
                WriteResourceFile();

                return info;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        [CanBeNull]
        Info<Texture2D> ProcessTextureResponse([NotNull] Uri uri, [NotNull] GetModelTextureResponse msg)
        {
            try
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                texture.LoadImage(msg.Image.Data);
                texture.name = uri.ToString();

                Info<Texture2D> info = new Info<Texture2D>(uri.ToString(), texture);
                loadedTextures[uri] = info;

                string localPath = SanitizeForFilename(uri.ToString());

                byte[] buffer = msg.Image.Data;
                File.WriteAllBytes($"{Settings.ResourcesPath}/{localPath}", buffer);
                Debug.Log($"Saving to {Settings.ResourcesPath}/{localPath}");
                Logger.Internal($"Added external texture <i>{uri}</i>");

                resourceFiles.Textures[uri] = localPath;
                WriteResourceFile();

                return info;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        [CanBeNull]
        Info<GameObject> ProcessSceneResponse([NotNull] Uri uri, [NotNull] GetSdfResponse msg,
            [CanBeNull] IExternalServiceProvider provider)
        {
            try
            {
                GameObject node = CreateSceneNode(msg.Scene, provider);

                Info<GameObject> info = new Info<GameObject>(uri.ToString(), node);

                loadedScenes[uri] = info;

                string localPath = SanitizeForFilename(uri.ToString());

                byte[] buffer = new byte[msg.Scene.RosMessageLength];
                Msgs.Buffer.Serialize(msg.Scene, buffer);
                File.WriteAllBytes($"{Settings.ResourcesPath}/{localPath}", buffer);
                Debug.Log($"Saving to {Settings.ResourcesPath}/{localPath}");
                Logger.Internal($"Added external scene <i>{uri}</i>");

                resourceFiles.Scenes[uri] = localPath;
                WriteResourceFile();

                return info;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return null;
            }
        }

        [NotNull]
        GameObject CreateModelObject([NotNull] Uri uri, [NotNull] Model msg,
            [CanBeNull] IExternalServiceProvider provider)
        {
            GameObject model = SceneModel.Create(uri, msg, provider).gameObject;
            if (Node != null)
            {
                model.transform.SetParent(Node.transform, false);
            }

            return model;
        }

        [NotNull]
        GameObject CreateSceneNode([NotNull] Scene scene, [CanBeNull] IExternalServiceProvider provider)
        {
            GameObject node = new GameObject(scene.Name);
            if (Node != null)
            {
                node.transform.SetParent(Node?.transform, false);
            }

            Debug.Log(scene.ToJsonString());

            foreach (Include include in scene.Includes)
            {
                GameObject child = new GameObject("Include");

                Matrix4x4 m = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                {
                    m[i] = include.Pose.M[i];
                }

                child.transform.SetParent(node.transform, false);
                child.transform.localRotation = m.rotation.Ros2Unity();
                child.transform.localPosition = ((UnityEngine.Vector3) m.GetColumn(3)).Ros2Unity();
                child.transform.localScale = m.lossyScale;

                Uri includeUri = new Uri(include.Uri);
                if (!Resource.TryGetResource(includeUri, out Info<GameObject> includeResource, provider))
                {
                    Debug.Log("ExternalResourceManager: Failed to retrieve resource '" + includeUri + "'");
                    continue;
                }

                includeResource.Instantiate(child.transform);
            }

            return node;
        }
    }
}