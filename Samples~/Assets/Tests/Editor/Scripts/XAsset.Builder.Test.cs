// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using EFramework.Unity.Utility;
using EFramework.Unity.Editor;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
using EFramework.Unity.Asset;
using static EFramework.Unity.Asset.Editor.XAsset;

/// <summary>
/// TestXAssetBuilder 是 XAsset.Builder 的单元测试。
/// </summary>
public class TestXAssetBuilder : IPrebuildSetup
{
    void IPrebuildSetup.Setup()
    {
        // 创建处理器
        var handler = new Builder() { ID = "Test/Build Test Assets" };

        // 使用包含路径
        var include = new string[] { "Assets/Tests/Runtime/Resources/Bundle", "Assets/Tests/Runtime/Scenes" };

        XPrefs.Asset.Set(Builder.Preferences.MergeSingle, false);
        XPrefs.Asset.Set(Builder.Preferences.MergeMaterial, true);
        XPrefs.Asset.Set(Builder.Preferences.Include, include);
        XPrefs.Asset.Set(Builder.Preferences.Exclude, new string[] { });

        var buildDir = XFile.NormalizePath(XPrefs.GetString(Builder.Preferences.Output, Builder.Preferences.OutputDefault).Eval(XEnv.Instance));
        var manifestFile = XFile.PathJoin(buildDir, XMani.Default);

        var report = XEditor.Tasks.Execute(handler);

        Assert.That(report.Result == XEditor.Tasks.Result.Succeeded, "资源构建应当成功。");
        Assert.That(XFile.HasFile(manifestFile), "资源清单应当生成成功。");

        var manifest = new XMani.Manifest();
        Assert.That(manifest.Read(manifestFile)(), "资源清单应当读取成功。");

        foreach (var file in manifest.Files)
        {
            var path = XFile.PathJoin(buildDir, file.Name);
            Assert.That(XFile.HasFile(path), "文件应当存在于本地：" + file.Name);
            Assert.That(XFile.FileMD5(path), Is.EqualTo(file.MD5), "文件MD5应当一致：" + file.Name);
            Assert.That(XFile.FileSize(path), Is.EqualTo(file.Size), "文件大小应当一致：" + file.Name);
        }

        // 复制资源到本地
        if (XFile.HasDirectory(XAsset.Constants.LocalPath)) XFile.DeleteDirectory(XAsset.Constants.LocalPath);
        XFile.CopyDirectory(buildDir, XAsset.Constants.LocalPath);
        Assert.That(XFile.HasDirectory(XAsset.Constants.LocalPath), "本地目录应当存在。");
    }

    [Test]
    public void Process() { }

    [TestCase(false, false, TestName = "不合并单包_不合并材质")]
    [TestCase(false, true, TestName = "不合并单包_合并材质")]
    [TestCase(true, true, TestName = "合并单包_合并材质")]
    [TestCase(true, false, TestName = "合并单包_不合并材质")]
    public void Merge(bool mergeSingle, bool mergeMaterial)
    {
        // 使用固定的包含路径
        var include = new string[] { "Assets/Tests/Runtime/Resources/Bundle", "Assets/Tests/Runtime/Scenes" };

        XPrefs.Asset.Set(Builder.Preferences.MergeSingle, mergeSingle);
        XPrefs.Asset.Set(Builder.Preferences.MergeMaterial, mergeMaterial);
        XPrefs.Asset.Set(Builder.Preferences.Include, include);
        XPrefs.Asset.Set(Builder.Preferences.Exclude, new string[] { });

        // 执行依赖关系生成
        var dependency = Builder.GenDependency();

        // 验证依赖关系生成结果
        Assert.That(dependency, Is.Not.Null, "依赖关系字典不应为空。");

        // 验证MergeSingle选项的效果
        var hasTestSingleBundle = dependency.Values.Any(k => k.Exists(e => e.Contains("TestSingle")));
        if (mergeSingle) Assert.That(hasTestSingleBundle, Is.False, "启用 MergeSingle 时，TestSingle 资源不应有独立 Bundle。");
        else Assert.That(hasTestSingleBundle, Is.True, "未启用 MergeSingle 时，TestSingle 资源应有独立 Bundle。");

        // 验证MergeMaterial选项的效果
        var hasMaterialBundle = false;
        foreach (var kvp in dependency)
        {
            foreach (var asset in kvp.Value)
            {
                if (asset.Contains("TestSceneMat1.mat") || asset.Contains("TestSceneMat2.mat"))
                {
                    hasMaterialBundle = true;
                    break;
                }
            }
            if (hasMaterialBundle) break;
        }
        if (mergeMaterial) Assert.That(hasMaterialBundle, Is.False, "启用 MergeMaterial 时，材质不应有独立 Bundle。");
        else Assert.That(hasMaterialBundle, Is.True, "未启用 MergeMaterial 时，材质应有独立 Bundle。");
    }

    [TestCase(new string[] { "Assets/Tests/Runtime/Resources/Bundle/", "Assets/Tests/Runtime/Scenes/TestScene.unity" }, new string[] { }, TestName = "路径包含_无排除")]
    [TestCase(new string[] { "Assets/Tests/Runtime/Resources/Bundle/", "Assets/Tests/Runtime/Scenes/TestScene.unity" }, new string[] { "Assets/Tests/Runtime/Scenes/TestScene.unity" }, TestName = "路径包含_路径排除")]
    [TestCase(new string[] { "Assets/Tests/Runtime/Resources/**/*", "Assets/Tests/Runtime/Scenes/**/*.unity" }, new string[] { }, TestName = "通配包含_无排除")]
    [TestCase(new string[] { "Assets/Tests/Runtime/Resources/**/*", "Assets/Tests/Runtime/Scenes/**/*.unity" }, new string[] { "Assets/Tests/Runtime/Resources/**/*", "Assets/Tests/Runtime/Scenes/TestScene2.unity" }, TestName = "通配包含_通配排除")]
    public void Exclude(string[] include, string[] exclude)
    {
        // 使用固定的合并选项
        XPrefs.Asset.Set(Builder.Preferences.MergeSingle, false);
        XPrefs.Asset.Set(Builder.Preferences.MergeMaterial, false);
        XPrefs.Asset.Set(Builder.Preferences.Include, include);
        XPrefs.Asset.Set(Builder.Preferences.Exclude, exclude);

        // 执行依赖关系生成
        var dependency = Builder.GenDependency();

        // 验证依赖关系生成结果
        Assert.That(dependency, Is.Not.Null, "依赖关系字典不应为空。");

        // 验证Include选项的效果
        if (include.Length > 0)
        {
            // 验证包含规则是否正确应用
            var allIncluded = true;
            foreach (var inc in include)
            {
                if (inc.Contains("*") || inc.Contains("?") || inc.Contains("["))
                {
                    // 对于特定的通配符模式进行验证
                    if (inc.Contains("*.unity"))
                    {
                        var hasUnityBundle = dependency.Values.Any(k => k.Exists(e => e.EndsWith(".unity")));
                        Assert.That(hasUnityBundle, Is.True, "应包含场景Bundle。");
                    }
                    continue;
                }

                if (XFile.HasFile(inc))
                {
                    // 单个文件
                    var found = false;
                    foreach (var kvp in dependency)
                    {
                        if (kvp.Value.Any(v => v.Contains(Path.GetFileName(inc))))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found && !exclude.Any(e => e.Contains(Path.GetFileName(inc))))
                    {
                        allIncluded = false;
                    }
                }
            }
            Assert.That(allIncluded, Is.True, "所有指定资源都被包含。");
        }

        // 验证Exclude选项的效果
        if (exclude.Length > 0)
        {
            // 验证排除的资源是否真的被排除
            foreach (var exc in exclude)
            {
                if (exc.Contains("*") || exc.Contains("?") || exc.Contains("["))
                {
                    // 对于特定的通配符模式进行验证
                    if (exc.Contains("*.unity"))
                    {
                        // 检查是否有匹配的unity文件被包含
                        var unityPattern = exc.Replace("*.unity", "");
                        var hasUnityFile = false;

                        foreach (var kvp in dependency)
                        {
                            foreach (var v in kvp.Value)
                            {
                                if (v.Contains(unityPattern) && v.EndsWith(".unity"))
                                {
                                    hasUnityFile = true;
                                    break;
                                }
                            }
                            if (hasUnityFile) break;
                        }
                        Assert.That(hasUnityFile, Is.False, $"通配符 {exc} 匹配的场景文件应被排除。");
                    }
                    continue;
                }

                var isExcluded = true;

                if (XFile.HasFile(exc))
                {
                    // 单个文件
                    foreach (var kvp in dependency)
                    {
                        if (kvp.Value.Any(v => v.Contains(Path.GetFileName(exc))))
                        {
                            isExcluded = false;
                            break;
                        }
                    }
                    Assert.That(isExcluded, Is.True, $"文件 {Path.GetFileName(exc)} 应被排除。");
                }
                else if (XFile.HasDirectory(exc))
                {
                    // 目录
                    foreach (var kvp in dependency)
                    {
                        foreach (var v in kvp.Value)
                        {
                            if (v.Contains(exc))
                            {
                                isExcluded = false;
                                break;
                            }
                        }
                        if (!isExcluded) break;
                    }
                    Assert.That(isExcluded, Is.True, $"目录 {exc} 中的资源应被排除。");
                }
            }
        }
    }

    [Test]
    public void Stash()
    {
        // 准备测试目录和文件
        var testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXAssetBuilder");

        var testStashDir = XFile.PathJoin(testDir, "Stash");
        if (!XFile.HasDirectory(testStashDir)) XFile.CreateDirectory(testStashDir);
        var testStashDirMeta = XFile.PathJoin(testDir, "Stash.meta");
        XFile.SaveText(testStashDirMeta, "Test meta dir");

        var testStashFile = XFile.PathJoin(testStashDir, "TestStashFile.txt");
        XFile.SaveText(testStashFile, "Test content for stash function");
        var testStashFileMeta = XFile.PathJoin(testStashDir, "TestStashFile.txt.meta");
        XFile.SaveText(testStashFileMeta, "Test meta content");

        XPrefs.Asset.Set(Builder.Preferences.Stash, new string[] { testStashDir });

        try
        {
            // 执行Stash操作
            Builder.Stash();

            // 验证原始文件已被移动
            Assert.That(XFile.HasFile(testStashFile), Is.False, "原始文件应当被移动。");
            Assert.That(XFile.HasFile(testStashFileMeta), Is.False, "原始 meta 文件应当被移动。");

            // 验证隐藏文件已创建
            var hiddenDir = $"{testStashDir}~";
            var hiddenDirMeta = XFile.PathJoin(Path.GetDirectoryName(testStashDir), "." + Path.GetFileName(testStashDir) + ".meta");
            Assert.That(XFile.HasDirectory(hiddenDir), Is.True, "隐藏文件应当存在。");
            Assert.That(XFile.HasFile(hiddenDirMeta), Is.True, "隐藏 meta 文件应当存在。");

            // 验证stashFile已创建并包含正确内容
            Assert.That(XFile.HasFile(Builder.stashFile), Is.True, "暂存文件应当存在。");
            var stashContent = XFile.OpenText(Builder.stashFile);
            Assert.That(stashContent.Contains(testStashDir), Is.True, "暂存文件应当包含测试目录路径。");

            // 执行Restore操作
            Assert.DoesNotThrow(() => Builder.Restore(), "恢复应当不抛出异常。");

            // 验证文件已恢复
            Assert.That(XFile.HasFile(testStashFile), Is.True, "原始文件应当恢复。");
            Assert.That(XFile.HasFile(testStashFileMeta), Is.True, "原始 meta 文件应当恢复。");

            // 验证隐藏文件已被删除
            Assert.That(XFile.HasDirectory(hiddenDir), Is.False, "隐藏文件应当被删除。");
            Assert.That(XFile.HasFile(hiddenDirMeta), Is.False, "隐藏 meta 文件应当被删除。");

            // 验证stashFile已被删除
            Assert.That(XFile.HasFile(Builder.stashFile), Is.False, "暂存文件应当被删除。");
        }
        finally
        {
            // 清理测试文件和目录
            if (XFile.HasDirectory(testStashDir)) XFile.DeleteDirectory(testStashDir);
            if (XFile.HasFile(testStashDirMeta)) XFile.DeleteFile(testStashDirMeta);

            // 确保stashFile被删除
            if (XFile.HasFile(Builder.stashFile)) XFile.DeleteFile(Builder.stashFile);
        }
    }
}
