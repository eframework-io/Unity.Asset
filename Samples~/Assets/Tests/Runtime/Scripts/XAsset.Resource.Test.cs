// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static EFramework.Unity.Asset.XAsset;

/// <summary>
/// TestXAssetResource 是 XAsset.Resource 的单元测试。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuilder))]
public class TestXAssetResource
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

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Load(bool bundleMode, bool referMode)
    {
        LogAssert.ignoreFailingMessages = true;

        Constants.bBundleMode = true;
        Constants.bundleMode = bundleMode;

        Constants.bReferMode = true;
        Constants.referMode = referMode;

        // Arrange
        var assetPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
        var notExistPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/NotExist";
        var bundleName = Constants.GetName(assetPath);

        // 非泛型加载
        var asset1 = Resource.Load(assetPath, typeof(GameObject), retain: true) as GameObject;
        Assert.That(asset1, Is.Not.Null, "加载的资产不应为空。");
        Assert.That(asset1, Is.InstanceOf<GameObject>(), "加载的资产应为 GameObject 类型。");
        if (bundleMode)
        {
            if (referMode) Assert.That(asset1.GetComponent<Resource.Refer>(), Is.Not.Null, "引用模式下 GameObject 实例上的 Resource.Refer 对象不应当为空。");
            // 使用 AssetBundle 模式加载时，Resource.Refer 组件会在源实例上保持，此处非 refer 模式下不作验证。
            // else Assert.That(asset1.GetComponent<Resource.Refer>(), Is.Null, "非引用模式下 GameObject 实例上的 Resource.Refer 对象应当为空。");

            var bundleInfo = Bundle.Find(bundleName);
            Assert.That(bundleInfo.Count, Is.EqualTo(1), "retain = true 时引用计数应当为 1。");

            // 卸载
            Assert.DoesNotThrow(() => Resource.Unload(assetPath));
            Assert.That(bundleInfo.Count, Is.EqualTo(0), "资源卸载后的引用计数应当仍为 0。");
        }

        // 泛型加载
        var asset2 = Resource.Load<GameObject>(assetPath, retain: false);
        Assert.That(asset2, Is.Not.Null, "加载的资产不应为空。");
        Assert.That(asset2, Is.InstanceOf<GameObject>(), "加载的资产应为 GameObject 类型。");
        if (bundleMode)
        {
            var bundleInfo = Bundle.Find(bundleName);
            Assert.That(bundleInfo.Count, Is.EqualTo(0), "retain = false 时引用计数应当为 0。");
        }

        var notExistAsset = Resource.Load(notExistPath, typeof(GameObject));
        Assert.That(notExistAsset, Is.Null, "加载的资产应为空。");

        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        LogAssert.ignoreFailingMessages = true;

        bool[] bundleModes = { true, false };
        bool[] referModes = { true, false };

        foreach (var bundleMode in bundleModes)
        {
            Constants.bBundleMode = false;
            Constants.bundleMode = bundleMode;

            foreach (var referMode in referModes)
            {
                Constants.bReferMode = false;
                Constants.referMode = referMode;

                // Arrange
                var assetPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
                var notExistPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/NotExist";
                var bundleName = Constants.GetName(assetPath);

                // Act
                var handler1 = Resource.LoadAsync(assetPath, typeof(GameObject), origin =>
                {
                    var asset = origin as GameObject;
                    Assert.That(asset, Is.Not.Null, "加载的资产不应为空。");
                    Assert.That(asset, Is.InstanceOf<GameObject>(), "加载的资产应为 GameObject 类型。");
                    if (bundleMode)
                    {
                        if (referMode) Assert.That(asset.GetComponent<Resource.Refer>(), Is.Not.Null, "引用模式下 GameObject 实例上的 Resource.Refer 对象不应当为空。");
                        // 使用 AssetBundle 模式加载时，Resource.Refer 组件会在源实例上保持，此处非 refer 模式下不作验证。
                        // else Assert.That(asset.GetComponent<Resource.Refer>(), Is.Null, "非引用模式下 GameObject 实例上的 Resource.Refer 对象应当为空。");

                        var bundleInfo = Bundle.Find(bundleName);
                        Assert.That(bundleInfo.Count, Is.EqualTo(1), "retain = true 时引用计数应当为 1。");

                        // 卸载
                        Assert.DoesNotThrow(() => Resource.Unload(assetPath));
                        Assert.That(bundleInfo.Count, Is.EqualTo(0), "资源卸载后的引用计数应当仍为 0。");
                    }
                }, retain: true);
                yield return handler1;

                // 测试泛型加载
                var handler2 = Resource.LoadAsync<GameObject>(assetPath, asset =>
                {
                    Assert.That(asset, Is.Not.Null, "加载的资产不应为空。");
                    Assert.That(asset, Is.InstanceOf<GameObject>(), "加载的资产应为 GameObject 类型。");
                    if (bundleMode)
                    {
                        var bundleInfo = Bundle.Find(bundleName);
                        Assert.That(bundleInfo.Count, Is.EqualTo(0), "retain = false 时引用计数应当为 0。");
                    }
                }, retain: false);
                yield return handler2;

                var handler3 = Resource.LoadAsync(notExistPath, typeof(GameObject), asset =>
                {
                    Assert.That(asset, Is.Null, "加载的资产应为空。");
                });
                yield return handler3;
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void IsLoading()
    {
        Resource.Loading.Clear();

        Resource.Loading.Add("TestIsLoading", new Resource.Task());
        Assert.That(Resource.IsLoading("TestIsLoading"), Is.True, "应当返回正在加载。");
        Assert.That(Resource.IsLoading(null), Is.False, "应当返回未正在加载。");
        Assert.That(Resource.IsLoading(string.Empty), Is.False, "应当返回未正在加载。");
        Assert.That(Resource.IsLoading("Invalid"), Is.False, "应当返回未正在加载。");

        Resource.Loading.Clear();
    }
}
