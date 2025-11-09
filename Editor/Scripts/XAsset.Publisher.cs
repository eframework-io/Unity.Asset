// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset.Editor
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Publisher 实现了资源包的发布工作流，用于将打包好的资源发布至存储服务中。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 首选项配置：提供首选项配置以自定义发布流程
        /// - 自动化流程：提供资源包发布任务的自动化执行
        /// 
        /// 使用手册
        /// 
        /// 1. 首选项配置
        /// 
        /// | 配置项   | 配置键                      | 默认值             |
        /// | -------- | --------------------------- | ------------------ |
        /// | 存储服务地址 | `Asset/Publisher/Endpoint@Editor` | `${Environment.StorageEndpoint}`   |
        /// | 存储分区名称 | `Asset/Publisher/Bucket@Editor` | `${Environment.StorageBucket}` |
        /// | 存储服务凭证 | `Asset/Publisher/Access@Editor` | `${Environment.StorageAccess}` |
        /// | 存储服务密钥 | `Asset/Publisher/Secret@Editor` | `${Environment.StorageSecret}` |
        /// 
        /// 关联配置项：`Asset/LocalUri`、`Asset/RemoteUri`
        /// 
        /// 以上配置项均可在 `Tools/EFramework/Preferences/Asset/Publisher` 首选项编辑器中进行可视化配置。
        /// 
        /// 2. 自动化流程
        /// 
        /// 2.1 本地环境
        /// 
        /// 本地开发环境可以使用 MinIO 作为文件存储服务：
        /// 
        /// 1. 安装服务：
        /// 
        /// ```bash
        /// # 启动 MinIO 容器
        /// docker run -d --name minio -p 9000:9000 -p 9090:9090 --restart=always \
        ///   -e "MINIO_ACCESS_KEY=admin" -e "MINIO_SECRET_KEY=adminadmin" \
        ///   minio/minio server /data --console-address ":9090" --address ":9000"
        /// ```
        /// 
        /// 2. 服务配置：
        /// - 控制台：http://localhost:9090
        /// - API：http://localhost:9000
        /// - 凭证：
        ///   - Access Key：admin
        ///   - Secret Key：adminadmin
        /// - 存储：创建 `default` 存储桶并设置公开访问权限
        /// 
        /// 3. 首选项配置：
        /// ```
        /// Asset/Publisher/Endpoint@Editor = http://localhost:9000
        /// Asset/Publisher/Bucket@Editor = default
        /// Asset/Publisher/Access@Editor = admin
        /// Asset/Publisher/Secret@Editor = adminadmin
        /// ```
        /// 
        /// 2.2 发布流程
        /// 
        /// ```mermaid
        /// stateDiagram-v2
        ///     direction LR
        ///     读取发布配置 --> 获取远端清单
        ///     获取远端清单 --> 对比本地清单
        ///     对比本地清单 --> 发布差异文件
        /// ```
        /// 
        /// 发布时根据清单对比结果进行增量上传：
        /// - 新增文件：`文件名@MD5`
        /// - 修改文件：`文件名@MD5`
        /// - 清单文件：`Manifest.db` 和 `Manifest.db@MD5`（用于版本记录）
        /// </code>
        /// 
        /// 更多信息请参考模块文档。
        /// </remarks>
        [XEditor.Tasks.Worker(name: "Publish Assets", group: "XAsset", runasync: true, priority: 102)]
        public class Publisher : XEditor.MinIO, XEditor.Tasks.Panel.IOnGUI
        {
            /// <summary>
            /// Preferences 是发布流程首选项设置类，包含存储服务相关的配置选项。
            /// </summary>
            public class Preferences : Builder.Preferences
            {
                /// <summary>
                /// Endpoint 是存储服务地址的键名。
                /// </summary>
                public const string Endpoint = "XAsset/Publisher/Endpoint@Editor";

                /// <summary>
                /// EndpointDefault 是存储服务地址的默认值。
                /// </summary>
                public const string EndpointDefault = "${Environment.StorageEndpoint}";

                /// <summary>
                /// Bucket 是存储分区名称的键名。
                /// </summary>
                public const string Bucket = "XAsset/Publisher/Bucket@Editor";

                /// <summary>
                /// BucketDefault 是存储分区名称的默认值。
                /// </summary>
                public const string BucketDefault = "${Environment.StorageBucket}";

                /// <summary>
                /// Access 是存储服务凭证的键名。
                /// </summary>
                public const string Access = "XAsset/Publisher/Access@Editor";

                /// <summary>
                /// AccessDefault 是存储服务凭证的默认值。
                /// </summary>
                public const string AccessDefault = "${Environment.StorageAccess}";

                /// <summary>
                /// Secret 是存储服务密钥的键名。
                /// </summary>
                public const string Secret = "XAsset/Publisher/Secret@Editor";

                /// <summary>
                /// SecretDefault 是存储服务密钥的默认值。
                /// </summary>
                public const string SecretDefault = "${Environment.StorageSecret}";

                /// <summary>
                /// Section 获取面板章节的名称。
                /// </summary>
                public override string Section => "XAsset";

                /// <summary>
                /// Priority 获取面板显示的优先级。
                /// </summary>
                public override int Priority => 102;

                /// <summary>
                /// <see cref="Preferences"/> 初始化的新实例。
                /// </summary>
                public Preferences() { foldout = false; }

                [NonSerialized] SerializedObject serialized;

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
                        foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Publisher", "XAsset Publisher Options."));
                    }
                    else foldout = true;
                    if (foldout && bundleMode)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Endpoint", "Storage Service Endpoint."), GUILayout.Width(60));
                        context.Set(Endpoint, EditorGUILayout.TextField("", context.GetString(Endpoint, EndpointDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Bucket", "Storage Bucket Name."), GUILayout.Width(60));
                        context.Set(Bucket, EditorGUILayout.TextField("", context.GetString(Bucket, BucketDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Access", "Storage Access Key."), GUILayout.Width(60));
                        context.Set(Access, EditorGUILayout.TextField("", context.GetString(Access, AccessDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("Secret", "Storage Secret Key."), GUILayout.Width(60));
                        context.Set(Secret, EditorGUILayout.TextField("", context.GetString(Secret, SecretDefault)));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                    else if (foldout && !bundleMode) EditorGUILayout.HelpBox("Bundle Mode is Disabled.", MessageType.None);
                    GUI.color = ocolor;

                    if (!taskPanel) EditorGUILayout.EndVertical();
                }
            }

            internal Preferences preferencesPanel;

            void XEditor.Tasks.Panel.IOnGUI.OnGUI()
            {
                preferencesPanel = preferencesPanel != null ? preferencesPanel : ScriptableObject.CreateInstance<Preferences>();
                preferencesPanel.OnVisualize("Task Runner", XPrefs.Asset);
            }

            public override void Preprocess(XEditor.Tasks.Report report)
            {
                Endpoint = XPrefs.GetString(Preferences.Endpoint, Preferences.EndpointDefault).Eval(XEnv.Instance);
                Bucket = XPrefs.GetString(Preferences.Bucket, Preferences.BucketDefault).Eval(XEnv.Instance);
                Access = XPrefs.GetString(Preferences.Access, Preferences.AccessDefault).Eval(XEnv.Instance);
                Secret = XPrefs.GetString(Preferences.Secret, Preferences.SecretDefault).Eval(XEnv.Instance);
                base.Preprocess(report);
                Local = XFile.PathJoin(Temp, XPrefs.GetString(Asset.XAsset.Preferences.LocalUri, Asset.XAsset.Preferences.LocalUriDefault));
                Remote = XPrefs.GetString(Asset.XAsset.Preferences.RemoteUri, Asset.XAsset.Preferences.RemoteUriDefault).Eval(XEnv.Instance);
            }

            public override void Process(XEditor.Tasks.Report report)
            {
                var root = XFile.NormalizePath(XPrefs.GetString(Builder.Preferences.Output, Builder.Preferences.OutputDefault).Eval(XEnv.Instance));

                var remoteMani = new XMani.Manifest();
                var tempFile = Path.GetTempFileName();
                var task = XEditor.Command.Run(bin: Bin, args: new string[] { "get", $"\"{Alias}/{Bucket}/{Remote}/{XMani.Default}\"", tempFile });
                task.Wait();
                if (task.Result.Code != 0)
                {
                    XLog.Warn("XAsset.Publisher.Process: get remote mainifest failed: {0}", task.Result.Error);
                }
                else
                {
                    remoteMani.Read(tempFile);
                    if (!string.IsNullOrEmpty(remoteMani.Error)) XLog.Warn("XAsset.Publisher.Process: parse remote mainifest failed: {0}", remoteMani.Error);
                }

                var localMani = new XMani.Manifest();
                localMani.Read(XFile.PathJoin(root, XMani.Default));
                if (!string.IsNullOrEmpty(localMani.Error)) XLog.Warn("XAsset.Publisher.Process: parse local mainifest failed: {0}", remoteMani.Error);
                else
                {
                    var diff = remoteMani.Compare(localMani);
                    var files = new List<string[]>();
                    for (var i = 0; i < diff.Added.Count; i++) { files.Add(new string[] { XFile.PathJoin(root, diff.Added[i].Name), diff.Added[i].MD5 }); }
                    for (var i = 0; i < diff.Modified.Count; i++) { files.Add(new string[] { XFile.PathJoin(root, diff.Modified[i].Name), diff.Modified[i].MD5 }); }
                    if (diff.Added.Count > 0 || diff.Modified.Count > 0)
                    {
                        var maniFile = XFile.PathJoin(root, XMani.Default);
                        files.Add(new string[] { maniFile, "" });
                        files.Add(new string[] { maniFile, XFile.FileMD5(maniFile) });
                    }
                    if (files.Count == 0)
                    {
                        XLog.Debug("XAsset.Publisher.Process: diff files is zero, no need to publish.");
                        return;
                    }
                    else
                    {
                        foreach (var kvp in files)
                        {
                            var file = kvp[0];
                            var md5 = kvp[1];
                            var src = file;
                            var dst = XFile.PathJoin(Local, Path.GetRelativePath(root, file));
                            if (!string.IsNullOrEmpty(md5)) dst += "@" + md5; // file@md5
                            var dir = Path.GetDirectoryName(dst);
                            if (!XFile.HasDirectory(dir)) XFile.CreateDirectory(dir);
                            XFile.CopyFile(src, dst);
                        }
                    }

                    base.Process(report);
                }
            }
        }
    }
}
