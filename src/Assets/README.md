# 资源文件说明

## 需要添加的图标文件

请将以下图标文件放置在此目录中：

1. **app.ico** - 应用程序图标（用于 .exe 文件）
   - 推荐尺寸：256x256 像素
   - 格式：ICO 格式（包含多种尺寸）

2. **clipboard.ico** - 系统托盘图标
   - 推荐尺寸：32x32 或 64x64 像素
   - 格式：ICO 格式

## 临时解决方案

如果暂时没有图标文件，可以：

1. 注释掉 `ClipMaster.csproj` 中的 `<ApplicationIcon>Assets\app.ico</ApplicationIcon>`
2. 在 `App.xaml.cs` 中将图标路径改为系统内置图标或注释掉相关代码

## 在线图标资源

可以从以下网站获取免费图标：
- https://www.flaticon.com/search?word=clipboard
- https://icons8.com/icons/set/clipboard
- https://www.iconfinder.com/search?q=clipboard

## 图标生成

也可以使用 PowerShell 生成简单的占位图标：

```powershell
# 需要安装 ImageMagick
magick convert -size 256x256 xc:#2B579A -gravity center -pointsize 120 -fill white -annotate +0+0 "C" app.ico
```
