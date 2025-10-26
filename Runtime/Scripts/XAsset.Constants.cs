// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Constants 提供了一些常量定义和运行时环境控制，包括运行配置和 Bundle 名称生成、偏移计算等功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 运行配置：提供 Bundle 模式、调试模式和资源路径等配置
        /// - 名称生成：提供资源路径的处理并生成归一化的 Bundle 名称
        /// - 偏移计算：根据首选项中配置的 Bundle 偏移因子计算文件的偏移
        ///
        /// 使用手册
        /// 1. 运行配置
        ///    - 配置项：Bundle 模式
        ///      配置说明：控制是否启用 Bundle 模式
        ///      依赖条件：编辑器模式下需要启用模拟模式
        ///      访问方式：
        ///      <code>
        ///      var isBundleMode = XAsset.Constants.BundleMode;
        ///      </code>
        ///
        ///    - 配置项：引用计数模式
        ///      配置说明：控制是否启用引用计数模式
        ///      依赖条件：仅在 Bundle 模式下可用
        ///      访问方式：
        ///      <code>
        ///      var isReferMode = XAsset.Constants.ReferMode;
        ///      </code>
        ///
        ///    - 配置项：调试模式
        ///      配置说明：控制是否启用调试模式
        ///      依赖条件：仅在 Bundle 模式下可用
        ///      访问方式：
        ///      <code>
        ///      var isDebugMode = XAsset.Constants.DebugMode;
        ///      </code>
        ///
        ///    - 配置项：本地路径
        ///      配置说明：获取资源文件的本地存储路径
        ///      访问方式：
        ///      <code>
        ///      var localPath = XAsset.Constants.LocalPath;
        ///      </code>
        ///
        /// 2. 名称生成
        ///    - 配置项：默认扩展名
        ///      <code>
        ///      XAsset.Constants.Extension = ".bundle";
        ///      </code>
        ///
        ///    - 配置项：生成规则
        ///      生成资源名称的规则如下：
        ///      1. 将资源路径的 `Assets/` 剔除
        ///      2. 获取资源的拓展名，若为空或 `.unity` 则不处理，否则剔除拓展名
        ///      3. 归一化路径并转为全小写，避免路径的大小写问题
        ///      4. 对归一化的路径进行 `MD5` 求值并追加 `XAsset.Constants.Extension` 扩展名
        ///
        ///      使用示例：
        ///      <code>
        ///      var assetPath = "Resources/Example/Test.prefab";
        ///      var bundleName = XAsset.Constants.GetName(assetPath);
        ///      </code>
        ///
        /// 3. 偏移计算
        ///    - 配置项：偏移因子
        ///      配置说明：控制 Bundle 文件偏移的计算因子
        ///      依赖条件：通过首选项配置 `XPrefs.GetInt(Preferences.OffsetFactor)`
        ///      默认值：从 `Preferences.OffsetFactorDefault` 获取
        ///
        ///    - 配置项：计算方法
        ///      偏移计算的规则如下：
        ///      1. 如果 Bundle 名称为空，返回 0
        ///      2. 如果偏移因子小于等于 0，返回 0
        ///      3. 计算公式：`(bundleName.Length % offsetFactor + 1) * 28`
        ///
        ///      使用示例：
        ///      <code>
        ///      var bundleName = XAsset.Constants.GetName(assetPath);
        ///      var offset = XAsset.Constants.GetOffset(bundleName);
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Constants
        {
            #region 运行配置
            /// <summary>
            /// Manifest 表示 AssetBundleManifest 的默认文件名。
            /// </summary>
            public static string Manifest = "Assets";

            /// <summary>
            /// bBundleMode 是 Bundle 模式的初始化标记。
            /// </summary>
            internal static bool bBundleMode;

            /// <summary>
            /// bundleMode 是 Bundle 模式的当前状态。
            /// </summary>
            internal static bool bundleMode;

            /// <summary>
            /// BundleMode 获取资源系统当前是否运行在 Bundle 模式下。
            /// Bundle 模式下会从打包的资源文件中加载资源，而不是直接从 Assets 目录加载，这个设置会受到编辑器状态和模拟模式的影响。
            /// </summary>
            public static bool BundleMode
            {
                get
                {
                    if (!bBundleMode)
                    {
                        bBundleMode = true;
                        bundleMode = XPrefs.GetBool(Preferences.BundleMode, Preferences.BundleModeDefault) && (!Application.isEditor || XPrefs.GetBool(Preferences.SimulateMode));
                    }
                    return bundleMode;
                }
            }

            /// <summary>
            /// bReferMode 是引用模式的初始化标记。
            /// </summary>
            internal static bool bReferMode;

            /// <summary>
            /// referMode 是引用模式的当前状态。
            /// </summary>
            internal static bool referMode;

            /// <summary>
            /// ReferMode 获取是否启用了引用计数模式。
            /// 在引用计数模式下，系统会跟踪资源的使用情况，只有当资源不再被任何对象引用时才会被卸载，这个模式需要 Bundle 模式开启才能生效。
            /// </summary>
            public static bool ReferMode
            {
                get
                {
                    if (!bReferMode)
                    {
                        bReferMode = true;
                        referMode = BundleMode && XPrefs.GetBool(Preferences.ReferMode, Preferences.ReferModeDefault);
                    }
                    return referMode;
                }
            }

            /// <summary>
            /// bDebugMode 是调试模式的初始化标记。
            /// </summary>
            internal static bool bDebugMode;

            /// <summary>
            /// debugMode 是调试模式的当前状态。
            /// </summary>
            internal static bool debugMode;

            /// <summary>
            /// DebugMode 获取是否启用了调试模式。
            /// 调试模式下会输出详细的日志信息，帮助开发者追踪资源加载和卸载的过程，这个模式同样需要 Bundle 模式开启才能生效。
            /// </summary>
            public static bool DebugMode
            {
                get
                {
                    if (!bDebugMode)
                    {
                        bDebugMode = true;
                        debugMode = BundleMode && XPrefs.GetBool(Preferences.DebugMode);
                    }
                    return debugMode;
                }
            }

            /// <summary>
            /// bLocalPath 是本地路径的初始化标记。
            /// </summary>
            internal static bool bLocalPath;

            /// <summary>
            /// localPath 是缓存的本地路径。
            /// </summary>
            internal static string localPath;

            /// <summary>
            /// LocalPath 获取资源文件的本地存储路径。
            /// 这个路径用于存放下载或解压后的资源文件，路径格式会根据当前平台和配置自动调整。
            /// </summary>
            public static string LocalPath
            {
                get
                {
                    if (!bLocalPath)
                    {
                        bLocalPath = true;
                        localPath = XFile.PathJoin(XEnv.LocalPath, XPrefs.GetString(Preferences.LocalUri, Preferences.LocalUriDefault));
                    }
                    return localPath;
                }
            }
            #endregion

            #region 生成名称
            /// <summary>
            /// Extension 是资源包文件的默认扩展名，用于标识打包后的资源文件。
            /// </summary>
            public static string Extension = ".bundle";

            /// <summary>
            /// nameCache 是资源名称的缓存，用于提高重复名称生成的性能。
            /// </summary>
            internal static readonly Dictionary<string, string> nameCache = new();

            /// <summary>
            /// GetName 根据资源路径生成唯一的资源包名称。
            /// </summary>
            /// <param name="assetPath">需要转换的资源路径</param>
            /// <returns>转换后的资源名称，包括扩展名</returns>
            public static string GetName(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath)) return string.Empty;
                if (nameCache.TryGetValue(assetPath, out var bundleName)) return bundleName;
                else
                {
                    if (assetPath.StartsWith("Assets/")) assetPath = assetPath["Assets/".Length..];
                    var extension = Path.GetExtension(assetPath);
                    if (!string.IsNullOrEmpty(extension) && extension != ".unity") // 场景文件只能单独打包
                    {
                        assetPath = assetPath.Replace(extension, "");
                    }
                    bundleName = XFile.NormalizePath(assetPath).ToLower().MD5() + Extension;
                    nameCache[assetPath] = bundleName;
                    return bundleName;
                }
            }
            #endregion

            #region 计算偏移
            /// <summary>
            /// bOffsetFactor 是 offsetFactor 的初始化标记。
            /// </summary>
            internal static bool bOffsetFactor;

            /// <summary>
            /// offsetFactor 是 Bundle 文件偏移的缓存值。
            /// </summary>
            internal static int offsetFactor;

            /// <summary>
            /// GetOffset 根据首选项中配置的 Bundle 偏移因子计算文件的偏移。
            /// </summary>
            /// <param name="bundleName">需要计算文件偏移的 Bundle 名称</param>
            /// <returns>Bundle 文件偏移</returns>
            public static ulong GetOffset(string bundleName)
            {
                if (string.IsNullOrEmpty(bundleName)) return 0;
                if (!bOffsetFactor)
                {
                    bOffsetFactor = true;
                    offsetFactor = XPrefs.GetInt(Preferences.OffsetFactor, Preferences.OffsetFactorDefault);
                }
                if (offsetFactor <= 0) return 0;
                return (ulong)((bundleName.Length % offsetFactor + 1) * 28);
            }
            #endregion
        }
    }
}
