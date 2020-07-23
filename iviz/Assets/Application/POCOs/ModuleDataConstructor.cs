﻿using Iviz.Controllers;
using Iviz.Resources;

namespace Iviz.App
{
    public sealed class ModuleDataConstructor
    {
        public Resource.Module Module { get; }
        public ModuleListPanel ModuleList { get; }
        public string Topic { get; }
        public string Type { get; }
        public IConfiguration Configuration { get; }

        public T GetConfiguration<T>() where T : class, IConfiguration => Configuration as T;

        public ModuleDataConstructor(Resource.Module module,
                                     ModuleListPanel moduleList,
                                     string topic,
                                     string type,
                                     IConfiguration configuration)
        {
            Module = module;
            ModuleList = moduleList;
            Topic = topic;
            Type = type;
            Configuration = configuration;
        }
    }
}
