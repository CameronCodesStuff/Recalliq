#define MyAppName "RecallIQ"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CameronCodesStuff"
#define MyAppURL "https://github.com/CameronCodesStuff"
#define MyAppExeName "RecallIQ.UI.exe"
#define MyAppDescription "AI-Powered Local File Search for Windows"
#define DotNetVersion "8"
#define DotNetDesktopRuntimeURL "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"
#define WindowsAppSDKURL "https://aka.ms/windowsappsdk/1.6/1.6.250108002/windowsappruntimeinstall-x64.exe"

[Setup]
AppId={{7A9F3D2E-4B1C-4D8E-9F2A-6C3B5D7E8F1A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
InfoBeforeFile=..\installer\PreInstallInfo.txt
OutputDir=..\installer\output
OutputBaseFilename=RecallIQ-Setup-{#MyAppVersion}
SetupIconFile=..\RecallIQ.UI\Assets\recalliq.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=110,110
WizardImageFile=WizardImage.bmp
WizardSmallImageFile=WizardSmallImage.bmp
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
english.InstallingDotNet=Installing .NET {#DotNetVersion} Desktop Runtime...
english.InstallingWinAppSDK=Installing Windows App SDK Runtime...
english.DotNetRequired=RecallIQ requires the .NET {#DotNetVersion} Desktop Runtime.%nIt will be downloaded and installed now.
english.WinAppSDKRequired=RecallIQ requires the Windows App SDK Runtime.%nIt will be downloaded and installed now.
english.DownloadFailed=Failed to download a required component.%nPlease check your internet connection and try again.
english.LaunchAfterInstall=Launch RecallIQ
english.CreateDesktopIcon=Create a desktop shortcut
english.InstallComplete=RecallIQ has been installed successfully!
english.OptionalComponents=Optional Components
english.OnnxModelInfo=For best search quality, download the all-MiniLM-L6-v2 ONNX model from Hugging Face and place it in:%n%n{app}\models\
english.TesseractInfo=For OCR support (scanned PDFs, images), download eng.traineddata and place it in:%n%n{app}\tessdata\

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "Start RecallIQ with Windows"; GroupDescription: "System Integration:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\models"; Permissions: users-modify
Name: "{app}\tessdata"; Permissions: users-modify
Name: "{localappdata}\RecallIQ"; Permissions: users-modify
Name: "{localappdata}\RecallIQ\logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"
Name: "{group}\{#MyAppName} Documentation"; Filename: "{app}\docs\USER_MANUAL.md"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "{#MyAppDescription}"
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--minimized"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\RecallIQ\logs"
Type: dirifempty; Name: "{localappdata}\RecallIQ"

[Code]
var
  DependenciesPage: TOutputMsgWizardPage;
  NeedsDotNet: Boolean;
  NeedsWinAppSDK: Boolean;

function CheckDotNetDesktopRuntime(): Boolean;
var
  FindRec: TFindRec;
  RuntimePath: String;
begin
  Result := False;
  RuntimePath := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');

  if DirExists(RuntimePath) then
  begin
    if FindFirst(RuntimePath + '\{#DotNetVersion}.*', FindRec) then
    begin
      try
        repeat
          if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
          begin
            Result := True;
            Break;
          end;
        until not FindNext(FindRec);
      finally
        FindClose(FindRec);
      end;
    end;
  end;

  if not Result then
  begin
    if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
    begin
      Result := True;
    end;
  end;
end;

function CheckWinAppSDK(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('cmd', '/c reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages" /f "WindowsAppRuntime" /s >nul 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
      Result := True;
  end;
end;

function DownloadAndRun(const URL, StatusText, Args: String): Boolean;
var
  DownloadPage: TDownloadWizardPage;
  ResultCode: Integer;
begin
  Result := False;

  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), StatusText, nil);
  DownloadPage.Clear;
  DownloadPage.Add(URL, ExtractFileName(URL), '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
      Result := True;
    except
      SuppressibleMsgBox(CustomMessage('DownloadFailed') + #13#10#13#10 + GetExceptionMessage, mbError, MB_OK, IDOK);
    end;
  finally
    DownloadPage.Hide;
  end;

  if Result then
  begin
    WizardForm.StatusLabel.Caption := StatusText;
    Result := Exec(ExpandConstant('{tmp}\') + ExtractFileName(URL), Args, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    if Result then
      Result := (ResultCode = 0) or (ResultCode = 3010) or (ResultCode = 1638);
  end;
end;

procedure InitializeWizard();
begin
  DependenciesPage := CreateOutputMsgPage(wpReady,
    'Runtime Dependencies',
    'RecallIQ requires the following runtime components.',
    'The installer will check for required dependencies and install any that are missing.' + #13#10 + #13#10 +
    '    .NET {#DotNetVersion} Desktop Runtime' + #13#10 +
    '    Windows App SDK Runtime' + #13#10 + #13#10 +
    'Click Next to check and install dependencies, then install RecallIQ.');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = DependenciesPage.ID then
  begin
    NeedsDotNet := not CheckDotNetDesktopRuntime();
    NeedsWinAppSDK := not CheckWinAppSDK();

    if NeedsDotNet then
    begin
      if MsgBox(CustomMessage('DotNetRequired'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        if not DownloadAndRun('{#DotNetDesktopRuntimeURL}', CustomMessage('InstallingDotNet'), '/install /quiet /norestart') then
        begin
          if MsgBox('.NET Runtime installation may have failed.' + #13#10 + 'Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
          begin
            Result := False;
            Exit;
          end;
        end;
      end
      else
      begin
        if MsgBox('RecallIQ may not work without the .NET Runtime.' + #13#10 + 'Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
        begin
          Result := False;
          Exit;
        end;
      end;
    end;

    if NeedsWinAppSDK then
    begin
      if MsgBox(CustomMessage('WinAppSDKRequired'), mbConfirmation, MB_YESNO) = IDYES then
      begin
        if not DownloadAndRun('{#WindowsAppSDKURL}', CustomMessage('InstallingWinAppSDK'), '--quiet') then
        begin
          if MsgBox('Windows App SDK installation may have failed.' + #13#10 + 'Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
          begin
            Result := False;
            Exit;
          end;
        end;
      end;
    end;
  end;
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := '';
  Result := Result + 'Installation Directory:' + NewLine;
  Result := Result + Space + WizardDirValue + NewLine + NewLine;

  if MemoTasksInfo <> '' then
  begin
    Result := Result + 'Additional Tasks:' + NewLine;
    Result := Result + MemoTasksInfo + NewLine + NewLine;
  end;

  Result := Result + 'Data Directory:' + NewLine;
  Result := Result + Space + ExpandConstant('{localappdata}\RecallIQ') + NewLine + NewLine;

  if NeedsDotNet then
    Result := Result + 'Will Install: .NET {#DotNetVersion} Desktop Runtime' + NewLine;
  if NeedsWinAppSDK then
    Result := Result + 'Will Install: Windows App SDK Runtime' + NewLine;
end;
