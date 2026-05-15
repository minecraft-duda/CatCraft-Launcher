@echo off
dotnet publish -c Release -p:PublishSingleFile=true --self-contained false -o ./publish
copy icon.ico ./publish\
echo 编译完成，exe 位于 ./publish/CatCraftLauncher.exe
pause