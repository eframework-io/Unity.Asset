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
        Assert.AreEqual(Preferences.BundleMode, "Asset/BundleMode");
        Assert.AreEqual(Preferences.ReferMode, "Asset/ReferMode");
        Assert.AreEqual(Preferences.DebugMode, "Asset/DebugMode");
        Assert.AreEqual(Preferences.SimulateMode, "Asset/SimulateMode@Editor");
        Assert.AreEqual(Preferences.OffsetFactor, "Asset/OffsetFactor");
        Assert.AreEqual(Preferences.AssetUri, "Asset/AssetUri");
        Assert.AreEqual(Preferences.LocalUri, "Asset/LocalUri");
        Assert.AreEqual(Preferences.RemoteUri, "Asset/RemoteUri");
    }

    [Test]
    public void Defaults()
    {
        Assert.AreEqual(Preferences.BundleModeDefault, true);
        Assert.AreEqual(Preferences.ReferModeDefault, true);
        Assert.AreEqual(Preferences.OffsetFactorDefault, 4);
        Assert.AreEqual(Preferences.AssetUriDefault, "Patch@Assets.zip");
        Assert.AreEqual(Preferences.LocalUriDefault, "Assets");
        Assert.AreEqual(Preferences.RemoteUriDefault, "Builds/Patch/${Environment.Author}/${Environment.Version}/${Environment.Platform}/Assets");
    }
}
