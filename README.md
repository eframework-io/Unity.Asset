# EFramework Asset for Unity

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

完整的 Unity 资源管理解决方案，实现了从资源打包发布到运行时加载的全流程管理，支持资源加载、引用计数、自动卸载等特性。

## 功能特性

- [XAsset.Core](Documentation~/XAsset.Core.md) 是资源管理器的基础模块，提供了内部事件管理器、异步加载处理器等功能
- [XAsset.Bundle](Documentation~/XAsset.Bundle.md) 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期
- [XAsset.Resource](Documentation~/XAsset.Resource.md) 提供了 Unity 资源的加载与卸载，支持自动处理依赖资源的生命周期
- [XAsset.Scene](Documentation~/XAsset.Scene.md) 提供了 Unity 场景的加载与卸载，支持自动处理依赖资源的生命周期
- [XAsset.Constants](Documentation~/XAsset.Constants.md) 提供了一些常量定义和运行时环境控制，包括运行配置和 Bundle 名称生成、偏移计算等功能
- [XAsset.Preferences](Documentation~/XAsset.Preferences.md) 提供了运行时的首选项管理，用于控制运行模式、调试选项和资源路径等配置项
- [XAsset.Builder](Documentation~/XAsset.Builder.md) 提供了资源的构建工作流，支持资源的依赖分析及打包功能
- [XAsset.Publisher](Documentation~/XAsset.Publisher.md) 实现了资源包的发布工作流，用于将打包好的资源发布至存储服务中

## 常见问题

更多问题，请查阅[问题反馈](CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](CHANGELOG.md)
- [贡献指南](CONTRIBUTING.md)
- [许可协议](LICENSE.md)