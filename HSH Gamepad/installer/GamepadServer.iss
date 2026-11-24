; HSH Gamepad installer  |  Inno Setup 6.3+ (نسخه‌ی 6)  |  Telegram: @HSHGamepad
;
; بیلد:   ISCC.exe /DArch=x64 /DTarget=modern /DFw=net48 GamepadServer.iss
;         Arch   = x64 | x86
;         Target = modern (ویندوز 10/11) | legacy (ویندوز 7 SP1 و 8.1، با پیش‌نیازهای درایور)
;         Fw     = net48 (کم‌حجم؛ اگه .NET Framework 4.8 نبود موقع نصب دانلود میشه)
;                  full  (نسخه‌ی Full؛ نصب‌کننده‌ی آفلاین .NET Framework 4.8 داخل Setup هست، بدون دانلود)
;         خودِ برنامه تو هر دو یکیه (net48، چند صد کیلوبایت)؛ فرق فقط تو پیش‌نیازهای همراه Setup ـه.
;         (build.bat همه‌ی ترکیب‌ها رو میسازه)

#ifndef Arch
  #define Arch "x64"
#endif
#ifndef Target
  #define Target "modern"
#endif
#ifndef Fw
  #define Fw "net48"
#endif
#if Target == "legacy"
  #define TargetName "Win7-8.1"
#else
  #define TargetName "Win10-11"
#endif

; درایور میکروفون مجازی = VB-Cable (فایل‌ها رو از زیپ VBCABLE_Driver_Pack43 تو redist\vbcable بریز؛ برای هر معماری فقط فایل‌های win7 لازمه)
;   x64: VBCABLE_Setup_x64.exe + vbaudio_cable64_win7.sys + vbaudio_cable64_win7.cat + vbMmeCable64_win7.inf
;   x86: VBCABLE_Setup.exe     + vbaudio_cable_win7.sys   + vbaudio_cable_win7.cat   + vbMmeCable_win7.inf
; (نسخه‌ی win7 برای ویندوز 7/8.1/10/11 همه استفاده میشه؛ فایل‌های xp/vista/2003 لازم نیست.) اگه پوشه نبود نصب‌کننده بدون میکروفون ساخته میشه.
#if Arch == "x64"
  #define VbSetup "VBCABLE_Setup_x64.exe"
  #define VbSys   "vbaudio_cable64_win7.sys"
  #define VbCat   "vbaudio_cable64_win7.cat"
  #define VbInf   "vbMmeCable64_win7.inf"
#else
  #define VbSetup "VBCABLE_Setup.exe"
  #define VbSys   "vbaudio_cable_win7.sys"
  #define VbCat   "vbaudio_cable_win7.cat"
  #define VbInf   "vbMmeCable_win7.inf"
#endif
#define HaveVb FileExists(AddBackslash(SourcePath) + "..\redist\vbcable\" + VbSetup)

#define AppName     "HSH Gamepad"
#define AppExe      "HSHGamepad.exe"
#define AppVer      "1.0.6"
#define ViGEmModern "ViGEmBus_1.22.0_x64_x86_arm64.exe"
#define ViGEmLegacy "ViGEmBus_Setup_1.16.116.exe"
#define Net48Url    "https://go.microsoft.com/fwlink/?LinkId=2085155"
#if Fw == "net48"
  #define Net48File "ndp48-web.exe"
#else
  ; نصب‌کننده‌ی آفلاین (~112MB) تو redist\ ؛ build.bat دانلودش میکنه
  #define Net48File "ndp48-x86-x64-allos-enu.exe"
#endif

#if Arch == "x64"
  #define KbFile    "Windows6.1-KB3033929-x64.msu"
  #define X360File  "Xbox360_64Eng.exe"
  #define VcFile    "vc_redist.x64.exe"
#else
  #define KbFile    "Windows6.1-KB3033929-x86.msu"
  #define X360File  "Xbox360_32Eng.exe"
  #define VcFile    "vc_redist.x86.exe"
#endif

[Setup]
AppId={{B3C1F2A4-6D7E-4A5B-9C10-2E8F7A1D4C33}
AppName={#AppName}
AppVersion={#AppVer}
AppPublisher=Telegram: @HSHGamepad
AppPublisherURL=https://t.me/HSHGamepad
DefaultDirName={autopf}\HSH Gamepad
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
#if Fw == "net48"
OutputBaseFilename=HSHGamepad-Setup-{#TargetName}-{#Arch}
#else
OutputBaseFilename=HSHGamepad-Setup-{#TargetName}-{#Arch}-Full
#endif
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
#if Target == "legacy"
; ویندوز 7 (با سرویس‌پک 1) و 8 / 8.1 ؛ ویندوز 10 به بالا باید نسخه‌ی Win10-11 رو بگیره
MinVersion=6.1sp1
OnlyBelowVersion=10.0
#else
MinVersion=10.0
#endif
; نصب درایور و قانون فایروال ادمین میخواد
PrivilegesRequired=admin
#if Arch == "x64"
; این نصب‌کننده فقط برای ویندوز 64 بیتی
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#else
; این نصب‌کننده فقط برای ویندوز 32 بیتی
ArchitecturesAllowed=x86os
#endif
CloseApplications=yes
ShowLanguageDialog=yes
LanguageDetectionMethod=uilanguage
RestartApplications=no

[Languages]
; پنجره‌ی انتخاب زبان خودِ Inno Setup (قبل از شروع نصب): «Installer Language / Please select a language»
; زبان‌هایی که ترجمه‌ی رسمی دارن از compiler:Languages ؛ فارسی/چینی/کره‌ای ترجمه‌ی رسمی ندارن،
; پس فقط اسمشون (isl\*.isl) روی متن انگلیسی گذاشته میشه. اسم هر زبان = کد زبان برنامه (همون lang\xx.json)
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fa"; MessagesFile: "compiler:Default.isl,isl\fa.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "zh"; MessagesFile: "compiler:Default.isl,isl\zh.isl"
Name: "pt"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "tr"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "ko"; MessagesFile: "compiler:Default.isl,isl\ko.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"

[InstallDelete]
; پاک کردن بقایای اسم قبلی (Gamepad Server) موقع آپدیت
Type: files; Name: "{app}\GamepadServer.exe"
Type: files; Name: "{autoprograms}\Gamepad Server.lnk"
Type: files; Name: "{autodesktop}\Gamepad Server.lnk"

[Files]
#if HaveVb
; درایور میکروفون مجازی (VB-Cable): فقط فایل‌های همین معماری + win7؛ تو temp کپی و بعد از نصب پاک میشه
Source: "..\redist\vbcable\{#VbSetup}"; DestDir: "{tmp}\vbcable"; Flags: deleteafterinstall; Check: NeedsVbCable
Source: "..\redist\vbcable\{#VbSys}";   DestDir: "{tmp}\vbcable"; Flags: deleteafterinstall; Check: NeedsVbCable
Source: "..\redist\vbcable\{#VbCat}";   DestDir: "{tmp}\vbcable"; Flags: deleteafterinstall; Check: NeedsVbCable
Source: "..\redist\vbcable\{#VbInf}";   DestDir: "{tmp}\vbcable"; Flags: deleteafterinstall; Check: NeedsVbCable
#endif
; خروجی کامل پوشه‌ی build (exe + ViGEmClient.dll + ...) ؛ ران‌تایم نیست، .NET Framework 4.8 باید رو سیستم باشه
Source: "..\publish\net48\{#Arch}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs
#if Fw != "net48"
; نسخه‌ی Full: .NET 4.8 داخل Setup جاسازی میشه (فقط موقع نیاز استخراج و نصب میشه، تو پوشه‌ی برنامه کپی نمیشه)
Source: "..\redist\{#Net48File}"; DestDir: "{tmp}"; Flags: dontcopy
#endif

#if Target == "legacy"
; --- ویندوز 7 / 8.1: ViGEmBus 1.17 به بعد فقط ویندوز 10 رو ساپورت میکنه، پس 1.16.116 ---
Source: "..\redist\win7\{#ViGEmLegacy}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsLegacyViGEm
; آپدیت امضای SHA-2 درایور (فقط ویندوز 7؛ قبل از کپی فایل‌ها تو PrepareToInstall نصب میشه)
Source: "..\redist\win7\{#KbFile}"; DestDir: "{tmp}"; Flags: dontcopy
; درایور Xbox 360 که ویندوز 7 خودش نداره (فقط ویندوز 7)
Source: "..\redist\win7\{#X360File}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsX360Driver
; ران‌تایم VC++ (ویندوز 7/8.1 بدون UCRT برنامه‌ی .NET 6 رو اجرا نمیکنن)
Source: "..\redist\{#VcFile}"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsVcRedist
#else
; --- ویندوز 10/11 ---
; ViGEmBus ویندوز 10/11: توسط [Code] (InstallModernViGEm) اجرا میشه تا نتیجه‌ی نصب چک بشه؛ پس اینجا فقط داخل exe جاسازی میشه
Source: "..\redist\{#ViGEmModern}"; DestDir: "{tmp}"; Flags: dontcopy
#endif

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
#if Target == "legacy"
; 0) ران‌تایم VC++ (فقط وقتی UCRT نیست)
Filename: "{tmp}\{#VcFile}"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Visual C++ runtime..."; Flags: waituntilterminated; Check: NeedsVcRedist
; 1) درایور Xbox 360 (فقط ویندوز 7)
;    ⚠ سوییچ بی‌صدا برای این نصب‌کننده تست نشده؛ پس فعلاً پنجره‌ی خودش باز میشه (Next بزن)
Filename: "{tmp}\{#X360File}"; StatusMsg: "Installing Xbox 360 controller driver..."; Flags: waituntilterminated; Check: NeedsX360Driver
; 2) درایور ViGEmBus 1.16.116
;    ⚠ سوییچ بی‌صداش هم تست نشده؛ اگه /S یا /quiet جواب داد اینجا اضافه‌ش کن
Filename: "{tmp}\{#ViGEmLegacy}"; StatusMsg: "Installing ViGEmBus driver (legacy)..."; Flags: waituntilterminated; Check: NeedsLegacyViGEm
#else
; 1) درایور ViGEmBus ویندوز 10/11: تو [Code] نصب میشه (InstallModernViGEm)
#endif
; 3) قانون فایروال تا گوشی بتونه وصل شه (همه‌ی شبکه‌ها: خصوصی، دامین و عمومی؛ وگرنه تو وای‌فای «عمومی» گوشی وصل نمیشد)
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Gamepad Server"""; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#AppName}"""; Flags: runhidden waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""{#AppName}"" dir=in action=allow program=""{app}\{#AppExe}"" enable=yes profile=any"; StatusMsg: "Configuring firewall..."; Flags: runhidden waituntilterminated
#if HaveVb
; 3.5) درایور میکروفون مجازی (VB-Cable)؛ ⚠ سوییچ «-i -h» (نصب بی‌صدا) از حافظه‌ست و تست نشده؛ اگه جواب نداد «-h» رو بردار تا پنجره‌ی خودش باز شه
Filename: "{tmp}\vbcable\{#VbSetup}"; Parameters: "-i -h"; WorkingDir: "{tmp}\vbcable"; StatusMsg: "Installing virtual microphone driver..."; Flags: waituntilterminated; Check: NeedsVbCable
#endif
; 4) اجرای برنامه
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Dirs]
; پوشه‌ی data کنار برنامه: تنظیمات و چیدمان/تم‌های ذخیره‌شده؛ کاربر عادی هم باید بتونه توش بنویسه
Name: "{app}\data"; Permissions: users-modify

[UninstallDelete]
; پوشه‌ی data رو هم خود برنامه پر میکنه؛ موقع حذف برنامه پاک میشه
Type: filesandordirs; Name: "{app}\data"
Type: files; Name: "{app}\setup-language.txt"
; پوشه‌ی lang رو خود برنامه موقع اجرا میسازه (نصب‌کننده نمیشناسدش)؛ پس باید صریح پاک بشه
Type: filesandordirs; Name: "{app}\lang"

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""{#AppName}"""; Flags: runhidden; RunOnceId: "DelFirewallRule"

[Code]
function NeedsVbCable: Boolean;   // میکروفون مجازی: اگه درایور VB-Cable از قبل نصب نیست
var Names: TArrayOfString; I: Integer;
begin
  Result := True;
  if RegGetSubkeyNames(HKLM, 'SYSTEM\CurrentControlSet\Services', Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos('vbaudio', Lowercase(Names[I])) > 0 then begin Result := False; Exit; end;
end;

function IsLegacyOS: Boolean;      // قدیمی‌تر از ویندوز 10
var V: TWindowsVersion;
begin
  GetWindowsVersionEx(V);
  Result := V.Major < 10;
end;

function IsWin7: Boolean;
var V: TWindowsVersion;
begin
  GetWindowsVersionEx(V);
  Result := (V.Major = 6) and (V.Minor = 1);
end;

function HasViGEm: Boolean;
begin
  // فقط کلید سرویس کافی نیست (بعد از آنینستال ناقص یا نصب خراب میمونه)؛ فایل درایور هم باید باشه
  Result := RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\ViGEmBus')
            and FileExists(ExpandConstant('{sys}\drivers\ViGEmBus.sys'));
end;

function NeedsModernViGEm: Boolean;
begin
  Result := (not HasViGEm) and (not IsLegacyOS);
end;

function NeedsLegacyViGEm: Boolean;
begin
  Result := (not HasViGEm) and IsLegacyOS;
end;

function NeedsX360Driver: Boolean;
begin
  // ویندوز 8 به بعد خودش درایور داره؛ ویندوز 7 اگه xusb21.sys داشته باشه یعنی قبلاً نصب شده
  Result := (not HasViGEm) and IsWin7 and (not FileExists(ExpandConstant('{sys}\drivers\xusb21.sys')));
end;

function NeedsVcRedist: Boolean;
begin
  // UCRT (ucrtbase.dll) تو ویندوز 10 هست؛ تو ویندوز 7/8.1 فقط با آپدیت KB2999226 یا نصب VC++ میاد.
  // vcruntime140.dll رو هم چک میکنیم چون ممکنه ViGEmClient.dll (بخش native) بخوادش.
  Result := IsLegacyOS and ((not FileExists(ExpandConstant('{sys}\ucrtbase.dll'))) or (not FileExists(ExpandConstant('{sys}\vcruntime140.dll'))));
end;

// ---------------- آنینستال: بستن برنامه‌ی در حال اجرا ----------------
// چک میکنه پروسه‌ای با این اسم در حال اجراست یا نه
function IsProcessRunning(const ExeName: String): Boolean;
var
  rc: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'),
    '/C tasklist /FI "IMAGENAME eq ' + ExeName + '" /NH | find /I "' + ExeName + '" >nul',
    '', SW_HIDE, ewWaitUntilTerminated, rc) and (rc = 0);
end;

// اول مودبانه (WM_CLOSE) میبنده، اگه تا 3 ثانیه بسته نشد اجباری میکشه (/F /T)
// برمیگردونه True اگه برنامه دیگه در حال اجرا نیست
function CloseProcess(const ExeName: String): Boolean;
var
  rc, i: Integer;
begin
  Result := True;
  if not IsProcessRunning(ExeName) then Exit;

  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM "' + ExeName + '"', '', SW_HIDE, ewWaitUntilTerminated, rc);
  for i := 1 to 12 do
  begin
    if not IsProcessRunning(ExeName) then Exit;
    Sleep(250);
  end;

  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /T /IM "' + ExeName + '"', '', SW_HIDE, ewWaitUntilTerminated, rc);
  for i := 1 to 8 do
  begin
    if not IsProcessRunning(ExeName) then Exit;
    Sleep(250);
  end;
  Result := not IsProcessRunning(ExeName);
end;

// قبل از شروع آنینستال صدا زده میشه؛ اگه False برگردونه آنینستال لغو میشه
function InitializeUninstall: Boolean;
begin
  Result := True;
  // اسم قبلی برنامه (Gamepad Server) هم چک میشه، برای نصب‌های قدیمی
  if not (CloseProcess('{#AppExe}') and CloseProcess('GamepadServer.exe')) then
  begin
    SuppressibleMsgBox(
      '{#AppName} is still running and could not be closed automatically.' + #13#10 +
      'Please close it manually (check the system tray / Task Manager) and run the uninstaller again.',
      mbError, MB_OK, IDOK);
    Result := False;
  end;
end;
// ---------------------------------------------------------------------

var
  ViGEmNeedsReboot: Boolean;

#if Target != "legacy"
// نصب‌کننده‌ی ViGEmBus رو اجرا میکنه و فقط وقتی True برمیگردونه که درایور واقعاً نصب شده باشه
function RunViGEmSetup(const Params: String; ShowCmd: Integer): Boolean;
var
  rc: Integer;
begin
  Result := False;
  if Exec(ExpandConstant('{tmp}\{#ViGEmModern}'), Params, '', ShowCmd, ewWaitUntilTerminated, rc) then
  begin
    // 3010 / 1641 = نصب شد ولی ویندوز ریستارت میخواد
    if (rc = 3010) or (rc = 1641) then ViGEmNeedsReboot := True;
    Result := HasViGEm;
  end;
end;

procedure InstallModernViGEm;
begin
  if not NeedsModernViGEm then Exit;
  WizardForm.StatusLabel.Caption := 'Installing ViGEmBus driver...';
  ExtractTemporaryFile('{#ViGEmModern}');
  // 1) بی‌صدا
  if RunViGEmSetup('/exenoui /qn /norestart', SW_HIDE) then Exit;
  if ViGEmNeedsReboot then Exit;
  // 2) بی‌صدا جواب نداد (مثلاً ویندوز 11 با تنظیمات امنیتی): خود پنجره‌ی نصب‌کننده‌ی درایور رو نشون بده
  WizardForm.StatusLabel.Caption := 'Installing ViGEmBus driver (follow the driver setup window)...';
  if RunViGEmSetup('/norestart', SW_SHOWNORMAL) then Exit;
  if ViGEmNeedsReboot then Exit;
  SuppressibleMsgBox(
    'The ViGEmBus driver could not be installed automatically.' + #13#10 + #13#10 +
    'Restart Windows and run this setup again, or install it manually from' + #13#10 +
    'https://github.com/nefarius/ViGEmBus/releases' + #13#10 +
    '(HSH Gamepad will not start without it.)',
    mbError, MB_OK, IDOK);
end;
#endif

procedure HshWriteLanguage; forward;

procedure CurStepChanged(CurStep: TSetupStep);
begin
#if Target != "legacy"
  if CurStep = ssInstall then InstallModernViGEm;
#endif
  if CurStep = ssPostInstall then HshWriteLanguage;
end;

// اگه درایور نصب شده ولی ویندوز ریستارت خواسته، آخر نصب پیشنهاد ریستارت میده
function NeedRestart(): Boolean;
begin
  Result := ViGEmNeedsReboot;
end;

// .NET Framework 4.8 (Release >= 528040). ویندوز 10 نسخه‌ی 1903 به بعد و ویندوز 11 از قبل دارن.
function NeedsNet48: Boolean;
var
  Rel: Cardinal;
begin
  Result := True;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Rel) then
    Result := Rel < 528040;
end;

// ---------------- زبان برنامه ----------------
// زبانی که کاربر تو پنجره‌ی «Installer Language» اول نصب انتخاب میکنه (ActiveLanguage = کد زبان، مثلاً fa)
// تو {app}\setup-language.txt نوشته میشه و برنامه بار اول زبانش رو از روی اون تنظیم میکنه.
// نصب بی‌صدا:  Setup.exe /VERYSILENT /LANG=de   (یا /APPLANG=de)
procedure HshWriteLanguage;
var
  Code, Stamp: String;
begin
  Code := ExpandConstant('{param:APPLANG|}');
  if Code = '' then Code := ActiveLanguage;
  if Code = '' then Exit;
  // مهر یکتا برای هر نصب؛ برنامه با اون میفهمه این انتخابِ جدیده
  Stamp := GetDateTimeString('yyyymmddhhnnss', #0, #0) + IntToStr(Random(100000));
  SaveStringToFile(ExpandConstant('{app}\setup-language.txt'), Code + #13#10 + Stamp + #13#10, False);
end;
// ---------------------------------------------------------------------

#if Fw == "net48"
var
  DownloadPage: TDownloadWizardPage;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

#endif

procedure InitializeWizard;
begin
#if Fw == "net48"
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
#endif
end;

// (نسخه‌ی کم‌حجم) قبل از شروع نصب، اگه .NET 4.8 نیست
// نصب‌کننده‌ی کوچیک مایکروسافت (~1.5MB) رو دانلود میکنه
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
#if Fw == "net48"
  if (CurPageID = wpReady) and NeedsNet48 then
  begin
    DownloadPage.Clear;
    DownloadPage.Add('{#Net48Url}', '{#Net48File}', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
      except
        Result := False;
        if not DownloadPage.AbortedByUser then
          SuppressibleMsgBox(
            'Could not download .NET Framework 4.8 (' + GetExceptionMessage + ').' + #13#10 + #13#10 +
            'Install it manually from https://dotnet.microsoft.com/download/dotnet-framework/net48' + #13#10 +
            'and run this setup again.' + #13#10 +
            '(Windows 7: the download may fail until the Windows update KB4474419 is installed.)',
            mbCriticalError, MB_OK, IDOK);
      end;
    finally
      DownloadPage.Hide;
    end;
  end;
#endif
end;

// آپدیت SHA-2 درایور (فقط ویندوز 7) و بعدش .NET 4.8 ؛ اگه ویندوز ریستارت بخواد، ریستارت میکنه
// و بعدش خودکار نصب رو ادامه میده.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  rc: Integer;
begin
  Result := '';
#if Target == "legacy"
  if IsWin7 and (not HasViGEm) then
  begin
    ExtractTemporaryFile('{#KbFile}');
    Exec(ExpandConstant('{sys}\wusa.exe'),
         '"' + ExpandConstant('{tmp}\{#KbFile}') + '" /quiet /norestart',
         '', SW_HIDE, ewWaitUntilTerminated, rc);
    // 3010 = نیاز به ریستارت؛ 2359302 = از قبل نصب بوده
    if rc = 3010 then
    begin
      NeedsRestart := True;
      Result := 'A required Windows update (KB3033929) was installed. Windows must restart; setup will continue automatically afterwards.';
      Exit;
    end;
  end;
#endif
  if NeedsNet48 then
  begin
    if not FileExists(ExpandConstant('{tmp}\{#Net48File}')) then
    begin
#if Fw == "net48"
      // بعد از ریستارتِ خودکار، فایل دانلودشده‌ی قبلی از بین رفته؛ دوباره دانلودش کن
      try
        DownloadTemporaryFile('{#Net48Url}', '{#Net48File}', '', @OnDownloadProgress);
      except
        Result := 'Could not download .NET Framework 4.8 (' + GetExceptionMessage + ').' + #13#10 +
                  'Install it manually from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this setup again.';
        Exit;
      end;
#else
      // نسخه‌ی Full: از داخل خود Setup استخراج میشه، اینترنت لازم نیست
      ExtractTemporaryFile('{#Net48File}');
#endif
    end;
    if not Exec(ExpandConstant('{tmp}\{#Net48File}'), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, rc) then
      rc := -1;
    if (rc = 3010) or (rc = 1641) then
    begin
      NeedsRestart := True;
      Result := '.NET Framework 4.8 was installed. Windows must restart; setup will continue automatically afterwards.';
    end
    else if rc <> 0 then
      Result := '.NET Framework 4.8 could not be installed (code ' + IntToStr(rc) + ').' + #13#10 +
                'Install it manually from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this setup again.' + #13#10 +
                '(Windows 7: needs the Windows updates KB4474419 and KB4490628 first.)';
  end;
end;
