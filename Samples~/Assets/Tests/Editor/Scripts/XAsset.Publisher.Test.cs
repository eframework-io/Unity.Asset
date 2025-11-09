// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Networking;
using EFramework.Unity.Editor;
using EFramework.Unity.Utility;
using EFramework.Unity.Asset;
using static EFramework.Unity.Asset.Editor.XAsset;

/// <summary>
/// TestXAssetPublisher 是 XAsset.Publisher 的单元测试。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuilder))]
public class TestXAssetPublisher
{
    [Test]
    public void Process()
    {
        // 设置测试环境
        XPrefs.Asset.Set(Publisher.Preferences.Endpoint, "http://localhost:9000");
        XPrefs.Asset.Set(Publisher.Preferences.Bucket, "default");
        XPrefs.Asset.Set(Publisher.Preferences.Access, "admin");
        XPrefs.Asset.Set(Publisher.Preferences.Secret, "adminadmin");
        XPrefs.Asset.Set(XAsset.Preferences.LocalUri, "Assets");
        XPrefs.Asset.Set(XAsset.Preferences.RemoteUri, $"TestXAssetPublisher/Builds-{XTime.GetMillisecond()}/Assets");

        // 创建处理器
        var handler = new Publisher() { ID = "Test/TestXAssetPublisher" };

        // 执行发布
        LogAssert.Expect(LogType.Error, new Regex(@"<ERROR> Object does not exist.*"));
        LogAssert.Expect(LogType.Error, new Regex(@"XEditor\.Command\.Run: finish mc.*"));
        var report = XEditor.Tasks.Execute(handler);

        // 验证发布结果
        Assert.That(report.Result == XEditor.Tasks.Result.Succeeded, "资源发布应当成功。");

        var manifestUrl = $"{XPrefs.Asset.GetString(Publisher.Preferences.Endpoint)}/{XPrefs.Asset.GetString(Publisher.Preferences.Bucket)}/{XPrefs.Asset.GetString(XAsset.Preferences.RemoteUri)}/{XMani.Default}";
        var request = UnityWebRequest.Get(manifestUrl);
        request.timeout = 10;
        request.SendWebRequest();
        while (!request.isDone) { }
        Assert.That(request.responseCode == 200, Is.True, "资源清单应当请求成功");

        var manifest = new XMani.Manifest();
        Assert.That(manifest.Parse(request.downloadHandler.text, out _), Is.True, "资源清单应当读取成功");
    }
}
