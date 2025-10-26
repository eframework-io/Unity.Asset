# XAsset.Scene

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

XAsset.Scene 提供了 Unity 场景的加载与卸载，支持自动处理依赖资源的生命周期。

## 功能特性

- 支持场景的加载与卸载
- 自动处理资源依赖关系

## 使用手册

### 1. 同步加载

- 功能说明：同步加载场景，在 `Bundle` 模式下会自动加载场景对应的资源包
- 函数参数：
  - `nameOrPath`：场景名称或完整路径（`string` 类型）
  - `loadMode`：场景加载模式（`LoadSceneMode` 类型），默认为 `Single`
- 使用示例：
```csharp
XAsset.Scene.Load("Scenes/TestScene", LoadSceneMode.Single);
```

### 2. 异步加载

- 功能说明：异步加载场景，适合加载大型场景，避免加载过程阻塞主线程
- 函数参数：
  - `nameOrPath`：场景名称或完整路径（`string` 类型）
  - `callback`：场景加载完成后的回调函数（`Action` 类型）
  - `loadMode`：场景加载模式（`LoadSceneMode` 类型），默认为 `Single`
- 函数返回：用于跟踪加载进度的 `Handler` 对象
- 使用示例：
```csharp
XAsset.Scene.LoadAsync("Scenes/TestScene", () =>
{
    Debug.Log("场景加载完成");
});
```

### 3. 卸载场景

- 功能说明：卸载指定场景，在 `Bundle` 模式下会同时卸载场景对应的资源包
- 函数参数：
  - `nameOrPath`：场景名称或完整路径（`string` 类型）
- 使用示例：
```csharp
XAsset.Scene.Unload("Scenes/TestScene");
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
