// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using EFramework.Unity.Asset;

/// <summary>
/// TestXAssetScene 是 XAsset.Scene 的单元测试。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuilder))]
public class TestXAssetScene
{
    internal const string TestScene = "Assets/Tests/Runtime/Scenes/TestScene";

    [OneTimeSetUp]
    public void Init()
    {
        XAsset.Constants.bBundleMode = true;
        XAsset.Constants.bundleMode = true;
        XAsset.Bundle.Initialize();

        var scenes = EditorBuildSettings.scenes.ToList();
        var scene = new EditorBuildSettingsScene
        {
            guid = new GUID(AssetDatabase.AssetPathToGUID(TestScene)),
            path = TestScene,
            enabled = true
        };
        scenes.Add(scene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        XAsset.Bundle.Loaded.Clear();
        XAsset.Constants.bBundleMode = false;
        XAsset.Bundle.Initialize();

        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(ele => ele.path == TestScene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [UnityTest]
    public IEnumerator Load()
    {
        LogAssert.ignoreFailingMessages = true;

        string[] sceneNames = { TestScene, "NotExist" };
        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            XAsset.Constants.bBundleMode = true;
            XAsset.Constants.bundleMode = bundleMode;

            foreach (var sceneName in sceneNames)
            {
                var name = Path.GetFileNameWithoutExtension(sceneName);
                XAsset.Scene.Load(sceneName, LoadSceneMode.Additive);
                yield return null; // 允许 Unity 场景系统刷新

                var isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == name && scene.isLoaded)
                    {
                        isLoaded = true;
                        break;
                    }
                }
                if (sceneName == "NotExist") Assert.IsFalse(isLoaded, "加载不存在的场景应当失败。");
                else Assert.IsTrue(isLoaded, "加载存在的场景应当成功。");
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        LogAssert.ignoreFailingMessages = true;

        string[] sceneNames = { TestScene, "NotExist" };
        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            XAsset.Constants.bBundleMode = true;
            XAsset.Constants.bundleMode = bundleMode;

            foreach (var sceneName in sceneNames)
            {
                var name = Path.GetFileNameWithoutExtension(sceneName);
                var handler = XAsset.Scene.LoadAsync(sceneName, callback: null, LoadSceneMode.Additive);
                yield return handler; // 等待 Unity 场景系统刷新

                var isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == name && scene.isLoaded)
                    {
                        isLoaded = true;
                        break;
                    }
                }
                if (sceneName == "NotExist")
                {
                    Assert.IsFalse(isLoaded, "加载不存在的场景应当失败。");
                    Assert.IsTrue(handler.Error, "加载不存在的场景 handler.Error 应当为 true。");
                }
                else
                {
                    Assert.IsTrue(isLoaded, "加载存在的场景应当成功。");
                    Assert.IsFalse(handler.Error, "加载存在的场景 handler.Error 应当为 false。");
                    Assert.AreEqual(1f, handler.Progress, "加载存在的场景 handler.Progress 应当为 1f。");
                }
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void IsLoading()
    {
        XAsset.Scene.Loading.Clear();

        XAsset.Scene.Loading.Add("TestIsLoading", new XAsset.Scene.Task());
        Assert.IsTrue(XAsset.Scene.IsLoading("TestIsLoading"), "应当返回正在加载。");
        Assert.IsFalse(XAsset.Scene.IsLoading(null), "应当返回未正在加载。");
        Assert.IsFalse(XAsset.Scene.IsLoading(string.Empty), "应当返回未正在加载。");
        Assert.IsFalse(XAsset.Scene.IsLoading("Invalid"), "应当返回未正在加载。");

        XAsset.Scene.Loading.Clear();
    }
}
