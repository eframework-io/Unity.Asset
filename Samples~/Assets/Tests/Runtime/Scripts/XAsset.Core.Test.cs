// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using static EFramework.Unity.Asset.XAsset;

/// <summary>
/// TestXAssetCore 是 XAsset.Core 的单元测试。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuilder))]
public class TestXAssetCore
{
    [OneTimeSetUp]
    public void Init()
    {
        Constants.bBundleMode = true;
        Constants.bundleMode = true;
        Bundle.Initialize();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Constants.bBundleMode = false;
        Bundle.Initialize();
    }

    [UnityTest]
    public IEnumerator Handler()
    {
        LogAssert.ignoreFailingMessages = true;

        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            Constants.bBundleMode = true;
            Constants.bundleMode = bundleMode;

            // 测试Progress
            var handler = new Handler
            {
                totalCount = 5,
                doneCount = 2
            };
            var progress = handler.Progress;
            Assert.That(progress, Is.EqualTo(0.4f), "Progress应该被正确计算。");

            // 测试IsDone
            handler = Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject));
            Assert.That(handler.IsDone, Is.False, "当Operation未完成时，IsDone应为false。");
            yield return handler;
            Assert.That(handler.IsDone, Is.True, "当Operation完成时，IsDone应为true。");

            handler = Resource.LoadAsync("NotExist", typeof(GameObject));
            yield return handler;
            Assert.That(handler.Error, Is.True, "当加载不存在的资源时，Error应为true。");

            // 测试MoveNext
            handler = Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject));
            Assert.That(handler.MoveNext(), Is.Not.EqualTo(handler.IsDone), "当 Operation 不为空时，MoveNext 应为 true。");
            yield return handler;

            // 测试Preload 和 Postload
            handler = new Handler();
            bool preloadWasCalled = false;
            bool postloadWasCalled = false;
            handler.OnPreload += () => preloadWasCalled = true;
            handler.OnPostload += () => postloadWasCalled = true;
            yield return Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject), null, handler);
            Assert.That(preloadWasCalled, Is.True, "OnPreload 事件应被调用。");
            Assert.That(postloadWasCalled, Is.True, "OnPostload 事件应被调用。");

            // 测试Reset
            handler.Reset();
            Assert.That(handler.doneCount, Is.EqualTo(0), "doneCount 应重置为 0。");
            Assert.That(handler.totalCount, Is.EqualTo(0), "totalCount 应重置为 0。");
            Assert.That(handler.Request, Is.Null, "Request 应重置为 null。");
            preloadWasCalled = false;
            postloadWasCalled = false;
            handler.InvokePreload();
            handler.InvokePostload();
            Assert.That(preloadWasCalled, Is.False, "OnPreload 事件不应被调用。");
            Assert.That(postloadWasCalled, Is.False, "OnPostload 事件不应被调用。");
        }

        LogAssert.ignoreFailingMessages = false;
    }
}
