# XAsset.Core

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

提供了内部事件管理器、异步加载处理器等功能，是资源管理器的基础模块。

## 功能特性

- 内部事件管理器：定义资源系统生命周期中的关键事件
- 异步加载处理器：负责跟踪和管理异步资源加载的过程

## 使用手册

### 1. 运行流程

以下流程图展示了资源管理器的运行时逻辑，包括资源/场景加载/卸载、引用计数管理、内置事件机制的主要流程：

```mermaid
stateDiagram-v2
    direction TB

    state 资源管理流程 {
        资源加载请求 --> 资源加载模式 : OnPreLoadResource
        
        资源加载模式 --> Bundle资源加载 : bundle = true
        资源加载模式 --> Resources资源加载 : bundle == false or resource = true
        
        Bundle资源加载 --> 手动管理引用 : retain = true
        Bundle资源加载 --> 自动管理引用 : Refer.Awake
        Resources资源加载 --> 加载目标资源
        
        增加引用计数 --> 加载目标资源
        加载目标资源 --> 资源加载完成 : OnPostLoadResource

        资源加载完成 --> 资源卸载请求
        资源卸载请求 --> 手动管理释放 : Unload
        资源卸载请求 --> 自动管理释放 : Refer.OnDestroy
    }

    state 场景管理流程 {
        场景加载请求 --> 场景加载模式 : OnPreLoadScene
        
        场景加载模式 --> Bundle场景加载 : bundle = true
        场景加载模式 --> Resources场景加载 : bundle = false

        Bundle场景加载 --> 自动管理引用
        Resources场景加载 --> 加载目标场景
        
        增加引用计数 --> 加载目标场景
        加载目标场景 --> 场景加载完成 : OnPostLoadScene

        场景加载完成 --> 场景卸载请求
        场景卸载请求 --> 手动管理释放 : Unload
        场景卸载请求 --> 自动管理释放 : SceneManager.sceneUnloaded
    }

    state 依赖管理流程 {
        state 依赖引用流程 {
            手动管理引用 --> 增加引用计数 : bundle.Retain
            自动管理引用 --> 增加引用计数 : bundle.Retain
        }

        state 依赖释放流程 {
            手动管理释放 --> 减少引用计数 : bundle.Release
            自动管理释放 --> 减少引用计数 : bundle.Release
            
            减少引用计数 --> 引用计数为零
            引用计数为零 --> 卸载资源依赖 : OnPostUnloadBundle
        }
    }
```

### 2. 事件类型

- 功能说明：定义资源生命周期中的关键事件
- 事件列表：
  - `OnPreLoadResource`：资源加载前
  - `OnPostLoadResource`：资源加载后
  - `OnPreLoadScene`：场景加载前
  - `OnPostLoadScene`：场景加载后
  - `OnPostUnloadBundle`：资源包卸载后
- 使用示例：
```csharp
XAsset.Event.Register(XAsset.EventType.OnPreLoadResource, (asset) => Debug.Log("资源加载前：" + asset));
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
