// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using EFramework.Unity.Utility;
using UnityEngine;

namespace EFramework.Unity.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Resource 提供了 Unity 资源的加载与卸载，支持自动处理依赖资源的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持资源的加载与卸载
        /// - 自动处理资源依赖关系
        ///
        /// 使用手册
        ///
        /// 1. 同步加载
        ///
        /// 1.1 加载资源
        /// - 功能说明：根据当前模式从 `Resources` 或 `AssetBundle` 中加载资源
        /// - 函数参数：
        ///   - `path`：资源路径（`string` 类型）
        ///   - `type`：资源类型（`Type` 类型）
        ///   - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
        /// - 函数返回：加载的资源对象（`UnityEngine.Object` 类型），加载失败时返回 `null`
        /// - 使用示例：
        /// ```csharp
        /// var asset = XAsset.Resource.Load("Example/Test.prefab", typeof(GameObject));
        /// ```
        ///
        /// 1.2 泛型加载
        /// - 功能说明：提供类型安全的资源加载方式
        /// - 函数参数：
        ///   - `path`：资源路径（`string` 类型）
        ///   - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
        /// - 函数返回：加载的资源对象（`T` 类型），加载失败时返回 `null`
        /// - 使用示例：
        /// ```csharp
        /// var asset = XAsset.Resource.Load&lt;GameObject&gt;("Example/Test.prefab");
        /// ```
        ///
        /// 2. 异步加载
        ///
        /// 2.1 加载资源
        /// - 功能说明：异步加载资源，适合加载大型资源
        /// - 函数参数：
        ///   - `path`：资源路径（`string` 类型）
        ///   - `type`：资源类型（`Type` 类型）
        ///   - `callback`：加载完成时的回调函数（`Callback` 类型）
        ///   - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
        /// - 函数返回：用于跟踪加载进度的 `Handler` 对象
        /// - 使用示例：
        /// ```csharp
        /// XAsset.Resource.LoadAsync("Example/Test.prefab", typeof(GameObject), (asset) =&gt;
        /// {
        ///     Debug.Log("加载完成：" + asset.name);
        /// });
        /// ```
        ///
        /// 2.2 泛型加载
        /// - 功能说明：提供类型安全的异步加载方式
        /// - 函数参数：
        ///   - `path`：资源路径（`string` 类型）
        ///   - `callback`：加载完成时的类型安全回调（`Action&lt;T&gt;` 类型）
        ///   - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
        /// - 函数返回：用于跟踪加载进度的 `Handler` 对象
        /// - 使用示例：
        /// ```csharp
        /// XAsset.Resource.LoadAsync&lt;GameObject&gt;("Example/Test.prefab", (asset) =&gt;
        /// {
        ///     Debug.Log("加载完成：" + asset.name);
        /// });
        /// ```
        ///
        /// 3. 卸载资源
        /// - 功能说明：卸载指定路径的资源
        /// - 函数参数：
        ///   - `path`：资源路径（`string` 类型）
        /// - 使用示例：
        /// ```csharp
        /// XAsset.Resource.Unload("Example/Test.prefab");
        /// ```
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Resource
        {
            /// <summary>
            /// Task 是异步加载的任务，用于跟踪单个资源的加载状态。
            /// 处理来自 Resources 和 AssetBundle 的异步加载请求。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// Name 是正在加载的资源路径。
                /// </summary>
                internal string Name;

                /// <summary>
                /// Request 是 Unity 的异步加载操作对象，可能是 ResourceRequest 或 AssetBundleRequest。
                /// </summary>
                internal AsyncOperation Request;
            }

            /// <summary>
            /// Loading 是当前正在进行的资源加载任务列表，用于处理并发加载请求，避免同一资源被重复加载。
            /// </summary>
            internal static readonly Dictionary<string, Task> Loading = new();

            /// <summary>
            /// Refer 用于跟踪资源（Prefab）实例（GameObject）的使用情况，确保资源依赖包被正确释放。
            /// </summary>
            internal class Refer : MonoBehaviour
            {
                /// <summary>
                /// Source 是资源在 Bundle 中的原始路径，用于定位资源包实例。
                /// </summary>
                [SerializeField]
                internal string Source;

                /// <summary>
                /// Label 是用于调试的对象标签，格式为"对象名@哈希码"。
                /// 即使对象被销毁也能保持唯一标识。
                /// </summary>
                internal string Label;

                /// <summary>
                /// GenLabel 用于构造用于调试的对象标签。
                /// </summary>
                internal void GenLabel() { Label = $"{name}@{GetHashCode()}"; }

                /// <summary>
                /// Awake 在 Unity 对象初始化时调用。
                /// </summary>
                internal void Awake()
                {
                    if (Constants.ReferMode)
                    {
                        GenLabel(); // 提前初始化标签，避免切换场景时因源实例销毁引起的空异常
                        var bundle = Bundle.Find(Source);
                        bundle?.Retain(Constants.DebugMode ? $"[Resource.Refer.Awake: {Label}]" : "");
                    }
                }

                /// <summary>
                /// OnDestroy 在 Unity 对象销毁时的调用。
                /// </summary>
                internal void OnDestroy()
                {
                    if (Constants.ReferMode)
                    {
                        var bundle = Bundle.Find(Source);
                        bundle?.Release(Constants.DebugMode ? $"[Resource.Refer.OnDestroy: {Label}]" : "");
                    }
                }

                /// <summary>
                /// Watch 监听指定游戏对象的生命周期，确保对象在被销毁前进行资源包释放。
                /// </summary>
                /// <param name="originalObject">要监听的游戏对象。</param>
                /// <param name="bundleName">资源包名称，用于定位资源包实例。</param>
                internal static Refer Watch(GameObject originalObject, string bundleName)
                {
                    if (originalObject && Constants.ReferMode)
                    {
                        if (!originalObject.TryGetComponent<Refer>(out var refer)) refer = originalObject.AddComponent<Refer>();
                        refer.Source = bundleName;
                        refer.GenLabel(); // 提前初始化标签，避免切换场景时因源实例销毁引起的空异常
                        return refer;
                    }
                    return null;
                }
            }

            /// <summary>
            /// Load 同步加载指定类型的资源。
            /// 根据当前模式和参数，从 Resources 目录或 AssetBundle 文件中加载资源。
            /// 在 Bundle 模式下会自动处理资源包的加载和依赖关系。
            /// </summary>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="type">要加载的资源类型</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <param name="retain">是否保留对 AssetBundle 的引用</param>
            /// <returns>加载的资源对象，加载失败时返回null</returns>
            public static UnityEngine.Object Load(string path, Type type, bool resource = false, bool retain = true)
            {
                try { Event.Notify(EventType.OnPreLoadResource, path); }
                catch (Exception e) { XLog.Panic(e); }
                UnityEngine.Object asset = null;
                try
                {
                    var resourceIndex = path.IndexOf("Resources/");
                    if (!Constants.BundleMode || resource || !Bundle.Manifest)
                    {
                        if (resourceIndex >= 0) path = path[(resourceIndex + 10)..];
                        asset = Resources.Load(path, type);
                    }
                    else
                    {
                        if (resourceIndex < 0) path = "Resources/" + path;
                        var lastPart = path.LastIndexOf("/");
                        var assetName = path[(lastPart + 1)..];
                        var bundleName = Constants.GetName(path);
                        var bundleInfo = Bundle.Load(bundleName);
                        if (bundleInfo != null)
                        {
                            asset = bundleInfo.Source.LoadAsset(assetName, type);
                            // 如果是自动引用模式且资源为游戏对象，则监控它的生命周期并自动引用与释放
                            if (asset is GameObject gameObject && Constants.ReferMode) Refer.Watch(gameObject, bundleName);
                            // 如果指示保留资源，则增加对该资源的引用计数并由业务层自行释放
                            if (retain) bundleInfo.Retain(Constants.DebugMode ? $"[Resource.Load: {path}]" : "");
                        }
                        else XLog.Error("XAsset.Resource.Load: sync load error: {0}", path);
                    }
                }
                catch (Exception e) { throw e; }
                finally
                {
                    try { Event.Notify(EventType.OnPostLoadResource, path); }
                    catch (Exception e) { XLog.Panic(e); }
                }
                return asset;
            }

            /// <summary>
            /// Load 同步加载指定类型的资源。
            /// 泛型版本的加载方法，提供更方便的类型安全的资源加载方式。
            /// </summary>
            /// <typeparam name="T">要加载的资源类型</typeparam>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <param name="retain">是否保留对 AssetBundle 的引用</param>
            /// <returns>加载的资源对象，加载失败时返回null</returns>
            public static T Load<T>(string path, bool resource = false, bool retain = true) where T : UnityEngine.Object { return Load(path, typeof(T), resource, retain) as T; }

            /// <summary>
            /// LoadAsync 异步加载资源。
            /// 提供非阻塞的资源加载方式，适合加载大型资源。
            /// 可以通过返回的Handler监控加载进度，并在加载完成时得到通知。
            /// </summary>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="type">要加载的资源类型</param>
            /// <param name="callback">资源加载完成时的回调函数</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <param name="retain">是否保留对 AssetBundle 的引用</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync(string path, Type type, Callback callback = null, bool resource = false, bool retain = true)
            {
                var handler = new Handler();
                XLoom.RunCoroutine(LoadAsync(path, type, callback, handler, resource, retain));
                return handler;
            }

            /// <summary>
            /// LoadAsync 异步加载指定类型的资源。
            /// 泛型版本的异步加载方法，提供类型安全的回调方式。
            /// </summary>
            /// <typeparam name="T">要加载的资源类型</typeparam>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="callback">资源加载完成时的类型安全回调</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <param name="retain">是否保留对 AssetBundle 的引用</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync<T>(string path, Action<T> callback = null, bool resource = false, bool retain = true) where T : UnityEngine.Object { return LoadAsync(path, typeof(T), (asset) => callback?.Invoke(asset as T), resource, retain); }

            /// <summary>
            /// LoadAsync 异步加载资源的内部实现。
            /// 处理实际的资源加载流程，包括依赖资源的加载、加载状态的跟踪和回调的触发。
            /// 
            /// 加载流程：
            /// 1. 根据当前模式选择加载方式（Resources/AssetBundle）
            /// 2. 处理并发加载请求，避免重复加载
            /// 3. 在 Bundle 模式下处理依赖关系
            /// 4. 触发加载完成事件和回调
            /// </summary>
            internal static IEnumerator LoadAsync(string path, Type type, Callback callback, Handler handler, bool resource = false, bool retain = true)
            {
                try { Event.Notify(EventType.OnPreLoadResource, path); }
                catch (Exception e) { XLog.Panic(e); }

                UnityEngine.Object asset = null;
                var resourceIndex = path.IndexOf("Resources/");
                if (!Constants.BundleMode || resource || !Bundle.Manifest)
                {
                    if (resourceIndex >= 0) path = path[(resourceIndex + 10)..];
                    if (!Loading.TryGetValue(path, out var task))
                    {
                        var request = Resources.LoadAsync(path, type);
                        task = new Task() { Name = path, Request = request };
                        handler.Request = request;
                        handler.InvokePreload();
                        Loading.Add(path, task);
                        yield return new WaitUntil(() => task.Request.isDone);
                        Loading.Remove(path);
                    }
                    else
                    {
                        handler.Request = task.Request;
                        handler.InvokePreload();
                        yield return new WaitUntil(() => task.Request.isDone);
                    }
                    asset = (task.Request as ResourceRequest).asset;

                    // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                    handler.Error = asset == null;
                    handler.InvokePostload();
                }
                else
                {
                    handler.totalCount++; // Load任务
                    if (resourceIndex < 0) path = "Resources/" + path;
                    var lastPart = path.LastIndexOf("/");
                    var assetName = path[(lastPart + 1)..];
                    var bundleName = Constants.GetName(path);
                    yield return XLoom.RunCoroutine(Bundle.LoadAsync(bundleName, handler));
                    var bundleInfo = Bundle.Find(bundleName);
                    if (bundleInfo != null)
                    {
                        if (!Loading.TryGetValue(path, out var task))
                        {
                            var request = bundleInfo.Source.LoadAssetAsync(assetName, type);
                            task = new Task() { Name = path, Request = request };
                            handler.Request = request;
                            handler.InvokePreload();
                            Loading.Add(path, task);
                            yield return request;
                            asset = request.asset;
                            Loading.Remove(path);
                            handler.doneCount++;
                            // 如果是自动引用模式且资源为游戏对象，则监控它的生命周期并自动引用与释放
                            if (asset is GameObject gameObject && Constants.ReferMode) Refer.Watch(gameObject, bundleName);
                            // 如果指示保留资源，则增加对该资源的引用计数并由业务层自行释放
                            if (retain) bundleInfo.Retain(Constants.DebugMode ? $"[Resource.LoadAsync.1: {path}]" : "");
                            handler.InvokePostload();
                        }
                        else
                        {
                            handler.Request = task.Request;
                            handler.InvokePreload();
                            yield return new WaitUntil(() => task.Request.isDone);
                            handler.doneCount++;
                            // 如果是自动引用模式且资源为游戏对象，则监控它的生命周期并自动引用与释放
                            if (asset is GameObject gameObject && Constants.ReferMode) Refer.Watch(gameObject, bundleName);
                            // 如果指示保留资源，则增加对该资源的引用计数并由业务层自行释放
                            if (retain) bundleInfo.Retain(Constants.DebugMode ? $"[Resource.LoadAsync.2: {path}]" : "");
                            handler.InvokePostload();
                        }
                        asset = (task.Request as AssetBundleRequest).asset;
                    }
                    else
                    {
                        XLog.Error("XAsset.Resource.LoadAsync: async load error: {0}", path);

                        // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                        handler.Error = true;
                        handler.InvokePostload();
                    }
                }

                try { Event.Notify(EventType.OnPostLoadResource, path); }
                catch (Exception e) { XLog.Panic(e); }

                try { callback?.Invoke(asset); }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// Unload 卸载指定路径的资源。
            /// 在 Bundle 模式下，会卸载对应的资源包。
            /// 注意：这个操作可能会影响到共享同一资源包的其他资源。
            /// </summary>
            /// <param name="path">要卸载的资源路径</param>
            public static void Unload(string path)
            {
                if (Constants.BundleMode && Bundle.Manifest)
                {
                    var resourceIndex = path.IndexOf("Resources/");
                    if (resourceIndex < 0) path = "Resources/" + path;
                    var bundleName = Constants.GetName(path);
                    var bundleInfo = Bundle.Find(bundleName);
                    bundleInfo?.Release(Constants.DebugMode ? $"[Resource.Unload: {path}]" : "");
                }
            }

            /// <summary>
            /// IsLoading 检查资源的加载状态。
            /// </summary>
            /// <param name="path">资源路径</param>
            /// <returns>是否正在加载</returns>
            public static bool IsLoading(string path)
            {
                if (string.IsNullOrEmpty(path)) return false;
                else return Loading.ContainsKey(path);
            }
        }
    }
}
