```
redist
├── NDP48-x86-x64-AllOS-ENU.exe
├── vc_redist.x64.exe
├── vc_redist.x86.exe
├── ViGEmBus_1.22.0_x64_x86_arm64.exe
└── win7
    ├── ViGEmBus_Setup_1.16.116.exe
    ├── Windows6.1-KB3033929-x64.msu
    ├── Windows6.1-KB3033929-x86.msu
    ├── Xbox360_32Eng.exe
    └── Xbox360_64Eng.exe
```
Files that get bundled INTO the installers. build.bat downloads what it can;
the rest must be placed here by hand.

redist\
  ViGEmBus_1.22.0_x64_x86_arm64.exe     Windows 10/11 driver (auto-downloaded)
  vc_redist.x64.exe / vc_redist.x86.exe VC++ 2015-2019 runtime, needed on Windows 7 (auto-downloaded, aka.ms/vs/16/release)

redist\win7\                            only used by the Win7/8.1 installers (KB + Xbox360 files: Win7 only)
  ViGEmBus_Setup_1.16.116.exe           last ViGEmBus that supports Windows 7 (auto-download attempted)
  Windows6.1-KB3033929-x64.msu          } SHA-2 driver-signing update, from Microsoft Update Catalog
  Windows6.1-KB3033929-x86.msu          }
  Xbox360_64Eng.exe / Xbox360_32Eng.exe Xbox 360 Accessories Software 1.2 (provides the missing xusb driver on Win7)

Manual-download mirror with the KB + Xbox360 files:
  https://github.com/Zalafina/ViGEmBus_v1.16.116_Win7_InstallFiles

NOTE: the .NET Framework 4.8 installers do not bundle anything for .NET - if the PC lacks 4.8
the installer downloads Microsoft's small web installer (go.microsoft.com/fwlink/?LinkId=2085155).
