# pLawnModLoader

**pLawnModLoader** 是一款为《PlantsVsZombies .NET》设计的轻量级模组加载器。基于 Harmony 补丁技术，它允许开发者扩展游戏功能而无需修改原始游戏文件。

项目包含三部分：
- **pLawnModLoader**：核心加载器，扫描并应用补丁，最终启动游戏。
- **pLawnModLoaderLauncher**：WPF 图形化管理器，用于选择游戏路径、管理补丁方案、一键更新并启动游戏。
- **pLMods**：示例补丁，展示如何编写和配置补丁。

> 本项目为独立创作，与原版游戏开发者无关。

---

##  快速开始

### 1. 获取启动器
从 [Releases](https://github.com/AmourLing/pLawnModLoader/releases) 下载最新版本，或自行编译源代码（注意调整 `Lawn.dll` 引用路径）。

### 2. 目录结构
将 `pLawnModLoaderLauncher.exe` 及其依赖文件置于任意文件夹（如 `D:\pLawnModLoaderLauncher`）。启动器会自动生成以下结构：

```
pLawnModLoaderLauncher.exe
pLawnModLoader.exe                (由启动器复制到游戏目录)
pLMod/                            (存放补丁源文件)
   ├── LargerSeedBank.dll
   ├── LargerSeedBank.txt         (补丁说明文件，可选)
   └── LargerImitaterDialog.dll
```

通过启动器操作后，游戏目录将生成：

```
游戏根目录/
├── pLawnModLoader.exe
├── pLMods/                       (运行时补丁文件夹)
│   ├── LargerSeedBank.dll        (启用的补丁)
│   ├── LargerImitaterDialog.dll
│   ├── config/                   (补丁配置文件)
│   │   └── pLawnModLoaderConfig.json
│   └── log/                      (日志文件夹)
│       └── p_2026_03_02_22_14_56.log   (每次启动生成时间戳日志)
└── ... (其他游戏文件)
```

### 3. 使用启动器
1. **选择游戏路径**：点击“浏览”选择游戏主程序 `Lawn.exe`（位于游戏根目录）。
2. **管理补丁**：在“可用补丁”列表中，每个补丁项右侧有三个操作按钮：
   - `[?]`：查看说明（自动读取与 DLL 同名的 `.txt` 文件）。
   - `⚙`：配置补丁（直接编辑 `pLMods/config/pLawnModLoaderConfig.json` 中的对应项）。
   - 复选框：启用/禁用该补丁。
3. **操作按钮**：
   - **更新**：将加载器和选中的补丁复制到游戏目录（不启动游戏）。
   - **更新并启动**：复制后立即启动游戏。
   - **打开补丁文件夹**：在资源管理器中打开游戏目录下的 `pLMods` 文件夹。
   - **刷新补丁列表**：重新扫描 `pLMod` 文件夹中的补丁。

> 💡 **首次使用**：建议点击“更新并启动”，确保游戏目录获得最新的加载器和补丁。

### 4. 手动启动（可选）
- 将 `pLawnModLoader.exe` 复制到游戏根目录（与 `Lawn.dll` 同级）。
- 在游戏根目录创建 `pLMods` 文件夹，将补丁 DLL 放入其中。
- 如需配置，创建 `pLMods/config/pLawnModLoaderConfig.json`。
- 运行 `pLawnModLoader.exe` 启动游戏（日志自动生成在 `pLMods/log`）。

---

## 项目结构

```
pLawnModLoader/
├── pLawnModLoader/                   # 核心加载器
│   ├── Program.cs                    # 入口：扫描补丁、应用、启动游戏
│   ├── Log.cs                        # 日志类（带时间戳）
│   ├── ModConfig.cs                   # 配置管理（读写 pLMods/config/...）
│   └── pLawnModLoader.csproj
├── pLawnModLoaderLauncher/           # WPF 启动器
│   ├── MainWindow.xaml                # 主界面
│   ├── AppConfig.cs                    # 方案配置模型
│   ├── PatchManager.cs                 # 补丁扫描和应用
│   ├── PatchItem.cs                     # 补丁数据模型
│   ├── InputDialog.xaml                 # 新建方案对话框
│   ├── pLModsConfigWindow.xaml          # 补丁配置窗口（直接编辑 JSON）
│   └── pLawnModLoaderLauncher.csproj
├── pLMods/                           # 示例补丁
│   ├── LargerSeedBank/                # 种子槽扩容
│   │   ├── LargerSeedBank.cs          # 补丁主类（含配置类）
│   │   ├── LargerSeedBank.txt         # 说明文件
│   │   └── LargerSeedBank.csproj
│   └── LargerImitaterDialog/          # 模仿者对话框扩容
│       ├── LargerImitaterDialog.cs
│       ├── LargerImitaterDialog.txt
│       └── LargerImitaterDialog.csproj
└── README.md
```

---

##  致谢
- [Harmony](https://github.com/pardeike/Harmony)
- [PVZ.NET](https://github.com/Mewnojs/PlantsVsZombies.NET)

---

##  联系与反馈
- [GitHub Issues](https://github.com/AmourLing/pLawnModLoader/issues)
- QQ群：1034609947

---
Ciallo～(∠・ω< )⌒★