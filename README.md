# 🍅 番茄标签 — 标签设计与打印软件

<p align="center">
  <b>Tomato Label — Label Design & Printing Software</b>
</p>

## 产品简介

番茄标签是一款面向中小企业及个人用户的**标签设计与打印软件**，基于 C# WPF 开发，支持 Windows 桌面端进行标签的创建、编辑、管理和打印。

### 核心功能

- 🎨 可视化拖拽式标签设计（所见即所得）
- 📊 支持条形码、二维码、关联二维码等多种编码元素
- 📁 模板库（官方模板 + 用户自定义模板）
- 🖨️ 批量打印与变量数据打印
- ☁️ 云端模板存储与同步
- 🔌 多种打印机支持（热敏打印机、激光打印机等）

### 支持的元素类型

| 元素 | 说明 |
|------|------|
| 文本 | 单行/多行/富文本，多种字体和样式 |
| 条码 | CODE128、CODE39、EAN-13、EAN-8、UPC-A、ITF-14、Codabar |
| 二维码 | 标准QR Code，支持文本/URL/电话/邮件/WiFi/名片 |
| 关联码 | 扫描跳转指定页面，支持动态内容 |
| 图片 | PNG、JPG、BMP、GIF、SVG |
| 图标 | 内置行业图标库 |
| 线条 | 水平/垂直/斜线，多种样式 |
| 矩形 | 支持圆角、阴影、边框 |
| 日期 | 自动日期，支持偏移计算 |
| 表格 | 自定义行列，支持合并单元格 |
| PDF | 导入PDF页面为图片元素 |
| 警示语 | ISO标准警示图标和文字 |
| 水印 | 全画布水印文字 |

## 技术架构

- **框架**: .NET 8.0 + WPF
- **架构模式**: MVVM (CommunityToolkit.Mvvm)
- **语言**: C# 12

### 项目结构

```
src/CodePrint/
├── Models/              # 数据模型（18个文件）
│   ├── LabelElement.cs  # 元素基类 + ElementType 枚举
│   ├── TextElement.cs   # 文本元素
│   ├── BarcodeElement.cs # 条码元素
│   ├── QrCodeElement.cs  # 二维码元素
│   ├── ...              # 其他元素类型
│   ├── LabelDocument.cs # 标签文档
│   ├── LabelFolder.cs   # 文件夹
│   ├── LabelTemplate.cs # 模板
│   ├── PrintSettings.cs # 打印设置
│   └── UserProfile.cs   # 用户信息
├── ViewModels/          # 视图模型（6个文件）
│   ├── MainViewModel.cs
│   ├── LabelManagementViewModel.cs
│   ├── TemplateLibraryViewModel.cs
│   ├── PrintViewModel.cs
│   ├── PropertyPanelViewModel.cs
│   └── LabelSettingsViewModel.cs
├── Views/               # 视图
│   ├── Panels/          # 面板控件
│   │   ├── ElementPanel.xaml    # 左侧元素面板
│   │   ├── CanvasPanel.xaml     # 中央画布
│   │   └── PropertyPanel.xaml   # 右侧属性面板
│   ├── Dialogs/         # 对话框
│   │   ├── LabelSettingsDialog.xaml
│   │   └── PrintSettingsDialog.xaml
│   └── Controls/        # 自定义控件
├── Converters/          # 值转换器（5个文件）
├── Helpers/             # 工具类
├── Resources/           # 资源文件（样式、主题）
├── Services/            # 服务层
└── Commands/            # 命令
```

## 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 7 / 10 / 11（64位） |
| 内存 | ≥ 4GB RAM |
| 硬盘 | ≥ 500MB 可用空间 |
| 分辨率 | ≥ 1280 × 720 |

## 构建与运行

```bash
# 还原依赖
dotnet restore src/CodePrint/CodePrint.csproj

# 构建
dotnet build src/CodePrint/CodePrint.csproj

# 运行（需要 Windows 环境）
dotnet run --project src/CodePrint/CodePrint.csproj
```

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + S` | 保存 |
| `Ctrl + Z` | 撤销 |
| `Ctrl + Y` | 恢复 |
| `Ctrl + A` | 全选 |
| `Ctrl + C` | 复制 |
| `Ctrl + V` | 粘贴 |
| `Ctrl + X` | 剪切 |
| `Delete` | 删除 |
| `Ctrl + P` | 打印 |
| `Ctrl + +/-` | 缩放 |
| `方向键` | 微移元素 |
| `Shift + 方向键` | 快速移动 |

## 许可证

版权所有 © 2026