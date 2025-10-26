// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using EFramework.Unity.Utility;
using static EFramework.Unity.Asset.XAsset;

/// <summary>
/// TestXAssetConstants 是 XAsset.Constants 的单元测试。
/// </summary>
public class TestXAssetConstants
{
    [TestCase("Assets/Textures/MyTexture")]     // 正常路径
    [TestCase("Packages\\Test\\MyTexture.png")]   // 包含反斜杠
    [TestCase("Assets/Scenes/MyScene.unity")]   // 场景资源
    [TestCase("")]
    [TestCase(null)]
    public void Name(string path)
    {
        // Arrange
        // 清空缓存以确保测试的准确性
        Constants.nameCache.Clear();

        // Act
        var name = Constants.GetName(path);
        var expected = string.Empty;
        if (!string.IsNullOrEmpty(path))
        {
            var extension = System.IO.Path.GetExtension(path);
            if (path.StartsWith("Assets/")) path = path["Assets/".Length..];
            if (!string.IsNullOrEmpty(extension) && extension != ".unity") // 场景文件只能单独打包
            {
                path = path.Replace(extension, "");
            }
            expected = XFile.NormalizePath(path).ToLower().MD5() + Constants.Extension;
        }

        // Assert
        Assert.AreEqual(expected, name, "生成的标签应符合预期格式。");
        if (!string.IsNullOrEmpty(path)) Assert.IsTrue(Constants.nameCache.ContainsKey(path), "资源路径在首次调用后应被缓存。");
    }

    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    public void Mode(bool bundleMode, bool referMode, bool debugMode)
    {
        Constants.bundleMode = bundleMode;
        Constants.referMode = referMode;
        Constants.debugMode = debugMode;

        // Assert
        Assert.AreEqual(Constants.bundleMode, bundleMode, "当在偏好设置中设置时，BundleMode 应为 true。");
        Assert.AreEqual(Constants.referMode, referMode, "当 BundleMode 和 ReferMode 在偏好设置中都设置时，ReferMode 应为 true。");
        Assert.AreEqual(Constants.debugMode, debugMode, "当 BundleMode 和 DebugMode 在偏好设置中都设置时，DebugMode 应为 true。");
    }

    [Test]
    public void Path()
    {
        // Arrange
        var expectedLocalPath = XFile.PathJoin(XEnv.LocalPath, XPrefs.GetString(Preferences.LocalUri, Preferences.LocalUriDefault));

        // Act
        var actualLocalPath = Constants.LocalPath;

        // Assert
        Assert.AreEqual(expectedLocalPath, actualLocalPath, "LocalPath 应与预期路径匹配。");
        Assert.IsTrue(Constants.bLocalPath, "获取 LocalPath后，bLocalPath 标志应被设置。");
    }
}
