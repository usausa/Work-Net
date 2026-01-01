dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0-windows                -r win-x64   --self-contained -p:PublishSingleFile=true -o publish/win-x64
dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 /p:BuildPlatform=linux -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 /p:BuildPlatform=linux -r linux-arm64 --self-contained -p:PublishSingleFile=true -o publish/linux-arm64
dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 /p:BuildPlatform=macos -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64

rem dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0-windows                -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
rem dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 /p:BuildPlatform=linux -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
rem dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 /p:BuildPlatform=macos -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/osx-x64

rem publish\win-x64\WorkPlugin.Service.exe
rem publish\linux-x64\WorkPlugin.Service.exe
rem publish\osx-x64\WorkPlugin.Service.exe
