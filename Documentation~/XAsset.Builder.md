# XAsset.Builder

[![NPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=NPM&logo=npm)](https://www.npmjs.com/package/io.eframework.unity.asset)
[![UPM](https://img.shields.io/npm/v/io.eframework.unity.asset?label=UPM&logo=unity&registry_uri=https://package.openupm.com)](https://openupm.com/packages/io.eframework.unity.asset)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/Unity.Asset)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

XAsset.Builder 提供了资源的构建工作流，支持资源的依赖分析及打包功能。

## 功能特性

- 首选项配置：提供首选项配置以自定义构建流程
- 自动化流程：提供资源包构建任务的自动化执行

## 使用手册

### 1. 首选项配置

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 输出路径 | `Asset/Builder/Output@Editor` | `Builds/Patch/${Environment.Platform}/Assets` | 资源包的输出路径 |
| 包含路径 | `Asset/Builder/Include@Editor` | `["Assets/Resources/Bundle", "Assets/Scenes/**/*.unity"]` | 需要打包的资源路径 |
| 排除路径 | `Asset/Builder/Exclude@Editor` | `[]` | 需要排除的资源路径 |
| 暂存路径 | `Asset/Builder/Stash@Editor` | `["Assets/Resources/Bundle"]` | 需要暂存的资源路径 |
| 合并材质 | `Asset/Builder/Merge/Material@Editor` | `true` | 合并材质选项 |
| 合并单包 | `Asset/Builder/Merge/Single@Editor` | `false` | 合并单包选项 |
| 拷贝资源 | `Asset/Builder/Streaming/Assets@Editor` | `true` | 构建时拷贝资源至 StreamingAssets |

关联配置项：`Asset/OffsetFactor`、`Asset/AssetUri`、`Asset/LocalUri`

以上配置项均可在 `Tools/EFramework/Preferences/Asset/Builder` 首选项编辑器中进行可视化配置。

### 2. 自动化流程

#### 2.1 构建流程

```mermaid
stateDiagram-v2
    direction LR
    分析依赖 --> 打包资源
    打包资源 --> 生成清单
```

#### 2.2 构建准备

##### 依赖分析
- 依赖分析系统将资源类型分为可加载资源和原生依赖资源
- 可加载资源资源一般位于 Resources 和 Scenes 目录中，通过 `Asset/Builder/Include@Editor` 选项进行设置，以单文件形式进行打包
- 原生依赖资源一般位于 RawAssets 目录中，不可以加载，以文件夹形式进行打包
- 可以通过设置 `Asset/Builder/Include@Editor` 选项排除 `Asset/Builder/Include@Editor` 中包含的文件/目录，支持通配符

##### 资源合并
- 材质合并：可选择是否将材质合并到场景包中以完整收集 `Shader` 变体，注意：若某材质的依赖数为1，则默认进行材质合并
- 单包合并：可选择是否将单一资源合并到主包中
- 自定义合并：支持通过 AssetImporter 设置自定义打包规则

#### 2.3 构建产物

在 `Asset/Builder/Output@Editor` 目录下会生成以下文件：
- `*.bundle`：资源包文件，格式为 `file_md5.bundle`
- `Manifest.db`：资源包清单，格式为 `名称|MD5|大小`

构建产物会在内置构建事件 `XEditor.Event.Internal.OnPreprocessBuild` 触发时内置于安装包的资源目录下：

- 移动平台 (Android/iOS/..)
  ```
  <AssetPath>/
  └── <AssetUri>  # 资源包压缩为 ZIP
  ```

- 桌面平台 (Windows/macOS/..)
  ```
  <输出目录>_Data/
  └── Local/
      └── <LocalUri>  # 资源包直接部署
  ```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE.md)
