﻿using Iviz.Resources;
using System;

namespace Iviz.Controllers
{
    /// <summary>
    /// Common interface for the configuration classes of the controllers.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// GUID of the controller.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Module type of the controller. 
        /// </summary>
        Resource.ModuleType ModuleType { get; }
        
        /// <summary>
        /// Whether the controller is visible. 
        /// </summary>
        bool Visible { get; set; }
    }
}
