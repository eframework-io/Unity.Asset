// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Preferences 提供了运行时的首选项管理，用于控制运行模式、调试选项和资源路径等配置项。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 运行模式配置：支持 AssetBundle 和 Resources 模式切换
        /// - 调试选项管理：支持调试模式和模拟模式的切换
        /// - 资源路径配置：支持配置内置、本地和远端资源路径
        /// - 可视化配置界面：在 Unity 编辑器中提供直观的设置面板
        ///
        /// 使用手册
        /// | 配置项 | 配置键 | 默认值 | 功能说明 |
        /// |--------|--------|--------|----------|
        /// | Bundle 模式 | `Asset/BundleMode` | `true` | 控制是否启用 AssetBundle 模式，启用后将从打包的资源文件加载资源 |
        /// | 引用计数模式 | `Asset/ReferMode` | `true` | 控制是否启用引用计数模式，启用后会自动跟踪资源引用，确保资源正确释放 |
        /// | 调试模式 | `Asset/DebugMode` | `false` | 控制是否启用调试模式，启用后会输出详细的资源加载和释放日志 |
        /// | 模拟模式 | `Asset/SimulateMode@Editor` | `false` | 控制是否启用模拟模式，仅在编辑器中可用，模拟 AssetBundle 的资源加载行为 |
        /// | 文件偏移 | `Asset/OffsetFactor` | `4` | 设置资源包的头部偏移算子，用于进行简单的 AssetBundle 资源加密 |
        /// | 内置资源路径 | `Asset/AssetUri` | `Patch@Assets.zip` | 设置资源包的内置路径，用于打包时的处理 |
        /// | 本地资源路径 | `Asset/LocalUri` | `Assets` | 设置资源包的本地路径，用于运行时的加载 |
        /// | 远端资源路径 | `Asset/RemoteUri` | `Builds/Patch/${Environment.Author}/${Environment.Version}/${Environment.Platform}/Assets` | 设置资源包的远端路径，用于运行时的下载 |
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Preferences : ScriptableObject, XPrefs.IEditor
        {
            /// <summary>
            /// BundleMode 是 Bundle 模式开关的配置键。
            /// 启用后将从打包的资源文件加载资源，否则使用Resources加载。
            /// </summary>
            public const string BundleMode = "XAsset/BundleMode";

            /// <summary>
            /// BundleModeDefault 是 Bundle 模式的默认值，默认开启以支持资源打包加载。
            /// </summary>
            public const bool BundleModeDefault = true;

            /// <summary>
            /// ReferMode 是引用计数模式的配置键。
            /// 启用后会自动跟踪资源引用，确保资源正确释放。
            /// </summary>
            public const string ReferMode = "XAsset/ReferMode";

            /// <summary>
            /// ReferModeDefault 是引用计数模式的默认值，默认开启以防止资源泄漏
            /// </summary>
            public const bool ReferModeDefault = true;

            /// <summary>
            /// DebugMode 是调试模式的配置键。
            /// 启用后会输出详细的资源加载和释放日志。
            /// </summary>
            public const string DebugMode = "XAsset/DebugMode";

            /// <summary>
            /// SimulateMode 是编辑器模拟模式的配置键。
            /// 在编辑器中可以模拟Bundle模式的资源加载，方便测试。
            /// </summary>
            public const string SimulateMode = "XAsset/SimulateMode@Editor";

            /// <summary>
            /// OffsetFactor 是 Bundle 文件偏移的配置键。
            /// </summary>
            public const string OffsetFactor = "XAsset/OffsetFactor";

            /// <summary>
            /// OffsetFactorDefault 是 Bundle 文件偏移的默认值。
            /// </summary>
            public const int OffsetFactorDefault = 4;

            /// <summary>
            /// AssetUri 是资源包文件名的配置键。
            /// 用于指定打包后的资源文件名称。
            /// </summary>
            public const string AssetUri = "XAsset/AssetUri";

            /// <summary>
            /// AssetUriDefault 是资源包的默认文件名。
            /// </summary>
            public const string AssetUriDefault = "Patch@Assets.zip";

            /// <summary>
            /// LocalUri 是本地资源路径的配置键，指定资源文件在本地存储的相对路径。
            /// </summary>
            public const string LocalUri = "XAsset/LocalUri";

            /// <summary>
            /// LocalUriDefault 是本地资源的默认存储路径。
            /// </summary>
            public const string LocalUriDefault = "Assets";

            /// <summary>
            /// RemoteUri 是远程资源地址的配置键，用于指定资源更新的远程服务器地址。
            /// </summary>
            public const string RemoteUri = "XAsset/RemoteUri";

            /// <summary>
            /// RemoteUriDefault 是远程资源的默认下载地址，支持变量求值。
            /// </summary>
            public const string RemoteUriDefault = "Builds/Patch/${Environment.Author}/${Environment.Version}/${Environment.Platform}/Assets";

            public virtual string Section => "XAsset";

            public virtual string Tooltip => "Preferences of XAsset.";

            public virtual bool Foldable => true;

            public virtual int Priority => 100;

            [SerializeField] protected bool foldout;

            public virtual void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement, XPrefs.IBase context) { }

            public virtual void OnVisualize(string searchContext, XPrefs.IBase context)
            {
#if UNITY_EDITOR
                var bundleMode = context.GetBool(BundleMode, BundleModeDefault);

                UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Bundle", "Switch to AssetBundle/Resources Mode."), GUILayout.Width(60));
                bundleMode = UnityEditor.EditorGUILayout.Toggle(bundleMode);
                context.Set(BundleMode, bundleMode);

                var ocolor = GUI.color;
                if (!bundleMode) GUI.color = Color.gray;

                GUILayout.Label(new GUIContent("Refer", "Auto Manage References."), GUILayout.Width(60));
                var referMode = UnityEditor.EditorGUILayout.Toggle(context.GetBool(ReferMode, ReferModeDefault));
                if (bundleMode) context.Set(ReferMode, referMode);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Debug", "Switch to Debug/Release Mode."), GUILayout.Width(60));
                var debugMode = UnityEditor.EditorGUILayout.Toggle(context.GetBool(DebugMode));
                if (bundleMode) context.Set(DebugMode, debugMode);

                GUILayout.Label(new GUIContent("Simulate", "Simulate to Load AssetBundle."), GUILayout.Width(60));
                var simulateMode = UnityEditor.EditorGUILayout.Toggle(context.GetBool(SimulateMode));
                if (bundleMode) context.Set(SimulateMode, simulateMode);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Offset", "Asset Bundle Offset Factor."), GUILayout.Width(60));
                var offsetFactor = UnityEditor.EditorGUILayout.IntField(context.GetInt(OffsetFactor, OffsetFactorDefault));
                if (bundleMode) context.Set(OffsetFactor, offsetFactor);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Asset", "Asset Uri of Assets."), GUILayout.Width(60));
                var assetFile = UnityEditor.EditorGUILayout.TextField("", context.GetString(AssetUri, AssetUriDefault));
                if (bundleMode) context.Set(AssetUri, assetFile);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Local", "Local Uri of Assets."), GUILayout.Width(60));
                var localPath = UnityEditor.EditorGUILayout.TextField("", context.GetString(LocalUri, LocalUriDefault));
                if (bundleMode) context.Set(LocalUri, localPath);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Remote", "Remote Uri of Assets."), GUILayout.Width(60));
                var remoteUri = UnityEditor.EditorGUILayout.TextField("", context.GetString(RemoteUri, RemoteUriDefault));
                if (bundleMode) context.Set(RemoteUri, remoteUri);
                UnityEditor.EditorGUILayout.EndHorizontal();

                GUI.color = ocolor;
                UnityEditor.EditorGUILayout.EndVertical();
#endif
            }

            public virtual void OnDeactivate(XPrefs.IBase context) { }

            public virtual bool OnSave(XPrefs.IBase context) { return true; }

            public virtual bool OnApply(XPrefs.IBase context) { return true; }

            public virtual bool OnBuild(XPrefs.IBase context) { return true; }
        }
    }
}
