# XAsset.Preferences

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了运行时的首选项管理，用于控制运行模式、调试选项和资源路径等配置项。

## 功能特性

- 运行模式配置：支持 AssetBundle 和 Resources 模式切换
- 调试选项管理：支持调试模式和模拟模式的切换
- 资源路径配置：支持配置内置、本地和远端资源路径
- 可视化配置界面：在 Unity 编辑器中提供直观的设置面板

## 使用手册

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| Bundle 模式 | `Asset/BundleMode` | `true` | 控制是否启用 AssetBundle 模式，启用后将从打包的资源文件加载资源 |
| 引用计数模式 | `Asset/ReferMode` | `true` | 控制是否启用引用计数模式，启用后会自动跟踪资源引用，确保资源正确释放 |
| 调试模式 | `Asset/DebugMode` | `false` | 控制是否启用调试模式，启用后会输出详细的资源加载和释放日志 |
| 模拟模式 | `Asset/SimulateMode@Editor` | `false` | 控制是否启用模拟模式，仅在编辑器中可用，模拟 AssetBundle 的资源加载行为 |
| 文件偏移 | `Asset/OffsetFactor` | `4` | 设置资源包的头部偏移算子，用于进行简单的 AssetBundle 资源加密 |
| 内置资源路径 | `Asset/AssetUri` | `Patch@Assets.zip` | 设置资源包的内置路径，用于打包时的处理 |
| 本地资源路径 | `Asset/LocalUri` | `Assets` | 设置资源包的本地路径，用于运行时的加载 |
| 远端资源路径 | `Asset/RemoteUri` | `Builds/Patch/${Environment.Author}/${Environment.Version}/${Environment.Platform}/Assets` | 设置资源包的远端路径，用于运行时的下载 |

以上配置项均可在 `Tools/EFramework/Preferences/Asset` 首选项编辑器中进行可视化配置。

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
