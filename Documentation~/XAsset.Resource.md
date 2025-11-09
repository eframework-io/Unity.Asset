# XAsset.Resource

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了 Unity 资源的加载与卸载，支持自动处理依赖资源的生命周期。

## 功能特性

- 支持资源的加载与卸载
- 自动处理资源依赖关系

## 使用手册

### 1. 同步加载

#### 1.1 加载资源
- 功能说明：根据当前模式从 `Resources` 或 `AssetBundle` 中加载资源
- 函数参数：
  - `path`：资源路径（`string` 类型）
  - `type`：资源类型（`Type` 类型）
  - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
- 函数返回：加载的资源对象（`UnityEngine.Object` 类型），加载失败时返回 `null`
- 使用示例：
```csharp
var asset = XAsset.Resource.Load("Example/Test.prefab", typeof(GameObject));
```

#### 1.2 泛型加载
- 功能说明：提供类型安全的资源加载方式
- 函数参数：
  - `path`：资源路径（`string` 类型）
  - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
- 函数返回：加载的资源对象（`T` 类型），加载失败时返回 `null`
- 使用示例：
```csharp
var asset = XAsset.Resource.Load<GameObject>("Example/Test.prefab");
```

### 2. 异步加载

#### 2.1 加载资源
- 功能说明：异步加载资源，适合加载大型资源
- 函数参数：
  - `path`：资源路径（`string` 类型）
  - `type`：资源类型（`Type` 类型）
  - `callback`：加载完成时的回调函数（`Callback` 类型）
  - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
- 函数返回：用于跟踪加载进度的 `Handler` 对象
- 使用示例：
```csharp
XAsset.Resource.LoadAsync("Example/Test.prefab", typeof(GameObject), (asset) =>
{
    Debug.Log("加载完成：" + asset.name);
});
```

#### 2.2 泛型加载
- 功能说明：提供类型安全的异步加载方式
- 函数参数：
  - `path`：资源路径（`string` 类型）
  - `callback`：加载完成时的类型安全回调（`Action<T>` 类型）
  - `resource`：是否强制从 `Resources` 加载（`bool` 类型）
- 函数返回：用于跟踪加载进度的 `Handler` 对象
- 使用示例：
```csharp
XAsset.Resource.LoadAsync<GameObject>("Example/Test.prefab", (asset) =>
{
    Debug.Log("加载完成：" + asset.name);
});
```

### 3. 卸载资源

- 功能说明：卸载指定路径的资源
- 函数参数：
  - `path`：资源路径（`string` 类型）
- 使用示例：
```csharp
XAsset.Resource.Unload("Example/Test.prefab");
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
