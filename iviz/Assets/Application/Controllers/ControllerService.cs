using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Iviz.App;
using Iviz.Core;
using Iviz.Msgs.IvizMsgs;
using Iviz.Msgs.RosgraphMsgs;
using Iviz.Resources;
using Iviz.Ros;
using Iviz.Roslib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Logger = Iviz.Core.Logger;

namespace Iviz.Controllers
{
    public sealed class ControllerService
    {
        const int DefaultTimeoutInMs = 4500;

        [NotNull] static RoslibConnection Connection => ConnectionManager.Connection;
        [NotNull] static IReadOnlyList<ModuleData> ModuleDatas => ModuleListPanel.Instance.ModuleDatas;

        static readonly (Resource.ModuleType module, string name)[] ModuleNames =
            typeof(Resource.ModuleType).GetEnumValues()
                .Cast<Resource.ModuleType>()
                .Select(module => (module, module.ToString()))
                .ToArray();


        public ControllerService()
        {
            Connection.AdvertiseService<AddModule>("add_module", AddModuleCallback);
            Connection.AdvertiseService<AddModuleFromTopic>("add_module_from_topic", AddModuleFromTopicCallback);
            Connection.AdvertiseService<UpdateModule>("update_module", UpdateModuleCallback);
            Connection.AdvertiseService<GetModules>("get_modules", GetModulesCallback);
            Connection.AdvertiseService<SetFixedFrame>("set_fixed_frame", SetFixedFrameCallback);
        }

        static void AddModuleCallback([NotNull] AddModule srv)
        {
            var (id, success, message) = TryAddModule(srv.Request.ModuleType, srv.Request.Id);
            srv.Response.Success = success;
            srv.Response.Message = message ?? "";
            srv.Response.Id = id ?? "";
        }

        static Resource.ModuleType ModuleTypeFromString(string moduleName)
        {
            return ModuleNames.FirstOrDefault(tuple => tuple.name == moduleName).module;
        }

        static (string id, bool success, string message) TryAddModule([NotNull] string moduleTypeStr,
            [NotNull] string requestedId)
        {
            (string id, bool success, string message) result = default;

            if (string.IsNullOrWhiteSpace(moduleTypeStr))
            {
                result.message = "EE Invalid module type";
                return result;
            }

            Resource.ModuleType moduleType = ModuleTypeFromString(moduleTypeStr);

            if (moduleType == Resource.ModuleType.Invalid)
            {
                result.message = "EE Invalid module type";
                return result;
            }

            if (moduleType != Resource.ModuleType.Grid &&
                moduleType != Resource.ModuleType.DepthCloud &&
                moduleType != Resource.ModuleType.AugmentedReality &&
                moduleType != Resource.ModuleType.Joystick &&
                moduleType != Resource.ModuleType.Robot)
            {
                result.message = "EE Cannot create module of that type, use AddModuleFromTopic instead";
                return result;
            }

            ModuleData moduleData;
            if (requestedId.Length != 0 &&
                (moduleData = ModuleDatas.FirstOrDefault(data => data.Configuration.Id == requestedId)) != null)
            {
                if (moduleData.ModuleType != moduleType)
                {
                    result.message =
                        $"EE Another module of the same id already exists, but it has type {moduleData.ModuleType}";
                }
                else
                {
                    result.success = true;
                    result.id = requestedId;
                    result.message = "** Module already exists";
                }

                return result;
            }


            SemaphoreSlim signal = new SemaphoreSlim(0, 1);

            GameThread.Post(() =>
            {
                try
                {
                    Logger.External("Creating module of type " + moduleType);
                    var newModuleData = ModuleListPanel.Instance.CreateModule(moduleType,
                        requestedId: requestedId.Length != 0 ? requestedId : null);
                    result.id = newModuleData.Configuration.Id;
                    result.success = true;
                    Logger.External("Done!");
                }
                catch (Exception e)
                {
                    result.message = $"EE An exception was raised: {e.Message}";
                }
                finally
                {
                    signal.Release();
                }
            });
            return signal.Wait(DefaultTimeoutInMs) ? result : ("", false, "EE Request timed out!");
        }

        static void AddModuleFromTopicCallback([NotNull] AddModuleFromTopic srv)
        {
            var (id, success, message) = TryAddModuleFromTopic(srv.Request.Topic, srv.Request.Id);
            srv.Response.Success = success;
            srv.Response.Message = message ?? "";
            srv.Response.Id = id ?? "";
        }

        static (string id, bool success, string message) TryAddModuleFromTopic([NotNull] string topic,
            [NotNull] string requestedId)
        {
            (string id, bool success, string message) result = default;
            if (string.IsNullOrWhiteSpace(topic))
            {
                result.message = "EE Invalid topic name";
                return result;
            }

            var data = ModuleDatas.FirstOrDefault(module => module.Topic == topic);
            if (data != null)
            {
                result.message = requestedId == data.Configuration.Id
                    ? "** Module already exists"
                    : "WW A module with that topic but different id already exists";
                result.id = data.Configuration.Id;
                result.success = true;
                return result;
            }

            if (requestedId.Length != 0 && ModuleDatas.Any(module => module.Configuration.Id == requestedId))
            {
                result.message = "EE There is already another module with that id";
                return result;
            }

            ReadOnlyCollection<BriefTopicInfo> topics = Connection.GetSystemTopicTypes(RequestType.CachedOnly);

            string type = topics.FirstOrDefault(topicInfo => topicInfo.Topic == topic)?.Type;
            if (type == null)
            {
                topics = Connection.GetSystemTopicTypes(RequestType.WaitForRequest);
                type = topics.FirstOrDefault(topicInfo => topicInfo.Topic == topic)?.Type;
            }

            if (type == null)
            {
                return ("", false, $"EE Failed to find topic '{topic}'");
            }

            if (!Resource.ResourceByRosMessageType.TryGetValue(type, out Resource.ModuleType resource))
            {
                result.message = $"EE Type '{type}' is unsupported";
            }

            SemaphoreSlim signal = new SemaphoreSlim(0, 1);
            GameThread.Post(() =>
            {
                try
                {
                    Logger.Debug(Time.time + ": Adding topic " + topic);
                    result.id = ModuleListPanel.Instance.CreateModule(resource, topic, type,
                        requestedId: requestedId.Length != 0 ? requestedId : null).Configuration.Id;
                    result.success = true;
                    Logger.Debug(Time.time + ": Done!");
                }
                catch (Exception e)
                {
                    result.message = $"EE An exception was raised: {e.Message}";
                    Logger.Warn(e);
                }
                finally
                {
                    signal.Release();
                }
            });

            return signal.Wait(DefaultTimeoutInMs) ? result : ("", false, "EE Request timed out!");
        }

        static void UpdateModuleCallback([NotNull] UpdateModule srv)
        {
            var (success, message) = TryUpdateModule(srv.Request.Id, srv.Request.Fields, srv.Request.Config);
            srv.Response.Success = success;
            srv.Response.Message = message ?? "";
        }

        static (bool success, string message) TryUpdateModule([NotNull] string id, string[] fields, string config)
        {
            (bool success, string message) result = default;
            if (string.IsNullOrWhiteSpace(id))
            {
                result.message = "EE Empty configuration id!";
                return result;
            }

            if (string.IsNullOrWhiteSpace(config))
            {
                result.message = "EE Empty configuration text!";
                return result;
            }

            SemaphoreSlim signal = new SemaphoreSlim(0, 1);
            GameThread.Post(() =>
            {
                Logger.Debug(Time.time + ": Updating module!");

                try
                {
                    ModuleData module = ModuleDatas.FirstOrDefault(data => data.Configuration.Id == id);
                    if (module == null)
                    {
                        result.success = false;
                        result.message = "EE There is no module with that id";
                        return;
                    }

                    module.UpdateConfiguration(config, fields);
                    result.success = true;
                }
                catch (JsonException e)
                {
                    result.success = false;
                    result.message = $"EE Error parsing JSON config: {e.Message}";
                    Logger.External("Error:", e);
                }
                catch (Exception e)
                {
                    result.success = false;
                    result.message = $"EE An exception was raised: {e.Message}";
                    Logger.External("Error:", e);
                }
                finally
                {
                    Logger.Debug(Time.time + ": Done!");
                    signal.Release();
                }
            });
            return signal.Wait(DefaultTimeoutInMs) ? result : (false, "EE Request timed out!");
        }

        static void GetModulesCallback([NotNull] GetModules srv)
        {
            string[] result = GetModules();
            srv.Response.Configs = result;
        }

        [NotNull]
        static string[] GetModules()
        {
            SemaphoreSlim signal = new SemaphoreSlim(0, 1);
            string[] result = Array.Empty<string>();
            GameThread.Post(() =>
            {
                try
                {
                    IConfiguration[] configurations = ModuleDatas.Select(data => data.Configuration).ToArray();
                    result = configurations.Select(JsonConvert.SerializeObject).ToArray();
                }
                catch (JsonException e)
                {
                    Logger.External($"ControllerService: Unexpected JSON exception in GetModules", e);
                }
                catch (Exception e)
                {
                    Logger.External($"ControllerService: Unexpected exception in GetModules", e);
                }
                finally
                {
                    signal.Release();
                }
            });
            if (!signal.Wait(DefaultTimeoutInMs))
            {
                Logger.External("Timeout in GetModules", LogLevel.Error);
            }

            return result;
        }

        static void SetFixedFrameCallback([NotNull] SetFixedFrame srv)
        {
            (bool success, string message) = TrySetFixedFrame(srv.Request.Id);
            srv.Response.Success = success;
            srv.Response.Message = message ?? "";
        }

        static (bool success, string message) TrySetFixedFrame(string id)
        {
            (bool success, string message) result = default;

            SemaphoreSlim signal = new SemaphoreSlim(0, 1);
            GameThread.Post(() =>
            {
                try
                {
                    TfListener.FixedFrameId = id;
                }
                finally
                {
                    signal.Release();
                }
            });

            if (!signal.Wait(DefaultTimeoutInMs))
            {
                Logger.External("ControllerService: Unexpected timeout in TrySetFixedFrame", LogLevel.Error);
                return result;
            }

            return (true, "");
        }
    }
}