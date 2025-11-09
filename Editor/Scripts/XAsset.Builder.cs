// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset.Editor
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Builder 提供了资源的构建工作流，支持资源的依赖分析及打包功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 首选项配置：提供首选项配置以自定义构建流程
        /// - 自动化流程：提供资源包构建任务的自动化执行
        /// 
        /// 使用手册
        /// 1. 首选项配置
        /// 
        /// 配置项说明：
        /// - 输出路径：`Asset/Builder/Output@Editor`，默认值为 `Builds/Patch/${Environment.Platform}/Assets`
        /// - 包含路径：`Asset/Builder/Include@Editor`，默认值为 `["Assets/Resources/Bundle", "Assets/Scenes/**/*.unity"]`
        /// - 排除路径：`Asset/Builder/Exclude@Editor`，默认值为 `[]`
        /// - 暂存路径：`Asset/Builder/Stash@Editor`，默认值为 `["Assets/Resources/Bundle"]`
        /// - 合并材质：`Asset/Builder/Merge/Material@Editor`，默认值为 `true`
        /// - 合并单包：`Asset/Builder/Merge/Single@Editor`，默认值为 `false`
        /// - 拷贝资源：`Asset/Builder/Streaming/Assets@Editor`，默认值为 `true`
        /// 
        /// 关联配置项：`Asset/OffsetFactor`、`Asset/AssetUri`、`Asset/LocalUri`
        /// 
        /// 以上配置项均可在 `Tools/EFramework/Preferences/Asset/Builder` 首选项编辑器中进行可视化配置。
        /// 
        /// 2. 自动化流程
        /// 
        /// 2.1 构建流程
        /// - 分析依赖 --> 打包资源 --> 生成清单
        /// 
        /// 2.2 构建准备
        /// 
        /// 依赖分析
        /// - 依赖分析系统将资源类型分为可加载资源和原生依赖资源
        /// - 可加载资源资源一般位于 Resources 和 Scenes 目录中，通过 `Asset/Builder/Include@Editor` 选项进行设置，以单文件形式进行打包
        /// - 原生依赖资源一般位于 RawAssets 目录中，不可以加载，以文件夹形式进行打包
        /// - 可以通过设置 `Asset/Builder/Include@Editor` 选项排除 `Asset/Builder/Include@Editor` 中包含的文件/目录，支持通配符
        /// 
        /// 资源合并
        /// - 材质合并：可选择是否将材质合并到场景包中以完整收集 `Shader` 变体，注意：若某材质的依赖数为1，则默认进行材质合并
        /// - 单包合并：可选择是否将单一资源合并到主包中
        /// - 自定义合并：支持通过 AssetImporter 设置自定义打包规则
        /// 
        /// 2.3 构建产物
        /// 
        /// 在 `Asset/Builder/Output@Editor` 目录下会生成以下文件：
        /// - `*.bundle`：资源包文件，格式为 `file_md5.bundle`
        /// - `Manifest.db`：资源包清单，格式为 `名称|MD5|大小`
        /// 
        /// 构建产物会在内置构建事件 `XEditor.Event.Internal.OnPreprocessBuild` 触发时内置于安装包的资源目录下：
        /// 
        /// - 移动平台 (Android/iOS/..)
        ///   ```
        ///   <AssetPath>/
        ///   └── <AssetUri>  # 资源包压缩为 ZIP
        ///   ```
        /// 
        /// - 桌面平台 (Windows/macOS/..)
        ///   ```
        ///   <输出目录>_Data/
        ///   └── Local/
        ///       └── <LocalUri>  # 资源包直接部署
        ///   ```
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        [XEditor.Tasks.Worker(name: "Build Assets", group: "XAsset", priority: 101)]
        public class Builder : XEditor.Tasks.Worker,
            XEditor.Tasks.Panel.IOnGUI,
            XEditor.Event.Internal.OnPreprocessBuild,
            XEditor.Event.Internal.OnPostprocessBuild
        {
            /// <summary>
            /// Preferences 是构建的配置管理器，提供资源打包相关的配置项和界面化设置功能。
            /// 包含输出路径、资源包含/排除规则、资源暂存设置以及合并选项等配置。
            /// </summary>
            public class Preferences : Asset.XAsset.Preferences
            {
                /// <summary>
                /// Output 是输出路径配置键。
                /// </summary>
                public const string Output = "XAsset/Builder/Output@Editor";

                /// <summary>
                /// OutputDefault 是输出路径默认值。
                /// </summary>
                public const string OutputDefault = "Builds/Patch/${Environment.Platform}/Assets";

                /// <summary>
                /// Include 是资源路径配置键。
                /// </summary>
                public const string Include = "XAsset/Builder/Include@Editor";

                /// <summary>
                /// IncludeDefault 是资源路径默认值。
                /// </summary>
                public static readonly string[] IncludeDefault = new string[] { "Assets/Resources/Bundle", "Assets/Scenes/**/*.unity" };

                /// <summary>
                /// Exclude 是资源排除配置键。
                /// </summary>
                public const string Exclude = "XAsset/Builder/Exclude@Editor";

                /// <summary>
                /// Stash 是资源暂存配置键。
                /// </summary>
                public const string Stash = "XAsset/Builder/Stash@Editor";

                /// <summary>
                /// StashDefault 是资源暂存默认值。
                /// </summary>
                public static readonly string[] StashDefault = new string[] { "Assets/Resources/Bundle" };

                /// <summary>
                /// MergeMaterial 是材质合并配置键。
                /// </summary>
                public const string MergeMaterial = "XAsset/Builder/Merge/Material@Editor";

                /// <summary>
                /// MergeMaterialDefault 是材质合并默认值。
                /// </summary>
                public const bool MergeMaterialDefault = true;

                /// <summary>
                /// MergeSingle 是资源合并配置键。
                /// </summary>
                public const string MergeSingle = "XAsset/Builder/Merge/Single@Editor";

                /// <summary>
                /// MergeSingleDefault 是资源合并默认值。
                /// </summary>
                public const bool MergeSingleDefault = false;

                /// <summary>
                /// StreamingAssets 是资源拷贝配置键。
                /// </summary>
                public const string StreamingAssets = "XAsset/Builder/Streaming/Assets@Editor";

                /// <summary>
                /// StreamingAssets 是资源拷贝默认值。
                /// </summary>
                public const bool StreamingAssetsDefault = true;

                public override string Section => "XAsset";

                public override int Priority => 101;

                [SerializeField] internal string[] include;

                [SerializeField] internal string[] exclude;

                [SerializeField] internal string[] stash;

                [NonSerialized] SerializedObject serialized;

                public Preferences() { foldout = false; }

                public override void OnVisualize(string searchContext, XPrefs.IBase context)
                {
                    var taskPanel = searchContext == "Task Runner";
                    serialized ??= new SerializedObject(this);
                    serialized.Update();

                    var ocolor = GUI.color;
                    var bundleMode = context.GetBool(BundleMode, BundleModeDefault);
                    if (!taskPanel)
                    {
                        if (!bundleMode) GUI.color = Color.gray;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Builder", "Assets Builder Options."));
                    }
                    else foldout = true;
                    if (foldout && bundleMode)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Output", "Output Path of AssetBundle."), GUILayout.Width(60));
                        context.Set(Output, EditorGUILayout.TextField("", context.GetString(Output, OutputDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Options"), GUILayout.Width(60));

                        GUILayout.Label(new GUIContent("Merge Material", "Merge Material into Bundle for Collecting Shader Variants."), GUILayout.Width(90));
                        context.Set(MergeMaterial, EditorGUILayout.Toggle(context.GetBool(MergeMaterial, MergeMaterialDefault)));

                        GUILayout.Label(new GUIContent("Merge Single", "Merge Single Raw Bundle into Main Bundle."), GUILayout.Width(80));
                        context.Set(MergeSingle, EditorGUILayout.Toggle(context.GetBool(MergeSingle, MergeSingleDefault)));

                        GUILayout.Label(new GUIContent("Streaming Assets", "Copy Assets Patch into Streaming Assets."), GUILayout.Width(105));
                        context.Set(StreamingAssets, EditorGUILayout.Toggle(context.GetBool(StreamingAssets, StreamingAssetsDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        include = context.GetStrings(Include, IncludeDefault);
                        EditorGUILayout.PropertyField(serialized.FindProperty("include"), new GUIContent("Include"));
                        if (GUILayout.Button(new GUIContent("?", "Learn more about File Globbing"), GUILayout.Width(20))) Application.OpenURL("https://learn.microsoft.com/zh-cn/dotnet/core/extensions/file-globbing");
                        if (serialized.ApplyModifiedProperties()) context.Set(Include, include);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        exclude = context.GetStrings(Exclude, Array.Empty<string>());
                        EditorGUILayout.PropertyField(serialized.FindProperty("exclude"), new GUIContent("Exclude"));
                        if (GUILayout.Button(new GUIContent("?", "Learn more about File Globbing"), GUILayout.Width(20))) Application.OpenURL("https://learn.microsoft.com/zh-cn/dotnet/core/extensions/file-globbing");
                        if (serialized.ApplyModifiedProperties()) context.Set(Exclude, exclude);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        stash = context.GetStrings(Stash, StashDefault);
                        EditorGUILayout.PropertyField(serialized.FindProperty("stash"), new GUIContent("Stash"));
                        if (serialized.ApplyModifiedProperties()) context.Set(Stash, stash);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                    }
                    else if (foldout && !bundleMode) EditorGUILayout.HelpBox("Bundle Mode is Disabled.", MessageType.None);
                    GUI.color = ocolor;

                    if (!taskPanel) EditorGUILayout.EndVertical();
                }
            }

            internal static string stashFile { get => XFile.PathJoin(XEnv.ProjectPath, "Library", "AssetStash.db"); }

            internal static string dependencyFile { get => XFile.PathJoin(XEnv.ProjectPath, "Library", "AssetDependency.db"); }

            internal string buildDir;

            internal Preferences preferencesPanel;

            void XEditor.Tasks.Panel.IOnGUI.OnGUI()
            {
                preferencesPanel = preferencesPanel != null ? preferencesPanel : ScriptableObject.CreateInstance<Preferences>();
                preferencesPanel.OnVisualize("Task Runner", XPrefs.Asset);
            }

            public override void Preprocess(XEditor.Tasks.Report report)
            {
                buildDir = XFile.NormalizePath(XPrefs.GetString(Preferences.Output, Preferences.OutputDefault).Eval(XEnv.Instance));
                if (string.IsNullOrEmpty(buildDir)) throw new ArgumentNullException("Preferences.Builder.Output is empty.");
                if (!XFile.HasDirectory(buildDir)) XFile.CreateDirectory(buildDir);

                var maniFile = XFile.PathJoin(buildDir, XMani.Default);
                var tmpManiFile = maniFile + ".tmp";
                if (XFile.HasFile(maniFile)) XFile.CopyFile(maniFile, tmpManiFile);
            }

            public override void Process(XEditor.Tasks.Report report)
            {
                var bundles = GenDependency();
                var builds = new List<AssetBundleBuild>();
                foreach (var kvp in bundles)
                {
                    var build = new AssetBundleBuild
                    {
                        assetBundleName = kvp.Key,
                        assetNames = kvp.Value.ToArray()
                    };
                    builds.Add(build);
                }

                try
                {
                    if (BuildPipeline.BuildAssetBundles(buildDir, builds.ToArray(),
                        BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.AssetBundleStripUnityVersion, EditorUserBuildSettings.activeBuildTarget) == null)
                    {
                        report.Error = "BuildPipeline.BuildAssetBundles returns nil.";
                    }
                }
                catch (Exception e) { XLog.Panic(e); report.Error = e.Message; }
            }

            public override void Postprocess(XEditor.Tasks.Report report)
            {
                GenManifest(report);
                GenSummary(report);
            }

            /// <summary>
            /// GenDependency 分析项目资源并生成依赖关系图，支持自定义打包规则和资源合并策略。
            /// 会处理场景文件、材质球等特殊资源，确保正确的打包顺序和依赖关系。
            /// </summary>
            /// <returns>资源依赖关系字典，键为Bundle名称，值为资源路径列表</returns>
            public static Dictionary<string, List<string>> GenDependency()
            {
                var buildBundles = new Dictionary<string, List<string>>();
                var buildTime = XTime.GetTimestamp();

                try
                {
                    var visited = new List<string>();
                    var fileBundles = new Dictionary<string, List<string>>();
                    var dirBundles = new Dictionary<string, List<string>>();
                    var customBundles = new Dictionary<string, List<string>>();
                    var refCountMap = new Dictionary<string, int>();
                    var sourceAssets = new List<string>();

                    // 处理 Include 规则
                    var includes = XPrefs.GetStrings(Preferences.Include, Preferences.IncludeDefault);
                    if (includes != null && includes.Length > 0)
                    {
                        foreach (var temp in includes)
                        {
                            if (temp.IndexOfAny(new char[] { '*', '?', '[' }) >= 0)
                            {
                                var tempAssets = new List<string>();
                                var rootDir = temp.Split('*', '?', '[')[0].TrimEnd('/', '\\');
                                var partten = temp[rootDir.Length..].TrimStart('/', '\\');

                                if (string.IsNullOrEmpty(rootDir)) rootDir = "Assets";
                                XEditor.Utility.CollectAssets(rootDir, tempAssets, ".cs", ".js", ".meta", ".tpsheet", ".DS_Store", ".gitkeep", ".variant", ".hlsl", ".cginc", ".shadersubgraph");

                                var matcher = new Matcher();
                                matcher.AddInclude(partten);

                                foreach (var asset in tempAssets)
                                {
                                    var relativeAsset = XFile.NormalizePath(Path.GetRelativePath(rootDir, asset));
                                    if (matcher.Match(relativeAsset).HasMatches) sourceAssets.Add(asset);
                                }
                            }
                            else if (XFile.HasFile(temp)) sourceAssets.Add(temp);
                            else if (XFile.HasDirectory(temp))
                            {
                                XEditor.Utility.CollectAssets(temp, sourceAssets, ".cs", ".js", ".meta", ".tpsheet", ".DS_Store", ".gitkeep", ".variant", ".hlsl", ".cginc", ".shadersubgraph");
                            }
                        }
                    }

                    // 处理 Exclude 规则
                    var excludes = XPrefs.GetStrings(Preferences.Exclude);
                    if (excludes != null && excludes.Length > 0)
                    {
                        for (var i = 0; i < sourceAssets.Count;)
                        {
                            var asset = sourceAssets[i];
                            var remove = false;

                            foreach (var exclude in excludes)
                            {
                                if (exclude.IndexOfAny(new char[] { '*', '?', '[' }) >= 0)
                                {
                                    var rootDir = exclude.Split('*', '?', '[')[0];
                                    if (asset.StartsWith(rootDir)) // 判断文件是否匹配根目录
                                    {
                                        var partten = exclude[rootDir.Length..];
                                        var matcher = new Matcher();
                                        matcher.AddInclude(partten);

                                        var relativeAsset = XFile.NormalizePath(Path.GetRelativePath(rootDir, asset));
                                        if (matcher.Match(relativeAsset).HasMatches)
                                        {
                                            remove = true;
                                            break;
                                        }
                                    }
                                }
                                else if (exclude == asset)  // 文件路径完全匹配
                                {
                                    remove = true;
                                    break;
                                }
                            }

                            if (remove)
                            {
                                sourceAssets.RemoveAt(i);
                                XLog.Debug("XAsset.Builder.GenDependency: {0} has been ignored by matcher.", asset);
                            }
                            else i++;
                        }
                    }

                    var dependAssets = XEditor.Utility.CollectDependency(sourceAssets);
                    for (int i = 0; i < sourceAssets.Count; i++)
                    {
                        var asset = sourceAssets[i];
                        visited.Add(asset);
                        var assetImporter = AssetImporter.GetAtPath(asset);
                        if (assetImporter)
                        {
                            var bundleName = Asset.XAsset.Constants.GetName(asset);
                            if (!fileBundles.TryGetValue(bundleName, out var dependencies)) { dependencies = new List<string>(); fileBundles.Add(bundleName, dependencies); }
                            if (!dependencies.Contains(asset)) dependencies.Add(asset);
                        }
                    }

                    var keys = dependAssets.Keys.ToList(); // 优先处理场景以剔除材质依赖
                    keys.Sort((a1, a2) =>
                    {
                        var b1 = a1.EndsWith(".unity") ? 0 : 1;
                        var b2 = a2.EndsWith(".unity") ? 0 : 1;
                        if (b1 > b2) return 1;
                        else if (b1 == b2) return 0;
                        else return -1;
                    });
                    foreach (var key in keys)
                    {
                        var assets = dependAssets[key];
                        for (int j = 0; j < assets.Count; j++)
                        {
                            var asset = assets[j];
                            if (asset.EndsWith(".hlsl") || asset.EndsWith(".cginc") || asset.EndsWith(".shadersubgraph")) continue;
                            if (visited.Contains(asset) == false)
                            {
                                visited.Add(asset);
                                if (asset.Contains("Editor/"))
                                {
                                    XLog.Warn("XAsset.Builder.GenDependency: ignore editor asset dependency: {0}.", asset);
                                    continue;
                                }
                                else
                                {
                                    var temp = asset[..asset.LastIndexOf("/")];
                                    var assetImporter = AssetImporter.GetAtPath(asset);
                                    if (assetImporter)
                                    {
                                        string bundleName;
                                        List<string> dependencies;
                                        if (!string.IsNullOrEmpty(assetImporter.assetBundleName))
                                        {
                                            bundleName = assetImporter.assetBundleName;
                                            XLog.Debug("XAsset.Builder.GenDependency: using custom bundle name: {0} for asset: {1}.", bundleName, asset);
                                            if (!customBundles.TryGetValue(bundleName, out dependencies)) { dependencies = new List<string>(); customBundles.Add(bundleName, dependencies); }
                                            if (!dependencies.Contains(asset)) dependencies.Add(asset);
                                        }
                                        else
                                        {
                                            var skip = assetImporter is ShaderImporter || asset.EndsWith(".shadergraph"); // 强制合并材质，避免着色器变体丢失
                                            if (!skip && XPrefs.GetBool(Preferences.MergeMaterial, Preferences.MergeMaterialDefault)) skip = asset.EndsWith(".mat") && key.EndsWith(".unity");
                                            if (skip) continue;
                                            else
                                            {
                                                var dir = XFile.NormalizePath(Path.GetDirectoryName(asset));
                                                bundleName = Asset.XAsset.Constants.GetName(dir.StartsWith("Assets/") ? dir["Assets/".Length..] : dir);
                                                if (!dirBundles.TryGetValue(bundleName, out dependencies)) { dependencies = new List<string>(); dirBundles.Add(bundleName, dependencies); }
                                                if (!dependencies.Contains(asset)) dependencies.Add(asset);
                                            }
                                        }
                                    }
                                }
                            }
                            if (!refCountMap.TryGetValue(asset, out var count)) refCountMap.Add(asset, count);
                            else refCountMap[asset] = count + 1;
                        }
                    }

                    if (XPrefs.GetBool(Preferences.MergeSingle, Preferences.MergeSingleDefault)) // 若业务层依赖depth为1（间接引用）的包，则会引起异常（如：fairygui.uipanel的bundle）
                    {
                        var deletes = new List<string>();
                        foreach (var kvp in dirBundles)     // 若引用的多个资源都只出现在该包中，也会合并
                        {
                            var sig = true;
                            foreach (var dependency in kvp.Value)
                            {
                                if (refCountMap[dependency] > 0) { sig = false; break; }
                            }
                            if (sig) deletes.Add(kvp.Key);
                        }

                        foreach (var k in deletes)
                        {
                            dirBundles.Remove(k);
                            XLog.Debug("XAsset.Builder.GenDependency: merged raw single asset '{0}'.", k);
                        }
                    }
                    else XLog.Debug("XAsset.Builder.GenDependency: ignore to merge raw single bundles.");

                    foreach (var kvp in fileBundles) buildBundles.Add(kvp.Key, kvp.Value);
                    foreach (var kvp in dirBundles) buildBundles.Add(kvp.Key, kvp.Value);
                    foreach (var kvp in customBundles) buildBundles.Add(kvp.Key, kvp.Value);

                    if (XFile.HasFile(dependencyFile)) XFile.DeleteFile(dependencyFile);
                    using var fs = File.Open(dependencyFile, FileMode.Create);
                    using var sw = new StreamWriter(fs);
                    foreach (var kvp in buildBundles)
                    {
                        sw.WriteLine($"bundle: {kvp.Key}");
                        var assets = kvp.Value;
                        for (var j = 0; j < assets.Count; j++)
                        {
                            var asset = assets[j];
                            if (asset.Contains("Editor/") == false && asset != kvp.Key) sw.WriteLine($"  asset: {asset}");
                        }
                    }
                    sw.Flush();
                    fs.Flush();
                }
                catch (Exception e)
                {
                    if (XFile.HasFile(dependencyFile)) XFile.DeleteFile(dependencyFile);
                    XLog.Panic(e);
                }
                XLog.Debug("XAsset.Builder.GenDependency: generate <a href=\"file:///{0}\">{1}</a> done, elapsed {2}s.", Path.GetFullPath(dependencyFile), Path.GetRelativePath(XEnv.ProjectPath, dependencyFile), XTime.GetTimestamp() - buildTime);
                return buildBundles;
            }

            /// <summary>
            /// GenManifest 根据构建结果生成资源清单文件，记录每个资源包的信息（如MD5、大小等）。
            /// 这个清单文件将用于运行时的资源加载和版本检查。
            /// </summary>
            /// <param name="report">构建报告对象</param>
            private void GenManifest(XEditor.Tasks.Report report)
            {
                if (report.Result != XEditor.Tasks.Result.Succeeded) return;

                var abManifestFilePath = XFile.PathJoin(buildDir, Asset.XAsset.Constants.Manifest);
                var manifestFilePath = abManifestFilePath + ".manifest";
                var assetManifestFilePath = XFile.PathJoin(buildDir, XMani.Default);
                if (XFile.HasFile(assetManifestFilePath)) XFile.DeleteFile(assetManifestFilePath);
                if (XFile.HasFile(abManifestFilePath) == false)
                {
                    report.Error = "No asset bundle manifest file.";
                    XLog.Error("XAsset.Builder.GenManifest: no asset bundle manifest file.");
                    return;
                }

                var bundle = AssetBundle.LoadFromFile(abManifestFilePath);
                var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (manifest == null)
                {
                    report.Error = "Null asset bundle manifest.";
                    XLog.Error("XAsset.Builder.GenManifest: null asset bundle manifest.");
                    return;
                }

                var fs = new FileStream(assetManifestFilePath, FileMode.OpenOrCreate);
                var sw = new StreamWriter(fs);
                // write ab manifest file;
                var manifestMD5 = XFile.FileMD5(abManifestFilePath);
                var manifestSize = XFile.FileSize(abManifestFilePath);
                sw.WriteLine(Asset.XAsset.Constants.GetName(Asset.XAsset.Constants.Manifest) + "|" + manifestMD5 + "|" + manifestSize);
                var lines = File.ReadAllLines(manifestFilePath);
                var abs = new List<string>();
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("      Name: "))
                    {
                        line = line.Replace("      Name: ", "");
                        line = line.Trim();
                        abs.Add(line);
                    }
                }
                var abs2 = new List<string>();
                var count = 0;
                while (abs.Count > 0)
                {
                    for (var i = 0; i < abs.Count;)
                    {
                        var ab = abs[i];
                        var dependencies = manifest.GetAllDependencies(ab);
                        if (dependencies.Length == count)
                        {
                            abs.RemoveAt(i);
                            abs2.Add(ab);
                        }
                        else i++;
                    }
                    count++;
                }
                for (var i = 0; i < abs2.Count; i++)
                {
                    var ab = abs2[i];
                    var filePath = XFile.PathJoin(buildDir, ab);
                    var size = XFile.FileSize(filePath);
                    var md5 = XFile.FileMD5(filePath);
                    sw.WriteLine(ab + "|" + md5 + "|" + size);
                }
                sw.Close();
                fs.Close();
                bundle.Unload(true);

                // 标准化 Bundle 文件命名
                var dstMani = XFile.PathJoin(buildDir, Asset.XAsset.Constants.GetName(Asset.XAsset.Constants.Manifest));
                if (XFile.HasFile(dstMani)) XFile.DeleteFile(dstMani);
                File.Move(abManifestFilePath, dstMani);

                dstMani += ".manifest";
                if (XFile.HasFile(dstMani)) XFile.DeleteFile(dstMani);
                File.Move(manifestFilePath, dstMani);
            }

            /// <summary>
            /// GenSummary 生成构建报告，记录资源变更情况并清理无效的资源文件。
            /// 通过对比新旧清单，可以了解此次构建的具体改动。
            /// </summary>
            /// <param name="report">构建报告对象，用于记录构建过程中的信息</param>
            private void GenSummary(XEditor.Tasks.Report report)
            {
                var tmpFile = XFile.PathJoin(buildDir, XMani.Default + ".tmp");
                var tmpMani = new XMani.Manifest(tmpFile);
                if (XFile.HasFile(tmpFile))
                {
                    tmpMani.Read();
                    XFile.DeleteFile(XFile.PathJoin(buildDir, XMani.Default + ".tmp"));
                }
                if (report.Result != XEditor.Tasks.Result.Succeeded) return;

                var maniFile = XFile.PathJoin(buildDir, XMani.Default);
                var mani = new XMani.Manifest(maniFile);
                mani.Read();

                var diff = tmpMani.Compare(mani);
                for (var i = 0; i < diff.Modified.Count; i++)
                {
                    var fi = diff.Modified[i];
                    XLog.Debug("XAsset.Builder.GenSummary: {0} has been modified.", fi.Name);
                }

                for (var i = 0; i < diff.Added.Count; i++)
                {
                    var fi = diff.Added[i];
                    XLog.Debug("XAsset.Builder.GenSummary: {0} has been added.", fi.Name);
                }

                for (var i = 0; i < diff.Deleted.Count; i++)
                {
                    var fi = diff.Deleted[i];
                    var file = XFile.PathJoin(buildDir, fi.Name);
                    var mfile = file + ".manifest";
                    XFile.DeleteFile(file);
                    XFile.DeleteFile(mfile);
                    XLog.Debug("XAsset.Builder.GenSummary: {0} has been deleted.", fi.Name);
                }

                try
                {
                    var files = Directory.GetFiles(buildDir);
                    for (var i = 0; i < files.Length; i++)
                    {
                        var f = files[i];
                        var n = Path.GetFileName(f);
                        var e = Path.GetExtension(f);
                        if (n == XMani.Default || e == ".manifest") continue;
                        if (mani.Files.Find((e) => { return e.Name == n; }) == null)
                        {
                            XFile.DeleteFile(f);
                            XFile.DeleteFile(f + ".manifest");
                            XLog.Warn("XAsset.Builder.GenSummary: invalid {0} has been deleted.", n);
                        }
                    }
                }
                catch (Exception e) { XLog.Panic(e); }

                var dirty = false;
                foreach (var fi in mani.Files)
                {
                    if (GenOffset(fi)) dirty = true;
                }
                if (dirty) XFile.SaveText(maniFile, mani.ToString());

                XLog.Debug("XAsset.Builder.GenSummary: {0} asset(s) has been modified, {1} asset(s) has been added, {2} asset(s) has been deleted.", diff.Modified.Count, diff.Added.Count, diff.Deleted.Count);
            }

            /// <summary>
            /// GenOffset 用于处理 Bundle 文件的偏移。
            /// </summary>
            /// <param name="fi">Bundle 文件信息</param>
            private bool GenOffset(XMani.FileInfo fi)
            {
                var offsetFactor = XPrefs.GetInt(Asset.XAsset.Preferences.OffsetFactor, Asset.XAsset.Preferences.OffsetFactorDefault);
                if (offsetFactor <= 0) return false;

                var src = XFile.PathJoin(buildDir, fi.Name);
                var dst = src + ".tmp";
                if (XFile.HasFile(dst)) XFile.DeleteFile(dst);

                var prefix = new byte[28];
                // 根据文件名长度计算 offsetCount，且至少偏移一个单元
                var offsetCount = fi.Name.Length % offsetFactor + 1;

                using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read))
                {
                    // 先读 prefix
                    var readPrefix = new byte[28];
                    int readLen = fs.Read(readPrefix, 0, readPrefix.Length);
                    if (readLen != prefix.Length) return false; // 文件太小，不处理

                    Buffer.BlockCopy(readPrefix, 0, prefix, 0, prefix.Length);

                    // 再读 offsetCount * prefix.Length 用于比对
                    var totalCheckSize = prefix.Length * offsetCount;
                    var checkData = new byte[totalCheckSize];
                    readLen = fs.Read(checkData, 0, totalCheckSize);

                    var alreadyOffset = true;
                    if (readLen < totalCheckSize) alreadyOffset = false;
                    else
                    {
                        for (int i = 0; i < offsetCount; i++)
                        {
                            for (int j = 0; j < prefix.Length; j++)
                            {
                                if (checkData[i * prefix.Length + j] != prefix[j])
                                {
                                    alreadyOffset = false;
                                    break;
                                }
                            }
                            if (!alreadyOffset) break;
                        }
                    }

                    if (alreadyOffset) return false;
                }

                // 如果未偏移则执行偏移写入
                var buffer = new byte[1024];
                using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read))
                using (var sw = new FileStream(dst, FileMode.Create, FileAccess.Write))
                {
                    // 读取原始的 prefix
                    fs.Read(prefix, 0, prefix.Length);

                    // 写入原始的 prefix
                    sw.Write(prefix, 0, prefix.Length);

                    // 写入偏移的 prefix
                    for (var i = 0; i < offsetCount; i++) sw.Write(prefix, 0, prefix.Length);

                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0) sw.Write(buffer, 0, bytesRead);

                    sw.Flush();
                }

                XFile.DeleteFile(src);
                File.Move(dst, src);
                XFile.DeleteFile(dst);
                fi.Size = XFile.FileSize(src);
                fi.MD5 = XFile.FileMD5(src);
                XLog.Debug("XAsset.Builder.GenOffset: add {0} prefix offset into {1}.", offsetCount, fi.Name);
                return true;
            }

            /// <summary>
            /// XEditor.Event.Internal.OnPreprocessBuild.Process 是构建开始前的预处理，主要处理平台相关的资源复制工作。
            /// 对于移动平台，会将资源打包成 zip 文件以便于分发。
            /// </summary>
            /// <param name="args">构建参数数组</param>
            void XEditor.Event.Internal.OnPreprocessBuild.Process(params object[] args)
            {
                if (!XPrefs.GetBool(Asset.XAsset.Preferences.BundleMode, Asset.XAsset.Preferences.BundleModeDefault))
                {
                    XLog.Debug("XAsset.Builder.OnPreprocessBuild: ignore to preprocess in non-bundle mode.");
                    return;
                }

                Stash();

                if (XPrefs.GetBool(Preferences.StreamingAssets, Preferences.StreamingAssetsDefault))
                {
                    var srcDir = XFile.NormalizePath(XPrefs.GetString(Preferences.Output, Preferences.OutputDefault).Eval(XEnv.Instance));
                    if (!XFile.HasDirectory(srcDir))
                    {
                        XLog.Warn("XAsset.Builder.OnPreprocessBuild: ignore to streaming asset(s) because of non-exists dir: {0}.", srcDir);
                    }
                    else
                    {
                        if (XEnv.Platform == XEnv.PlatformType.Android || XEnv.Platform == XEnv.PlatformType.iOS)
                        {
                            var dstDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", XPrefs.GetString(Asset.XAsset.Preferences.LocalUri, Asset.XAsset.Preferences.LocalUriDefault));
                            var srcZip = XFile.PathJoin(XEnv.ProjectPath, "Temp", XPrefs.GetString(Asset.XAsset.Preferences.AssetUri, Asset.XAsset.Preferences.AssetUriDefault));
                            var dstZip = XFile.PathJoin(XEnv.AssetPath, XPrefs.GetString(Asset.XAsset.Preferences.AssetUri, Asset.XAsset.Preferences.AssetUriDefault));

                            if (XFile.HasDirectory(dstDir)) XFile.DeleteDirectory(dstDir);
                            XFile.CopyDirectory(srcDir, dstDir, ".manifest");
                            XEditor.Utility.ZipDirectory(dstDir, srcZip);
                            XFile.CopyFile(srcZip, dstZip);

                            AssetDatabase.Refresh();
                        }
                        else
                        {
                            XEditor.Event.Decode<BuildReport>(out var report, args);
                            var outputDir = Path.GetDirectoryName(report.summary.outputPath);
                            var outputName = Path.GetFileNameWithoutExtension(report.summary.outputPath);
                            var dstDir = XFile.PathJoin(outputDir, outputName + "_Data", "Local", XPrefs.GetString(Asset.XAsset.Preferences.LocalUri, Asset.XAsset.Preferences.LocalUriDefault));
                            XFile.CopyDirectory(srcDir, dstDir, ".manifest");
                        }
                        XLog.Debug("XAsset.Builder.OnPreprocessBuild: streaming asset(s) from <a href=\"file:///{0}\">{1}</a> succeeded.", Path.GetFullPath(srcDir), Path.GetRelativePath(XEnv.ProjectPath, srcDir));
                    }
                }
            }

            /// <summary>
            /// XEditor.Event.Internal.OnPostprocessBuild.Process 是构建完成后的后处理，负责恢复暂存的资源并清理临时文件。
            /// 确保构建过程不会影响项目的正常开发。
            /// </summary>
            /// <param name="args">构建参数数组</param>
            void XEditor.Event.Internal.OnPostprocessBuild.Process(params object[] args)
            {
                if (!XPrefs.GetBool(Asset.XAsset.Preferences.BundleMode, Asset.XAsset.Preferences.BundleModeDefault))
                {
                    XLog.Debug("XAsset.Builder.OnPostprocessBuild: ignore to postprocess in non-bundle mode.");
                    return;
                }

                Restore();

                if (XPrefs.GetBool(Preferences.StreamingAssets, Preferences.StreamingAssetsDefault))
                {
                    if (XEnv.Platform == XEnv.PlatformType.Android)
                    {
                        var dstZip = XFile.PathJoin(XEnv.AssetPath, "Patch@Assets.zip");
                        if (XFile.HasFile(dstZip))
                        {
                            XFile.DeleteFile(dstZip);
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            /// <summary>
            /// Stash 将指定资源暂时移动到临时位置，用于构建过程中的资源管理。
            /// 会同时处理资源文件及其对应的 meta 文件，并记录暂存信息。
            /// </summary>
            public static void Stash()
            {
                try
                {
                    if (XFile.HasFile(stashFile)) XFile.DeleteFile(stashFile);
                    using var fs = File.Open(stashFile, FileMode.Create);
                    using var sw = new StreamWriter(fs);

                    var stashes = XPrefs.GetStrings(Preferences.Stash, Preferences.StashDefault).OrderByDescending(asset => asset.Length).ToList();
                    foreach (var stash in stashes)
                    {
                        var src = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, stash));
                        var dst = XFile.HasFile(src) ? XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src)) : $"{src}~";
                        if (XFile.HasDirectory(src) || XFile.HasFile(src))
                        {
                            if (XFile.HasDirectory(dst)) XFile.DeleteDirectory(dst);
                            if (XFile.HasFile(dst)) XFile.DeleteFile(dst);

                            FileUtil.MoveFileOrDirectory(src, dst);

                            var srcm = XFile.PathJoin(Path.GetDirectoryName(src), Path.GetFileName(src) + ".meta");
                            var dstm = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src) + ".meta");
                            if (XFile.HasFile(srcm))
                            {
                                if (XFile.HasFile(dstm)) XFile.DeleteFile(dstm);
                                FileUtil.MoveFileOrDirectory(srcm, dstm);
                            }

                            AssetDatabase.Refresh();
                            sw.WriteLine(stash);
                            XLog.Debug("XAsset.Builder.Stash: stashed asset {0} to {1}.", src, dst);
                        }
                        else XLog.Warn("XAsset.Builder.Stash: stashed asset {0} not found.", src);
                    }

                    sw.Flush();
                    fs.Flush();
                }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// Restore 将暂存的资源恢复到原始位置。
            /// 为了避免资源丢失，不会主动删除目标位置的文件，如果恢复过程中出现问题，会提示用户手动处理。
            /// </summary>
            [InitializeOnLoadMethod]
            public static void Restore()
            {
                if (XFile.HasFile(stashFile))
                {
                    try
                    {
                        var stashes = File.ReadAllLines(stashFile);
                        foreach (var stash in stashes)
                        {
                            var src = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, stash));
                            var dst = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src));
                            if (!XFile.HasFile(dst)) dst = $"{src}~";
                            if (XFile.HasDirectory(dst) || XFile.HasFile(dst))
                            {
                                // 不主动删除，避免资源丢失，抛异常用户自行处理
                                // if (XFile.HasDirectory(dst)) XFile.DeleteDirectory(dst);
                                // if (XFile.HasFile(dst)) XFile.DeleteFile(dst);
                                FileUtil.MoveFileOrDirectory(dst, src);

                                var srcm = XFile.PathJoin(Path.GetDirectoryName(src), Path.GetFileName(src) + ".meta");
                                var dstm = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src) + ".meta");
                                if (XFile.HasFile(dstm))
                                {
                                    // if (XFile.HasFile(srcm)) XFile.DeleteFile(srcm); // 同上
                                    FileUtil.MoveFileOrDirectory(dstm, srcm);
                                }

                                AssetDatabase.Refresh();
                                XLog.Debug("XAsset.Builder.Restore: popuped asset {0} from {1}.", dst, src);
                            }
                        }
                    }
                    catch (Exception e) { XLog.Panic(e); }
                    finally { XFile.DeleteFile(stashFile); }
                }
            }
        }
    }
}
