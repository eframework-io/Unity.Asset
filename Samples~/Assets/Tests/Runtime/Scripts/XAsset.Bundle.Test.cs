// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EFramework.Unity.Asset;
using static EFramework.Unity.Asset.XAsset;

/// <summary>
/// TestXAssetBundle 是 XAsset.Bundle 的单元测试。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuilder))]
public class TestXAssetBundle
{
    [SetUp]
    public void Setup()
    {
        Constants.bBundleMode = true;
        Constants.bundleMode = true;
        Bundle.Initialize();
    }

    [TearDown]
    public void Reset()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Constants.bBundleMode = false;
        Bundle.Initialize();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Initialize(bool bundleMode)
    {
        Constants.bBundleMode = true;
        Constants.bundleMode = bundleMode;
        Bundle.Initialize();
        if (bundleMode) Assert.That(Bundle.Manifest, Is.Not.Null, "Bundle 模式下 Manifest 应该被加载且不为空。");
        else Assert.That(Bundle.Manifest, Is.Null, "非 Bundle 模式下 Manifest 应保持为空。");
    }

    [Test]
    public void Retain()
    {
        // Arrange
        var bundle = new Bundle { Name = "TestBundle", Count = 0 };
        Bundle.Loaded.Add(bundle.Name, bundle);

        // Act
        int count1 = bundle.Retain();
        int count2 = bundle.Retain();

        // Assert
        Assert.That(count1, Is.EqualTo(1), "Retain 应该增加引用计数。");
        Assert.That(count2, Is.EqualTo(2), "Retain 应该增加引用计数。");
    }

    [Test]
    public void Release()
    {
        // Arrange
        AssetBundle assetBundle = null;
        var bundleName = Constants.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        var bundle = Bundle.Load(bundleName);
        XAsset.Event.Register(XAsset.EventType.OnPostUnloadBundle, (AssetBundle ab) => { assetBundle = ab; });

        // Act
        bundle.Retain();
        int count = bundle.Release();

        // Assert
        Assert.That(count, Is.EqualTo(0), "Release应该减少引用计数。");
        Assert.That(Bundle.Loaded.ContainsKey(bundle.Name), Is.False, "当计数为零时，Bundle 应该从 Loaded 中移除。");
        Assert.That(assetBundle, Is.Not.Null, "当 bundle 被释放时，应该通知事件。");
    }

    [Test]
    public void Load()
    {
        // Act
        var bundleName = Constants.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        // 检查初始状态
        Assert.That(Bundle.Find(bundleName), Is.Null, "初始状态下 Bundle 不应该被加载。");
        var bundle = Bundle.Load(bundleName);

        // 测试加载不存在的bundle
        LogAssert.Expect(LogType.Error, new Regex(".*Unable to open archive.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*Failed to read data for the AssetBundle.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*sync load main bundle error.*"));
        var noneBundleName = "non/existent/bundle";
        var noneBundle = Bundle.Load(noneBundleName);

        // Assert 
        Assert.That(bundle, Is.Not.Null, "加载的 bundle 不应为空。");
        Assert.That(bundleName, Is.EqualTo(bundle.Name), "加载的 bundle 名称应匹配。");
        Assert.That(noneBundle, Is.Null, "加载不存在的 bundle 应返回null。");
        Assert.That(Bundle.Loaded.ContainsKey(noneBundleName), Is.False, "不存在的 bundle 不应被添加到 Loaded 字典中。");
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        // Act
        var bundleName = Constants.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        yield return Bundle.LoadAsync(bundleName);
        var bundle = Bundle.Find(bundleName);
        Assert.That(bundle, Is.Not.Null, "加载的 bundle 不应为空。");
        Assert.That(bundleName, Is.EqualTo(bundle.Name), "加载的 bundle 名称应匹配。");
    }

    [UnityTest]
    public IEnumerator LoadConcurrent()
    {
        var bundleName = Constants.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        for (var i = 0; i < 100; i++)
        {
            Setup();

            var iter1 = Bundle.LoadAsync(bundleName);
            iter1.MoveNext(); // 进入异步加载队列

            var iter2 = Bundle.LoadAsync(bundleName);
            iter2.MoveNext();

            var iter3 = Bundle.LoadAsync(bundleName);
            iter3.MoveNext();

            var bundle = Bundle.Load(bundleName);

            yield return iter1;
            yield return iter2;
            yield return iter3;

            Assert.That(bundle, Is.Not.Null, "加载的 bundle 不应为空。");
            Assert.That(bundleName, Is.EqualTo(bundle.Name), "加载的 bundle 名称应匹配。");

            Reset();
        }
    }

    [Test]
    public void Find()
    {
        Assert.That(Bundle.Find("TestBundle"), Is.Null);

        var bundle = new Bundle { Name = "TestBundle", Count = 1 };
        Bundle.Loaded.Add(bundle.Name, bundle);

        Assert.That(Bundle.Find("TestBundle"), Is.Not.Null);
    }
}
