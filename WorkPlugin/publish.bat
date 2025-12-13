dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0-windows -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
rem dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0-macos -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64
dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
dotnet publish WorkPlugin.Service/WorkPlugin.Service.csproj -c Release -f net10.0 -r linux-arm64 --self-contained -p:PublishSingleFile=true -o publish/linux-arm64
