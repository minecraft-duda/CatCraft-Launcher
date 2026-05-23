@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ========================================
echo    CatCraft Launcher 发布构建脚本
echo ========================================
echo.

:: 清理
if exist ".\release" (
    echo 清理旧的发布文件夹...
    rmdir /s /q ".\release"
)

echo.
echo 正在构建（保留所有文件）...
echo.

:: 先还原包
dotnet restore

:: 编译 Release 版本（不发布，直接输出所有文件）
dotnet build -c Release -o ./release

:: 复制图标
if exist ".\icon.ico" (
    copy /y ".\icon.ico" ".\release\icon.ico"
)

echo.
echo ========================================
if exist ".\release\CatCraftLauncher.exe" (
    echo 构建成功！
    echo.
    echo 输出目录: %CD%\release
    echo.
    echo 将此文件夹下的所有文件复制到目标电脑
    echo 目标电脑需要安装 .NET 7.0 运行时
) else (
    echo 构建失败
)
echo ========================================
echo.
pause