# XAsset.Bundle

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

XAsset.Bundle 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期。

## 功能特性

- 资源包管理：支持同步和异步加载资源包，支持缓存
- 引用计数管理：自动处理依赖关系，通过引用计数管理资源的生命周期

## 使用手册

### 1. 基本用法

#### 1.1 初始化资源清单
- 功能说明：初始化 Bundle 的清单文件
- 使用示例：
```csharp
/// Initialize 初始化 Bundle 的清单文件，如果存在旧的清单会先卸载它。
/// 仅适用于 Bundle 模式，这个清单文件对于资源的正确加载是必需的。
XAsset.Bundle.Initialize();
```

#### 1.2 同步加载资源包
- 功能说明：加载指定的资源包及其所有依赖资源包
- 使用示例：
```csharp
var bundle = XAsset.Bundle.Load("example.bundle");
```

#### 1.3 异步加载资源包
- 功能说明：异步加载指定的资源包及其所有依赖资源包
- 使用示例：
```csharp
var handler = new Handler();
yield return XAsset.Bundle.LoadAsync("example.bundle", handler);
```

#### 1.4 查找已加载的资源包
- 功能说明：在已加载的资源包中查找指定名称的资源包
- 使用示例：
```csharp
var bundle = XAsset.Bundle.Find("example.bundle");
```

#### 1.5 卸载已加载的资源包
- 功能说明：卸载指定的资源包，减少其引用计数，当计数为 0 时释放资源
- 使用示例：
```csharp
XAsset.Bundle.Unload("example.bundle");
```

### 2. 引用计数

#### 2.1 增加引用
- 功能说明：增加资源包的引用计数，同时增加所有依赖资源包的引用计数
- 使用示例：
```csharp
var count = bundle.Retain();
```

#### 2.2 减少引用
- 功能说明：减少资源包的引用计数，当计数为 0 时自动卸载资源包及其不再被引用的依赖资源
- 使用示例：
```csharp
var count = bundle.Release();
```

## 常见问题

### 1. 为什么资源包没有被卸载？
- 确保引用计数已降为 0
- 检查是否有其他资源包依赖该资源包

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
