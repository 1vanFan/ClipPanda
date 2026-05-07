# ClipPanda - Windows 11 剪贴板管理器

一只可爱的熊猫剪贴板助手，让你的复制粘贴更高效！

## 功能特性

### MVP 版本功能
- ✅ 自动捕获剪贴板内容（文本、图片、文件）
- ✅ 关键字实时搜索
- ✅ 全局快捷键唤出（Ctrl+`）
- ✅ 系统托盘常驻
- ✅ SQLite 本地存储
- ✅ 自动去重
- ✅ 过期自动清理
- ✅ 可爱熊猫图标 🐼

### 计划功能（V1.0+）
- 长期收藏列表
- 分类标签管理
- 智能分级清理策略
- 自定义快捷键
- 深色模式

## 技术栈

- **.NET 8** - 运行时框架
- **WPF** - UI 框架
- **Entity Framework Core + SQLite** - 数据存储
- **Hardcodet.NotifyIcon.Wpf** - 系统托盘支持

## 编译说明

### 环境要求
- Windows 10 1809 或更高版本
- .NET 8 SDK
- Visual Studio 2022 或 VS Code

### 编译步骤

1. **安装 .NET 8 SDK**
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   ```

2. **克隆或下载项目**
   ```bash
   cd ClipPanda
   ```

3. **还原 NuGet 包**
   ```bash
   dotnet restore
   ```

4. **编译项目**
   ```bash
   dotnet build --configuration Release
   ```

5. **运行项目**
   ```bash
   dotnet run --project src/ClipPanda.csproj
   ```

6. **发布单文件版本**
   ```bash
   dotnet publish src/ClipPanda.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
   ```

## 使用说明

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + `` | 唤出/隐藏主面板 |
| `Enter` | 粘贴选中项 |
| `Ctrl + S` | 收藏/取消收藏 |
| `Delete` | 删除选中项 |
| `Esc` | 隐藏面板 |

### 操作流程

1. 复制任意内容（Ctrl+C）
2. 按 `Ctrl + `` 唤出面板
3. 输入关键字搜索或直接选择
4. 按 Enter 粘贴

## 项目结构

```
ClipPanda/
├── src/
│   ├── Models/              # 数据模型
│   │   ├── ClipboardItem.cs
│   │   └── AppSettings.cs
│   ├── Services/           # 服务层
│   │   ├── DatabaseService.cs
│   │   ├── ClipboardMonitorService.cs
│   │   └── HotkeyService.cs
│   ├── Views/              # 视图层
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   ├── ViewModels/         # 视图模型
│   │   └── MainViewModel.cs
│   ├── Assets/             # 资源文件
│   │   └── Styles.xaml
│   ├── App.xaml
│   ├── App.xaml.cs
│   └── ClipPanda.csproj
├── .github/
│   └── workflows/
│       └── build.yml       # GitHub Actions 自动编译配置
└── README.md
```

## 数据存储

- 数据库位置：`%LocalAppData%\ClipPanda\clipboard.db`
- 默认保留天数：7 天
- 默认最大条数：500 条

## 许可证

MIT License
