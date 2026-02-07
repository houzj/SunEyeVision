using System;
using System.Collections.Generic;
using System.Reflection;
using SunEyeVision.Models;

namespace SunEyeVision.PluginSystem.Base.Interfaces
{
    /// <summary>
    /// è§è§æä»¶æ¥å£
    /// </summary>
    public interface IVisionPlugin
    {
        /// <summary>
        /// æä»¶åç§°
        /// </summary>
        string Name { get; }

        /// <summary>
        /// æä»¶çæ¬
        /// </summary>
        string Version { get; }

        /// <summary>
        /// æä»¶ä½è?
        /// </summary>
        string Author { get; }

        /// <summary>
        /// æä»¶æè¿°
        /// </summary>
        string Description { get; }

        /// <summary>
        /// æä»¶ID
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// æä»¶ä¾èµ
        /// </summary>
        List<string> Dependencies { get; }

        /// <summary>
        /// æä»¶å¾æ 
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// åå§åæä»?
        /// </summary>
        void Initialize();

        /// <summary>
        /// å¸è½½æä»¶
        /// </summary>
        void Unload();

        /// <summary>
        /// è·åç®æ³èç¹åè¡¨
        /// </summary>
        /// <returns>ç®æ³èç¹ç±»ååè¡¨</returns>
        List<Type> GetAlgorithmNodes();

        /// <summary>
        /// æä»¶æ¯å¦å·²å è½?
        /// </summary>
        bool IsLoaded { get; }
    }
}
