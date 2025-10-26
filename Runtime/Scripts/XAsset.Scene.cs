// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EFramework.Unity.Utility;

namespace EFramework.Unity.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Scene 提供了 Unity 场景的加载与卸载，支持自动处理依赖资源的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持场景的加载与卸载
        /// - 自动处理资源依赖关系
        ///
        /// 使用手册
        ///
        /// 1. 同步加载
        /// - 功能说明：同步加载场景，在 `Bundle` 模式下会自动加载场景对应的资源包
        /// - 函数参数：
        ///   - `nameOrPath`：场景名称或完整路径（`string` 类型）
        ///   - `loadMode`：场景加载模式（`LoadSceneMode` 类型），默认为 `Single`
        /// - 使用示例：
        ///   <code>
        ///   XAsset.Scene.Load("Scenes/TestScene", LoadSceneMode.Single);
        ///   </code>
        ///
        /// 2. 异步加载
        /// - 功能说明：异步加载场景，适合加载大型场景，避免加载过程阻塞主线程
        /// - 函数参数：
        ///   - `nameOrPath`：场景名称或完整路径（`string` 类型）
        ///   - `callback`：场景加载完成后的回调函数（`Action` 类型）
        ///   - `loadMode`：场景加载模式（`LoadSceneMode` 类型），默认为 `Single`
        /// - 函数返回：用于跟踪加载进度的 `Handler` 对象
        /// - 使用示例：
        ///   <code>
        ///   XAsset.Scene.LoadAsync("Scenes/TestScene", () =>
        ///   {
        ///       Debug.Log("场景加载完成");
        ///   });
        ///   </code>
        ///
        /// 3. 卸载场景
        /// - 功能说明：卸载指定场景，在 `Bundle` 模式下会同时卸载场景对应的资源包
        /// - 函数参数：
        ///   - `nameOrPath`：场景名称或完整路径（`string` 类型）
        /// - 使用示例：
        ///   <code>
        ///   XAsset.Scene.Unload("Scenes/TestScene");
        ///   </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Scene
        {
            /// <summary>
            /// Task 是场景加载的任务，用于跟踪异步加载过程中的场景状态。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// Name 是场景的名称，用于标识和查找场景。
                /// </summary>
                internal string Name;

                /// <summary>
                /// Request 是 Unity 场景加载的异步操作对象。
                /// </summary>
                internal AsyncOperation Request;
            }

            /// <summary>
            /// Loading 记录当前正在加载的场景，用于防止重复加载同一场景。
            /// </summary>
            internal static readonly Dictionary<string, Task> Loading = new();

            /// <summary>
            /// Loaded 记录已加载的场景。
            /// </summary>
            internal static readonly List<string> Loaded = new();

#if UNITY_EDITOR
            [UnityEditor.InitializeOnLoadMethod]
#else
            [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
            /// <summary>
            /// OnInitialize 是资源系统的初始化方法，在编辑器或运行时自动调用，监听场景加载和卸载的回调，自动处理场景依赖资源的卸载。
            /// </summary>
            internal static void OnInitialize()
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    if (Constants.BundleMode)
                    {
                        if (!Loaded.Contains(scene.name)) Loaded.Add(scene.name);
                    }
                };
                SceneManager.sceneUnloaded += scene =>
                {
                    if (Constants.BundleMode)
                    {
                        if (Loaded.Contains(scene.name))
                        {
                            Unload(scene.name);
                            Loaded.Remove(scene.name);
                        }
                    }
                };
            }

            /// <summary>
            /// Load 同步加载场景。在 Bundle 模式下会自动加载场景对应的资源包。
            /// </summary>
            /// <param name="nameOrPath">场景名称或完整路径，支持从 Assets 目录下的相对路径加载</param>
            /// <param name="loadMode">场景加载模式：Single 会卸载当前场景，Additive 则保留当前场景</param>
            public static void Load(string nameOrPath, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                var sceneName = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                try { Event.Notify(EventType.OnPreLoadScene, sceneName); }
                catch (Exception e) { XLog.Panic(e); }

                try
                {
                    if (Constants.BundleMode && Bundle.Manifest)
                    {
                        string bundleName;
                        if (nameOrPath.Contains("/"))
                        {
                            if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                            if (!nameOrPath.EndsWith(".unity")) nameOrPath += ".unity";
                            bundleName = Constants.GetName(nameOrPath);
                        }
                        else bundleName = Constants.GetName($"Scenes/{sceneName}.unity");
                        var bundleInfo = Bundle.Load(bundleName);
                        if (bundleInfo == null) XLog.Error("XAsset.Scene.Load: can not load scene caused by nil scene bundle file.");
                        else
                        {
                            bundleInfo.Retain(Constants.DebugMode ? $"[Scene.Load: {nameOrPath}]" : "");
                            SceneManager.LoadScene(sceneName, loadMode);
                        }
                    }
                    else SceneManager.LoadScene(sceneName, loadMode);
                }
                catch (Exception e) { throw e; }
                finally
                {
                    try { Event.Notify(EventType.OnPostLoadScene, sceneName); }
                    catch (Exception e) { XLog.Panic(e); }
                }
            }

            /// <summary>
            /// LoadAsync 异步加载场景。适用于加载大型场景，避免加载过程阻塞主线程。
            /// </summary>
            /// <param name="nameOrPath">场景名称或完整路径</param>
            /// <param name="callback">场景加载完成后的回调函数</param>
            /// <param name="loadMode">场景加载模式：Single 或 Additive</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync(string nameOrPath, Action callback = null, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                var handler = new Handler();
                if (callback != null) handler.OnPostload += callback;
                XLoom.RunCoroutine(LoadAsync(nameOrPath, handler, loadMode));
                return handler;
            }

            /// <summary>
            /// LoadAsync 是场景异步加载的内部实现。
            /// 处理场景加载的具体流程：
            /// 1. 在Bundle模式下先加载场景资源包
            /// 2. 通过SceneManager加载场景
            /// 3. 处理加载回调和事件通知
            /// </summary>
            /// <param name="nameOrPath">场景名称或路径。</param>
            /// <param name="handler">场景加载处理程序。</param>
            /// <param name="loadMode">加载模式（单场景或多场景）。</param>
            /// <returns>协程。</returns>
            internal static IEnumerator LoadAsync(string nameOrPath, Handler handler, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                var sceneName = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                try { Event.Notify(EventType.OnPreLoadScene, sceneName); }
                catch (Exception e) { XLog.Panic(e); }

                if (Constants.BundleMode && Bundle.Manifest)
                {
                    handler.totalCount++; // Load任务
                    string bundleName;
                    if (nameOrPath.Contains("/"))
                    {
                        if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                        if (!nameOrPath.EndsWith(".unity")) nameOrPath += ".unity";
                        bundleName = Constants.GetName(nameOrPath);
                    }
                    else bundleName = Constants.GetName($"Scenes/{sceneName}.unity");
                    yield return XLoom.RunCoroutine(Bundle.LoadAsync(bundleName, handler));
                    var bundleInfo = Bundle.Find(bundleName);
                    if (bundleInfo != null)
                    {
                        if (!Loading.TryGetValue(sceneName, out var task))
                        {
                            bundleInfo.Retain(Constants.DebugMode ? $"[Scene.LoadAsync: {nameOrPath}]" : "");
                            var request = SceneManager.LoadSceneAsync(sceneName, loadMode);
                            task = new Task() { Name = sceneName, Request = request };
                            handler.Request = request;
                            handler.InvokePreload();
                            Loading.Add(sceneName, task);
                            yield return new WaitUntil(() => task.Request.isDone);
                            Loading.Remove(sceneName);
                            handler.doneCount++;
                            handler.InvokePostload();
                        }
                        else
                        {
                            handler.Request = task.Request;
                            handler.InvokePreload();
                            yield return new WaitUntil(() => task.Request.isDone);
                            handler.doneCount++;
                            handler.InvokePostload();
                        }
                    }
                    else
                    {
                        XLog.Error("XAsset.Scene.LoadAsync: async load error: {0}", sceneName);

                        // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                        handler.Error = true;
                        handler.InvokePostload();
                    }
                }
                else
                {
                    if (!Loading.TryGetValue(sceneName, out var task))
                    {
                        var request = SceneManager.LoadSceneAsync(sceneName, loadMode);
                        if (request != null)
                        {
                            task = new Task() { Name = sceneName, Request = request };
                            handler.Request = request;
                            handler.InvokePreload();
                            Loading.Add(sceneName, task);
                            yield return new WaitUntil(() => task.Request.isDone);
                            Loading.Remove(sceneName);
                            handler.InvokePostload();
                        }
                        else
                        {
                            // 等待下一帧进行错误处理，避免业务层未获取到 handler 的实例
                            yield return null;

                            // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                            handler.Error = true;
                            handler.InvokePostload();
                        }
                    }
                    else
                    {
                        handler.Request = task.Request;
                        handler.InvokePreload();
                        yield return new WaitUntil(() => task.Request.isDone);
                        handler.InvokePostload();
                    }
                }

                try { Event.Notify(EventType.OnPostLoadScene, sceneName); }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// Unload 卸载指定场景。在 Bundle 模式下会同时卸载场景对应的资源包。
            /// </summary>
            /// <param name="nameOrPath">要卸载的场景名称或路径</param>
            public static void Unload(string nameOrPath)
            {
                var sceneName = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                if (Constants.BundleMode && Bundle.Manifest)
                {
                    string bundleName;
                    if (nameOrPath.Contains("/"))
                    {
                        if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                        if (!nameOrPath.EndsWith(".unity")) nameOrPath += ".unity";
                        bundleName = Constants.GetName(nameOrPath);
                    }
                    else bundleName = Constants.GetName($"Scenes/{sceneName}.unity");

                    var bundleInfo = Bundle.Find(bundleName);
                    bundleInfo?.Release(Constants.DebugMode ? $"[Scene.Unload: {nameOrPath}]" : "");
                }
            }

            /// <summary>
            /// IsLoading 检查场景的加载状态。
            /// </summary>
            /// <param name="path">场景名称或路径</param>
            /// <returns>是否正在加载</returns>
            public static bool IsLoading(string nameOrPath)
            {
                if (string.IsNullOrEmpty(nameOrPath)) return false;
                else
                {
                    var sceneName = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                    return Loading.ContainsKey(sceneName);
                }
            }
        }
    }
}
