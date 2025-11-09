// Copyright (c) 2025 EFramework Innovation. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using NUnit.Framework;
using static EFramework.Unity.Asset.XAsset;

/// <summary>
/// TestXAssetPreferences 是 XAsset.Preferences 的单元测试。
/// </summary>
public class TestXAssetPreferences
{
    [Test]
    public void Keys()
    {
        Assert.That(Preferences.BundleMode, Is.EqualTo("XAsset/BundleMode"));
        Assert.That(Preferences.ReferMode, Is.EqualTo("XAsset/ReferMode"));
        Assert.That(Preferences.DebugMode, Is.EqualTo("XAsset/DebugMode"));
        Assert.That(Preferences.SimulateMode, Is.EqualTo("XAsset/SimulateMode@Editor"));
        Assert.That(Preferences.OffsetFactor, Is.EqualTo("XAsset/OffsetFactor"));
        Assert.That(Preferences.AssetUri, Is.EqualTo("XAsset/AssetUri"));
        Assert.That(Preferences.LocalUri, Is.EqualTo("XAsset/LocalUri"));
        Assert.That(Preferences.RemoteUri, Is.EqualTo("XAsset/RemoteUri"));
    }

    [Test]
    public void Defaults()
    {
        Assert.That(Preferences.BundleModeDefault, Is.True);
        Assert.That(Preferences.ReferModeDefault, Is.True);
        Assert.That(Preferences.OffsetFactorDefault, Is.EqualTo(4));
        Assert.That(Preferences.AssetUriDefault, Is.EqualTo("Patch@Assets.zip"));
        Assert.That(Preferences.LocalUriDefault, Is.EqualTo("Assets"));
        Assert.That(Preferences.RemoteUriDefault, Is.EqualTo("Builds/Patch/${Environment.Author}/${Environment.Version}/${Environment.Platform}/Assets"));
    }
}
