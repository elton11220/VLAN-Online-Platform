; 脚本用 Inno Setup 脚本向导 生成。
; 查阅文档获取创建 INNO SETUP 脚本文件的详细资料！

#define MyAppName "VLAN Online Platform"
#define MyAppVersion "1.9"
#define MyAppPublisher "Elton11220"
#define MyAppURL "https://gitee.com/elton11220/VLAN-Online-Platform/releases"
#define MyAppExeName "VLAN Online Platform.exe"

[Setup]
; 注意: AppId 的值是唯一识别这个程序的标志。
; 不要在其他程序中使用相同的 AppId 值。
; (在编译器中点击菜单“工具 -> 产生 GUID”可以产生一个新的 GUID)
AppId={{23D25531-8B98-4C3D-A76D-D67FB81B9403}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=C:\
OutputBaseFilename=setup
SetupIconFile=C:\Users\Elton\source\repos\VLAN Online Platform\icon.ico
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimp.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\Dotfuscated\VLAN Online Platform.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\MetroFramework.Design.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\MetroFramework.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\MetroFramework.Fonts.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Elton\source\repos\VLAN Online Platform\bin\Release\data\*"; DestDir: "{app}\\data"; Flags: ignoreversion recursesubdirs createallsubdirs
; 注意: 不要在任何共享的系统文件使用 "Flags: ignoreversion"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent