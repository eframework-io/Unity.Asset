// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Bundle 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 资源包管理：支持同步和异步加载资源包，支持缓存
        /// - 引用计数管理：自动处理依赖关系，通过引用计数管理资源的生命周期
        ///
        /// 使用手册
        /// 1. 基本用法
        ///
        /// 1.1 初始化资源清单
        /// - 功能说明：初始化 Bundle 的清单文件
        /// - 使用示例：
        /// <code>
        /// /// Initialize 初始化 Bundle 的清单文件，如果存在旧的清单会先卸载它。
        /// /// 仅适用于 Bundle 模式，这个清单文件对于资源的正确加载是必需的。
        /// XAsset.Bundle.Initialize();
        /// </code>
        ///
        /// 1.2 同步加载资源包
        /// - 功能说明：加载指定的资源包及其所有依赖资源包
        /// - 使用示例：
        /// <code>
        /// var bundle = XAsset.Bundle.Load("example.bundle");
        /// </code>
        ///
        /// 1.3 异步加载资源包
        /// - 功能说明：异步加载指定的资源包及其所有依赖资源包
        /// - 使用示例：
        /// <code>
        /// var handler = new Handler();
        /// yield return XAsset.Bundle.LoadAsync("example.bundle", handler);
        /// </code>
        ///
        /// 1.4 查找已加载的资源包
        /// - 功能说明：在已加载的资源包中查找指定名称的资源包
        /// - 使用示例：
        /// <code>
        /// var bundle = XAsset.Bundle.Find("example.bundle");
        /// </code>
        ///
        /// 1.5 卸载已加载的资源包
        /// - 功能说明：卸载指定的资源包，减少其引用计数，当计数为 0 时释放资源
        /// - 使用示例：
        /// <code>
        /// XAsset.Bundle.Unload("example.bundle");
        /// </code>
        ///
        /// 2. 引用计数
        ///
        /// 2.1 增加引用
        /// - 功能说明：增加资源包的引用计数，同时增加所有依赖资源包的引用计数
        /// - 使用示例：
        /// <code>
        /// var count = bundle.Retain();
        /// </code>
        ///
        /// 2.2 减少引用
        /// - 功能说明：减少资源包的引用计数，当计数为 0 时自动卸载资源包及其不再被引用的依赖资源
        /// - 使用示例：
        /// <code>
        /// var count = bundle.Release();
        /// </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Bundle
        {
            /// <summary>
            /// Name 是资源包的名称，用于在资源系统中唯一标识一个资源包。
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            /// Source 是 Unity 的 AssetBundle 对象，包含实际的资源数据。
            /// </summary>
            public AssetBundle Source { get; internal set; }

            /// <summary>
            /// Count 是资源包的引用计数，用于追踪资源包的使用情况。当计数为 0 时，资源包可以被安全卸载。
            /// </summary>
            public int Count { get; internal set; }

            /// <summary>
            /// Retain 增加资源包的引用计数，同时增加所有依赖资源包的引用计数。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>增加后的引用计数</returns>
            public int Retain(string from = "")
            {
                var dependencies = Manifest.GetAllDependencies(Name);
                foreach (var dependency in dependencies)
                {
                    if (Name == dependency) continue;
                    if (Loaded.TryGetValue(dependency, out var dependBundle))
                    {
                        dependBundle.Count++;
                        if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Retain: depend bundle: {0}, reference count: {1}, cached bundle count: {2}, retain from: {3}.", dependBundle.Name, dependBundle.Count, Loaded.Count, from);
                    }
                }
                Count++;
                if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Retain: main bundle: {0}, reference count: {1}, cached bundle count: {2}, retain from: {3}.", Name, Count, Loaded.Count, from);
                return Count;
            }

            /// <summary>
            /// Release 减少资源包的引用计数，当计数为 0 时自动卸载资源包及其不再被引用的依赖资源。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>减少后的引用计数</returns>
            public int Release(string from = "")
            {
                var dependencies = Manifest.GetAllDependencies(Name);
                if (dependencies != null && dependencies.Length > 0)
                {
                    for (var i = 0; i < dependencies.Length; i++)
                    {
                        var dependency = dependencies[i];
                        if (dependency == Name) continue;
                        if (Loaded.TryGetValue(dependency, out var dependBundle))
                        {
                            dependBundle.Count--;
                            if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Release: depend bundle: {0}, reference count: {1}, cached bundle count: {2}, release from: {3}.", dependBundle.Name, dependBundle.Count, Loaded.Count, from);
                            if (dependBundle.Source == null) Loaded.Remove(dependency);
                            else if (dependBundle.Count <= 0)
                            {
                                dependBundle.Source.Unload(true);
                                Loaded.Remove(dependency);
                                if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload depend bundle: {0}, cached bundle: {1}, release from: {2}.", dependBundle.Name, Loaded.Count, from);
                                try { Event.Notify(EventType.OnPostUnloadBundle, dependBundle.Source); }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                    }
                }
                Count--;
                if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Release: main bundle: {0}, reference count: {1}, cached bundle count: {2}, release from: {3}.", Name, Count, Loaded.Count, from);
                if (Source == null)
                {
                    Loaded.Remove(Name);
                }
                else if (Count <= 0)
                {
                    try { Event.Notify(EventType.OnPostUnloadBundle, Source); }
                    catch (Exception e) { XLog.Panic(e); }
                    Source.Unload(true);
                    Loaded.Remove(Name);
                    if (Constants.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload main bundle: {0}, cached bundle count: {1}, release from: {2}.", Name, Loaded.Count, from);
                }
                return Count;
            }
        }

        public partial class Bundle
        {
            /// <summary>
            /// Task 是异步加载任务，用于跟踪资源包的加载状态和进度。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// Name 是正在加载的资源包名称。
                /// </summary>
                internal string Name;

                /// <summary>
                /// Request 是 AssetBundle 的异步加载操作对象。
                /// </summary>
                internal AssetBundleCreateRequest Request;

                /// <summary>
                /// IsDone 表示是否完成加载。
                /// </summary>
                internal bool IsDone;
            }

            /// <summary>
            /// Manifest 是 Bundle 依赖的清单。
            /// </summary>
            internal static AssetBundleManifest Manifest;

            /// <summary>
            /// manifestBundle 保持了 AssetBundleManifest 所在 Bundle 的引用。
            /// </summary>
            private static AssetBundle manifestBundle;

            /// <summary>
            /// Loading 记录当前正在加载的资源包，用于处理并发加载请求。
            /// </summary>
            internal static Dictionary<string, Task> Loading = new();

            /// <summary>
            /// Loaded 缓存已加载的资源包，避免重复加载相同的资源。
            /// </summary>
            internal static Dictionary<string, Bundle> Loaded = new();

            /// <summary>
            /// Initialize 初始化 Bundle 的清单文件，如果存在旧的清单会先卸载它。
            /// 仅适用于 Bundle 模式，这个清单文件对于资源的正确加载是必需的。
            /// </summary>
            public static void Initialize()
            {
                if (Constants.BundleMode)
                {
                    try
                    {
                        if (manifestBundle)
                        {
                            manifestBundle.Unload(true);
                            XLog.Notice("XAsset.Bundle.Initialize: previous manifest has been unloaded.");
                        }
                    }
                    catch (Exception e) { XLog.Panic(e, "XAsset.Bundle.Initialize: unload manifest failed."); }

                    var file = XFile.PathJoin(Constants.LocalPath, Constants.GetName(Constants.Manifest));
                    if (XFile.HasFile(file))
                    {
                        try
                        {
                            manifestBundle = AssetBundle.LoadFromFile(file, 0, Constants.GetOffset(Path.GetFileName(file)));
                            Manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            XLog.Notice("XAsset.Bundle.Initialize: load manifest from <a href=\"file:///{0}\">{1}</a> succeeded.", Path.GetFullPath(file), Path.GetRelativePath(XEnv.ProjectPath, file));
                        }
                        catch (Exception e) { XLog.Panic(e, "XAsset.Bundle.Initialize: load manifest from <a href=\"file:///{0}\">{1}</a> failed.".Format(Path.GetFullPath(file), Path.GetRelativePath(XEnv.ProjectPath, file))); }
                    }
                    else XLog.Warn("XAsset.Bundle.Initialize: load failed because of non exist file: {0}.", Path.GetRelativePath(XEnv.ProjectPath, file));
                }
                else Manifest = null;
            }

            /// <summary>
            /// Load 同步加载资源包。如果资源包已加载，则直接返回缓存的实例，否则会加载资源包及其所有依赖。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <returns>加载的资源包，如果加载失败则返回 null</returns>
            public static Bundle Load(string name)
            {
                if (!Loaded.TryGetValue(name, out var bundleInfo))
                {
                    var dependencies = Manifest.GetAllDependencies(name);
                    if (dependencies != null && dependencies.Length > 0)
                    {
                        var breakDependency = -1;
                        for (var i = 0; i < dependencies.Length; i++)
                        {
                            var dependency = dependencies[i];
                            if (!Loaded.ContainsKey(dependency))
                            {
                                if (Loading.TryGetValue(dependency, out var dependencyTask))
                                {
                                    // 如果正在异步加载，通过访问 assetBundle 可以获取到实例，然后 isDone 为 true
                                    // 这样就可以解决同步和异步并发加载相同资源导致的异常
                                    // 内部机制：https://docs.unity.cn/cn/2021.1/ScriptReference/AssetBundleCreateRequest-assetBundle.html
                                    // 踩坑小记：访问 assetBundle 时，其内部会执行 UnityEngine.SetupCoroutine:InvokeMoveNext
                                    // 这样一来 LoadAsync 的 yield request 后续的流程就会执行，然后装载实例至 Loaded 字典中
                                    // 所以维护当前分支无需维护 Loaded 字典，仅获取即可
                                    if (dependencyTask.Request.assetBundle) XLog.Warn("XAsset.Bundle.Load: access depend bundle: {0} in async loading queue.", dependency);
                                    else XLog.Error("XAsset.Bundle.Load: access nil depend bundle: {0} in async loading queue.", dependency);
                                }
                                else
                                {
                                    var file = XFile.PathJoin(Constants.LocalPath, dependency);
                                    var bundle = AssetBundle.LoadFromFile(file, 0, Constants.GetOffset(dependency));
                                    if (bundle == null)
                                    {
                                        XLog.Error("XAsset.Bundle.Load: sync load depend bundle error: {0}.", dependency);
                                        breakDependency = i;
                                        break;
                                    }
                                    else
                                    {
                                        var dependBundle = new Bundle() { Name = dependency, Source = bundle };
                                        Loaded.Add(dependency, dependBundle);
                                    }
                                }
                            }
                        }

                        // 如果有任何一个依赖加载失败，则解除对已加载依赖的引用
                        if (breakDependency >= 0)
                        {
                            for (var i = 0; i < breakDependency; i++)
                            {
                                var dependency = dependencies[i];
                                if (Loaded.TryGetValue(dependency, out var dependBundle))
                                {
                                    if (dependBundle.Count == 0) // 只处理未被引用的 Bundle
                                    {
                                        if (dependBundle.Source) dependBundle.Source.Unload(true);
                                        Loaded.Remove(dependency);
                                        XLog.Warn("XAsset.Bundle.Load: unload: {0} because of broken depend bundle: {1}.", dependency, dependencies[breakDependency]);
                                    }
                                }
                            }

                            return null; // 不再执行后续流程
                        }
                    }

                    if (Loading.TryGetValue(name, out var mainTask))
                    {
                        // 流程处理机制参考上述加载依赖的分支
                        if (mainTask.Request.assetBundle) XLog.Warn("XAsset.Bundle.Load: access main bundle: {0} in async loading queue.", name);
                        else XLog.Error("XAsset.Bundle.Load: access nil main bundle: {0} in async loading queue.", name);
                        bundleInfo = Find(name);
                    }
                    else
                    {
                        var file = XFile.PathJoin(Constants.LocalPath, name);
                        var bundle = AssetBundle.LoadFromFile(file, 0, Constants.GetOffset(name));
                        if (bundle == null)
                        {
                            XLog.Error("XAsset.Bundle.Load: sync load main bundle error: {0}.", name);

                            // 如果主包加载失败，则解除对所有依赖的引用
                            if (dependencies != null && dependencies.Length > 0)
                            {
                                for (var i = 0; i < dependencies.Length; i++)
                                {
                                    var dependency = dependencies[i];
                                    if (Loaded.TryGetValue(dependency, out var dependBundle))
                                    {
                                        if (dependBundle.Count == 0) // 只处理未被引用的 Bundle
                                        {
                                            if (dependBundle.Source) dependBundle.Source.Unload(true);
                                            Loaded.Remove(dependency);
                                            XLog.Warn("XAsset.Bundle.Load: unload: {0} because of broken main bundle: {1}.", dependency, name);
                                        }
                                    }
                                }
                            }

                            return null; // 不再执行后续流程
                        }

                        bundleInfo = new Bundle() { Name = name, Source = bundle };
                        Loaded.Add(name, bundleInfo);
                    }
                }

                return bundleInfo;
            }

            /// <summary>
            /// LoadAsync 异步加载资源包。支持同时加载多个资源包，并通过 handler 参数报告加载进度。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <param name="handler">用于跟踪和报告加载进度的处理器</param>
            /// <returns>异步加载的协程对象</returns>
            public static IEnumerator LoadAsync(string name, Handler handler = null)
            {
                if (!Loaded.ContainsKey(name))
                {
                    var dependencies = Manifest.GetAllDependencies(name);
                    if (handler != null) handler.totalCount += dependencies.Length + 1; // Self and Dependency
                    if (dependencies != null && dependencies.Length > 0)
                    {
                        var breakDependency = -1;
                        for (var i = 0; i < dependencies.Length; i++)
                        {
                            var dependency = dependencies[i];
                            if (!Loaded.ContainsKey(dependency))
                            {
                                if (!Loading.TryGetValue(dependency, out var task))
                                {
                                    task = new Task() { Name = dependency };
                                    Loading.Add(dependency, task);

                                    var file = XFile.PathJoin(Constants.LocalPath, dependency);
                                    task.Request = AssetBundle.LoadFromFileAsync(file, 0, Constants.GetOffset(dependency));
                                    yield return task.Request;

                                    Loading.Remove(dependency);
                                    task.IsDone = true;
                                }
                                else yield return new WaitUntil(() => task.IsDone);

                                if (!Loaded.ContainsKey(dependency))
                                {
                                    if (task.Request.assetBundle == null)
                                    {
                                        XLog.Error("XAsset.Bundle.LoadAsync: async load depend bundle error: {0}.", dependency);
                                        breakDependency = i;
                                        break;
                                    }
                                    else
                                    {
                                        var bundle = new Bundle() { Name = dependency, Source = task.Request.assetBundle };
                                        Loaded.Add(dependency, bundle);
                                    }
                                }
                            }
                            if (handler != null) handler.doneCount++;
                        }

                        // 如果有任何一个依赖加载失败，则解除对已加载依赖的引用
                        if (breakDependency >= 0)
                        {
                            for (var i = 0; i < breakDependency; i++)
                            {
                                var dependency = dependencies[i];
                                if (Loaded.TryGetValue(dependency, out var dependBundle))
                                {
                                    if (dependBundle.Count == 0 && dependBundle.Source) // 只处理未被引用的 Bundle
                                    {
                                        dependBundle.Source.Unload(true);
                                        Loaded.Remove(dependency);
                                        XLog.Warn("XAsset.Bundle.LoadAsync: unload: {0} because of broken depend bundle: {1}.", dependency, dependencies[breakDependency]);
                                    }
                                }
                            }

                            yield break; // 不再执行后续流程
                        }
                    }

                    if (!Loaded.ContainsKey(name))
                    {
                        if (!Loading.TryGetValue(name, out var task))
                        {
                            task = new Task() { Name = name };
                            Loading.Add(name, task);

                            var file = XFile.PathJoin(Constants.LocalPath, name);
                            task.Request = AssetBundle.LoadFromFileAsync(file, 0, Constants.GetOffset(name));
                            yield return task.Request;

                            Loading.Remove(name);
                            task.IsDone = true;
                        }
                        else yield return new WaitUntil(() => task.IsDone);

                        if (task.Request.assetBundle == null)
                        {
                            XLog.Error("XAsset.Bundle.LoadAsync: async load main bundle error: {0}.", name);

                            // 如果主包加载失败，则解除对所有依赖的引用
                            if (dependencies != null && dependencies.Length > 0)
                            {
                                for (var i = 0; i < dependencies.Length; i++)
                                {
                                    var dependency = dependencies[i];
                                    if (Loaded.TryGetValue(dependency, out var dependBundle))
                                    {
                                        if (dependBundle.Count == 0 && dependBundle.Source) // 只处理未被引用的 Bundle
                                        {
                                            dependBundle.Source.Unload(true);
                                            Loaded.Remove(dependency);
                                            XLog.Warn("XAsset.Bundle.LoadAsync: unload: {0} because of broken main bundle: {1}.", dependency, name);
                                        }
                                    }
                                }
                            }

                            yield break; // 不再执行后续流程
                        }
                        else
                        {
                            if (!Loaded.ContainsKey(name))
                            {
                                var bundleInfo = new Bundle() { Name = name, Source = task.Request.assetBundle };
                                Loaded.Add(name, bundleInfo);
                            }
                        }
                    }

                    if (handler != null) handler.doneCount++;
                }
            }

            /// <summary>
            /// Find 在已加载的资源包中查找指定名称的资源包。
            /// </summary>
            /// <param name="name">要查找的资源包名称</param>
            /// <returns>找到的资源包，如果未找到则返回 null</returns>
            public static Bundle Find(string name)
            {
                Loaded.TryGetValue(name, out var info);
                return info;
            }
        }
    }
}
