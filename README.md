<div align="center">

<img src="icon.ico" width="96" alt="HSH Gamepad">

# HSH Gamepad

**Turn your phone into a Gamepad (PS4 / Xbox 360 controller), gyroscope steering wheel, mouse & keyboard and a touch screen for your PC**
**گوشی‌ت رو تبدیل کن به دسته‌ی بازی PS4 / Xbox 360 / فرمون ژیروسکوپی / ماوس و کیبورد / نمایشگر لمسی برای کامپیوتر**

![Version](https://img.shields.io/badge/version-1.0.5-blue)
[![Downloads](https://img.shields.io/github/downloads/Amirhoshein/hshgamepad/total?color=3b5a99)](https://github.com/Amirhoshein/hshgamepad/releases)
![Platform](https://img.shields.io/badge/Windows-7%20%7C%208.1%20%7C%2010%20%7C%2011-0078D6)
![Players](https://img.shields.io/badge/players-up%20to%209-brightgreen)
![Latency](https://img.shields.io/badge/latency-2--6%20ms-orange)
![Gyroscope](https://img.shields.io/badge/gyroscope-supported-purple)
![Languages](https://img.shields.io/badge/languages-12-teal)
![Touch Screen](https://img.shields.io/badge/phone%20as%20monitor-new-red)

[**🇬🇧English**](#en) · [**🇮🇷فارسی**](#fa) · [🔗Telegram @HSHGamepad](https://t.me/HSHGamepad)

<br>

<img src="https://github.com/user-attachments/assets/3290b7a3-bf2c-4dee-b915-f1c5bd43968a" alt="HSH Gamepad Demo" width="900">

<!-- ویدیوی دمو: فایل mp4 رو توی ویرایشگر README بکش و رها کن، بعد لینکی که گیت‌هاب می‌سازه رو اینجا بذار:
<video src="LINK_VIDEO_اینجا" controls width="700"></video>
-->

<br>

[![Download for Windows](https://img.shields.io/badge/_-Download_Now-2ea44f?style=for-the-badge)](https://github.com/Amirhoshein/hshgamepad/releases/latest)

</div>

---

<a id="en"></a>

# 🇬🇧 English

**Contents:** [Quick Start](#en-quickstart) · [What's New](#en-new) · [Features](#en-features) · [Latency](#en-latency) · [Macros](#en-macros) · [Install & Use](#en-install) · [FAQ](#en-faq) · [Roadmap](#en-roadmap) · [Developers](#en-dev)

## What is HSH Gamepad?

A simple Windows app that turns your phone into a **real game controller** for your PC. No phone app needed: just open an address in your phone's browser. Games see a genuine Xbox 360 or PS4 controller plugged into the computer.

**Keywords:** phone as gamepad · gyroscope controller · motion controls · gyro steering wheel · virtual Xbox 360 / DualShock 4 · phone as mouse & keyboard · local multiplayer up to 9 players · phone as touch screen / second monitor · multi-language · ViGEmBus

Great for:
- Playing **local multiplayer with up to 9 players** without owning any controllers 🎮
- Using your phone's **gyroscope** for motion controls and as a **steering wheel** 🏎️
- Controlling your PC's **mouse & keyboard** from the phone, like a laptop touchpad 🖱️
- Seeing your PC screen **live on the phone** and using it as a **touch screen** 🖥️📱
- Using the app in **your own language** (12 built-in) 🌍
- Keeping the connection **private** with a secret address and automatic ban of guessers 🔐

<a id="en-quickstart"></a>

## 🚀 Quick Start (3 steps)

1. **Download & install:** get the installer for your Windows from **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)** (it installs the ViGEmBus driver for you)
2. **Run the app** and accept the administrator prompt. Make sure the **phone and PC are on the same Wi-Fi**
3. **Scan the QR code** in the app (or type the shown address into your phone's browser), choose your player and controller type, and launch your game 🎉

> Details, USB-cable mode and troubleshooting are below.

---

<a id="en-new"></a>

## 🆕 What's New in v1.0.5

> Upgrading from **v1.0.4**? Here is everything that was added and fixed. Full details are in [CHANGELOG.md](CHANGELOG.md).

| | |
|---|---|
| 🌍 **Multi-language** | 12 built-in languages for both the Windows app and the phone page, with a language picker, import / export and your own translations |
| 🔐 **Secret address + auto ban** | Optional secret key in the address, and automatic temporary ban of IPs that guess wrong |
| 🖥️ **Touch Screen mode** | See your PC monitor live on the phone and control it by touch (player 1) |
| 🌐 **Smarter IP detection** | Fixed: the shown address no longer jumps to a VPN / VMware / Hyper-V adapter, so the phone connects again |
| 🧱 **Firewall helper** | Detects when Windows Firewall blocks the current network and offers to fix it |
| 🧬 **Layout JSON editor** | View, copy, edit and paste your whole controller layout as JSON, to back it up or share it with friends |
| ⌨️ **Better typing** | Type any text (Persian and other Unicode too) with the phone keyboard |

### 🌍 Multi-language
- **12 languages built in:** English, فارسی, Deutsch, Español, Français, Italiano, Português, Русский, Türkçe, 中文, 日本語, 한국어
- The **Windows app, tray menu, dialogs and the phone page** all follow the chosen language
- New **language window** in the app: one card per language, plus **Import / Export / Open folder**
- Languages are plain **JSON files** in the `lang` folder next to the exe. Copy one, translate it, drop it in, and it shows up in the list (a missing key falls back to English, so nothing is ever blank)
- Changing the language in the app is instant for the Windows side. On the phone, just **refresh the page** and it switches too
- The **installer** also asks for a language, and your choice becomes the app language on first run

**Add your own language (custom JSON):**
1. Open the language window → **Open folder** (or go to the `lang` folder next to the exe)
2. Copy `en.json`, rename it to anything you like, and translate the texts. Keep the `meta` section (with `code` and `name`) and the `strings` section
3. Click **Import** (or just drop the file onto the language window, or into the folder). Your language appears as a new card; click it to apply
4. Use **Export** to save a language file and share it with others

### 🧬 Layout JSON editor
Besides the theme JSON, you can now edit the **layout** as JSON too.
- In the layout editor tap **Edit JSON**, then choose **this controller** or **all controllers** (PS4 / Xbox and wheel)
- The code shows every button's position, size and settings. **Copy** it to back it up or send it to a friend, or **Paste** someone else's layout
- Edit by hand if you like, press **Apply** to preview, **Undo** to go back, and **Save** at the end
- If the code belongs to another controller type, the app tells you instead of breaking your layout

### 🔐 Connection security
- **Secret address (access key):** phones can only connect through an address that contains your key, like `http://192.168.1.5/q2szc`. Any other address looks like a site that doesn't exist
- **Random key** button to generate a new key any time. The QR code and the shown address always include the key
- **Auto ban:** after N wrong attempts an IP is blocked for M minutes. Both numbers are adjustable, and you can turn the ban off
- Both options live in the server window. See [SECURITY.md](SECURITY.md) for the details

### 🖥️ Touch Screen mode (Monitor management)
Use the phone as a **live view of your PC monitor** and control the PC by touching it.
- Pick the **monitor** you want to see (multi-monitor PCs supported)
- Choose **image quality** and **FPS**; the real limit is always the lower of PC and phone resolution / Hz
- **H.264** live video (low bandwidth, higher fps) with automatic fallback to **JPEG** if the browser or PC can't do it
- Fast capture with **Desktop Duplication (DXGI)** on Windows 8 and later (it also captures fullscreen games), and an automatic GDI fallback on Windows 7
- Option to **show or hide the mouse pointer** in the image, and a frame **buffer** setting (more buffer = smoother and higher fps, a bit more delay)
- **Touch controls:** short touch = left click · hold = right click · one-finger drag = drag windows and files · two fingers = scroll · pinch = zoom (Ctrl + wheel)
- **Type with the phone keyboard** or use the **full keyboard**, with Unicode text (Persian too) independent of the Windows keyboard layout
- For **player 1 only**, and only when "Mouse & keyboard access from phone" is on in the Windows app

### 🌐 Fixes & improvements
- **Host IP changing / phone can't connect:** the app now ignores VPN, VMware, VirtualBox, Hyper-V, WSL, ZeroTier, Hamachi and other virtual adapters, and shows the IP of your **real Wi-Fi / Ethernet** card. APIPA (169.254.x.x) addresses are skipped too
- **Firewall:** Windows keeps separate rules for Domain / Private / Public networks, so switching Wi-Fi could silently block the phone. The app now checks the current network and, with your permission, adds a rule for all networks
- **Port helper:** the "port 80 is busy" dialogs are fully translated and name the program that holds the port
- **Mouse & keyboard:** new modes for moving the mouse and keyboard together, tap-and-drag delay and "hold main keys like Alt on double-click"
- Windows 7 / 8.1 and 10 / 11 installers are unchanged: same file names as before

---

<a id="en-features"></a>

## ✨ All Features at a Glance

### 🎮 Controllers
- **PS4 (DualShock 4)** and **Xbox 360** emulation, pick whichever you like
- **Up to 9 players** at once, each with their own color
- **Rumble / vibration**: when the game vibrates, your phone does too (can be turned off)
- **No app to install on the phone**, works in the phone's browser
- Custom **name per player** and a live **ping indicator** (green / yellow / red)
- Fullscreen mode

### 🎯 Gyroscope & Motion Controls
- **Phone gyroscope support**: the phone's motion sensor is sent to the PC as the **DualShock 4 (PS4) motion input** (PS4 mode)
- **Gyroscope steering wheel**: tilt and rotate your phone like a real wheel
  - Full **rotation angle** setting
  - **Sensitivity** and **N-degree multiplier**
  - **Dead zone**
  - **Smoothing** for soft, stable movement
  - **Center calibration**
  - **Gas / brake pedals**, analog or digital
  - Wheel look is fully customizable (gradient colors)
- **Live gyro log** in the Windows controller tester, so you can verify the data is arriving correctly

### 🌍 Languages
- **12 built-in languages** for the Windows app and the phone page, with custom JSON translations ([details](#en-new))

### 🔐 Security
- **Secret address** and **auto ban** for wrong attempts ([details](#en-new))

### 🖥️ Touch Screen (Player 1 only)
- Live **PC monitor on the phone**, monitor picker, H.264 / JPEG, quality and FPS control, full touch control ([details](#en-new))

### 🖱️ Mouse & Keyboard (Player 1 only)
- Laptop-style **touchpad** on the phone
- **Tap** = click · **two fingers** = scroll · **two-finger tap** = right click · **tap-and-drag**
- Adjustable **sensitivity**, **scroll speed** and **tap-and-drag delay**
- **Full keyboard** and typing with the phone keyboard (Unicode / Persian supported)
- **On/off switch** on the Windows app for phone mouse & keyboard access (extra safety)

### 🛠️ Total Freedom in Layout Editing
- **Layout editor**: press and hold any button, drop it wherever you want
- **Exact size** (width, height, font size) and **opacity** per button
- **Duplicate, delete and add** buttons
- **Different D-Pad / second-control modes**: buttons, separated buttons (action style), fixed or following joystick, right-stick ↔ buttons, separated or diamond action buttons
- **Macros**: any button can run a sequence of actions (`press`, `hold`, `release`, `delay`)
- **Layout JSON editor**: copy, edit and paste your layout as code, to back it up or share it ([details](#en-new))
- **Save, cancel and restore** your layout any time

### 🎨 Look & Themes
- Dozens of **built-in themes**, or make your own
- **Theme JSON editor**: copy your theme, tweak it, or paste a friend's and apply
- Every color is adjustable: background, buttons, text, border and border width, shadows, **neon glow**, pressed color, direction buttons, wheel (gradient), sticks, action buttons, macros and more
- Checkered background, **3D lighting effect**, smooth press animation
- **Light mode** for weaker phones (turns off blur and some animations to reduce latency)

### 💻 Windows App
- Shows the **connection address** + copy button + **QR code**, just scan
- List of connected players (name, phone model, IP)
- **Controller tester**: a live 3D window showing which buttons are pressed + gyro log + **rumble test**
- **Auto-remove idle controllers** after N seconds (optional)
- **Tray icon**: closing the window keeps the app running in the tray
- Taskbar right-click menu with **"Exit"**
- **Single instance** only (launching again just shows the existing window)
- **Automatic port selection** if port 80 is busy (IIS, Apache, Skype, ...), with a help window
- **Language window** (12 languages, import / export your own)
- **Smart IP detection** that skips VPN / virtual adapters, plus a **firewall check** for the current network
- Runs with **administrator rights**, so mouse/keyboard input can reach admin apps like Task Manager

### ⚡ Connection
- **2 to 6 ms** end-to-end latency (measured on the device side, see [Latency](#en-latency))
- Works over **Wi-Fi** or a **USB cable (USB tethering)** for the lowest latency
- Supports Windows **7, 8.1, 10 and 11** (x86 and x64)

---

<a id="en-latency"></a>

## ⚡ Latency

The app's own end-to-end latency, from touching the phone screen to the input being applied on the PC, is **2 ms to 6 ms**. This was measured with a **240 Hz camera** and **does not include network transit time** (i.e. it assumes local network latency of 0).

**Your real-world latency = this value + your network latency.** Wi-Fi latency varies with signal strength, interference and the router, while a USB cable is much steadier.

> 🎥 The test video recorded with a **240 Hz** camera is available on the Telegram channel: [@HSHGamepad](https://t.me/HSHGamepad)

For the **lowest possible latency**, connect your phone to the PC with a **USB cable** and turn on **USB tethering** (details below). If you'd rather not use a cable:
- Use **5 GHz Wi-Fi** and stay close to the router
- Connect the PC to the router with an **Ethernet cable** if possible
- Turn on **Light mode** on the phone
- For mouse use, set **tap-and-drag delay** to 0

### 🔌 USB Cable Connection (lowest latency)
1. Plug the phone into the PC with a USB cable
2. **Android:** Settings → Hotspot & tethering → turn on **USB tethering**
   **iPhone:** Settings → turn on Personal Hotspot (to use it over the cable, Apple's drivers / iTunes must be installed on Windows)
3. Windows creates a new network. Open the address the app shows (or the IP of the new network adapter from `ipconfig`) in the phone's browser
4. If the address shown belongs to your old network and doesn't open, type the IP of the new adapter (usually named Ethernet or Remote NDIS) manually in the phone's browser

> A cable has no Wi-Fi air interference or router in the middle, so its latency is steadier. It's the best choice for fast games (fighting, shooters, rhythm).

### 📶 Why no Bluetooth?
Bluetooth was left out on purpose. Its latency is **much higher** than Wi-Fi or a cable, it didn't feel good in testing because of lag, and many users wouldn't use it anyway. So the project focuses on the low-latency options: Wi-Fi and USB.

---

<a id="en-macros"></a>

## 🧩 Macro Tutorial

A macro **runs a whole sequence of actions automatically with one tap**, e.g. a fighting-game combo, or several buttons in a row.

**Creating a macro:**
1. Tap **Edit layout** → **Add macro**; a new button named `M1` (then `M2`, ...) appears
2. Tap the macro button → **Edit macro**
3. Add command lines (use the **Press / Hold / Release / Delay** buttons or type them)
4. Tap **Save** and leave edit mode. Tapping that button now runs the macro

**Each line is one command, executed top to bottom:**

| Command | What it does | Example |
|---|---|---|
| `press` | Presses the button and releases it (default 60 ms) | `press A` · `press A,B` · `press X 150ms` |
| `hold` | **Holds** the button until a `release` (or the macro ends) | `hold LT` |
| `release` | Releases a button that was held | `release LT` |
| `delay` | Waits, then continues with the next line (`wait` also works) | `delay 200ms` · `delay 1.5s` |

Tips:
- Press several buttons **at once** with commas and no spaces: `press A,B,X`
- Write time in `ms` (milliseconds) or `s` (seconds). No unit means milliseconds. Max per line is 5 minutes
- There's a tiny gap (20 ms) between consecutive lines
- Buttons that were held and never released are **released automatically** when the macro ends (or is stopped), so nothing gets stuck
- While a macro is running, tapping its button again does nothing. A **progress ring** fills around the button so you can see how much is left
- Up to 100 lines per macro and 30 macros per layout

**Button names you can use:**
`A` `B` `X` `Y` · `LB` `RB` · `LT` `RT` · `L3` `R3` · `BACK` `START` `GUIDE` · `DU` `DD` `DL` `DR` (D-Pad up, down, left, right)

PS4 names work too: `cross`=A · `circle`=B · `square`=X · `triangle`=Y · `L1`=LB · `R1`=RB · `L2`=LT · `R2`=RT · `share`/`select`=BACK · `options`=START · `ps`/`home`=GUIDE · `up` `down` `left` `right`

**Example 1: a simple combo**
```
press X
delay 100ms
press X
delay 100ms
press Y 120ms
```

**Example 2: sprint (hold the trigger), jump, then let go**
```
hold LT
delay 800ms
press A
delay 300ms
release LT
```

**Example 3: several buttons at once (e.g. a special move)**
```
press A,B
delay 50ms
press DL,X 200ms
```

> You can restyle macros too (button color, border, text, fill color) under "Appearance → Macro".

---

<a id="en-install"></a>

## 📥 Install & Use

### 1) Install
Download the right installer from **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)**:

| OS | File |
|---|---|
| Windows 10 / 11 | `HSHGamepad-Setup-Win10-11-x64.exe` or `...-x86.exe` |
| Windows 7 / 8.1 | `HSHGamepad-Setup-Win7-8.1-x64.exe` or `...-x86.exe` |

The installer sets up the **ViGEmBus** driver (needed for virtual controllers) and other prerequisites for you. If unsure, `x64` is almost always right.

> Windows 10/11 already include .NET Framework 4.8. On Windows 7 / 8.1 the installer downloads it.

### 2) Run
1. Open the app and accept the administrator prompt
2. **Phone and PC must be on the same Wi-Fi network**
3. Type the address shown in the app into your phone's browser, or scan the **QR code**
4. In the phone settings choose your name, player number and controller type
5. Launch your game, done! 🎉

### 3) Good to know
- If you turn on the **secret address**, use the full address with the key (the QR code already has it)
- Each player number can only be used by **one phone**
- Windows limit: **Xbox 360 has only 4 slots**. Players 5–9 must be PS4 (if Xbox can't be created, it automatically falls back to PS4)
- Mouse & keyboard and **Touch Screen** are **Player 1 only**
- The gyroscope is used in **PS4 mode** and in the **steering wheel** mode
- Opening the address on the PC that runs the server is blocked on purpose, use your phone

---

<a id="en-faq"></a>

## ❓ FAQ

<details>
<summary><b>Installed, but the game doesn't detect the controller</b></summary>

Make sure the ViGEmBus driver is installed and you restarted the PC after installation. Check the game's controller settings too, since some games detect Xbox controllers more reliably.
</details>

<details>
<summary><b>My phone can't connect</b></summary>

- Phone and PC must be on the same Wi-Fi
- Windows Firewall may block it, so allow the app on **Private networks** (the app also checks this and offers to add a rule for all networks)
- If you use a **VPN** or virtual machines, the app shows your real Wi-Fi / Ethernet IP; if it still looks wrong, type the IP from `ipconfig` yourself
- With the **secret address** on, the address must include the key, otherwise the page looks like it doesn't exist
- Too many wrong tries can get an IP **temporarily banned**; wait, or turn the ban off in the server window
- If port 80 is busy the app picks another port (e.g. 1111); type the full address including the port
- Guest/public Wi-Fi networks often block device-to-device traffic
</details>

<details>
<summary><b>Antivirus / SmartScreen shows a warning</b></summary>

The app uses low-level Windows APIs to create virtual controllers and simulate mouse/keyboard input, and runs as administrator, so some antivirus products or SmartScreen may warn. The code is fully open, so you can review and build it yourself. Only download from this GitHub page.
</details>

<details>
<summary><b>How secure is it?</b></summary>

The phone ↔ PC link stays on your local network. Since v1.0.5 you can turn on a **secret address (access key)** so only people who know the full address can connect, and **auto ban** blocks IPs that keep guessing. Without the key, anyone on the same Wi-Fi who knows the address can connect. See [SECURITY.md](SECURITY.md) for details. On public or crowded networks, turn on the key and turn off "mouse & keyboard access from phone".
</details>

---

<a id="en-roadmap"></a>

## 🗺️ Roadmap / Ideas

- [x] Optional **secret address / key** and auto ban for connecting a phone *(added in v1.0.5)*
- [x] **Multi-language** support *(added in v1.0.5)*
- [x] **Touch Screen** mode *(added in v1.0.5)*
- [ ] Code signing for the installer and exe (fewer SmartScreen / antivirus warnings)
- [ ] Demo video and more screenshots

Have an idea? [Open an issue](https://github.com/Amirhoshein/hshgamepad/issues/new/choose) or tell us on Telegram.

---

## 💬 Contact & Support

- Telegram channel (tutorials, videos, updates): **[@HSHGamepad](https://t.me/HSHGamepad)**
- Bugs & suggestions: [Issues](https://github.com/Amirhoshein/hshgamepad/issues)
- Security problems: see [SECURITY.md](SECURITY.md)
- Changes between versions: [CHANGELOG.md](CHANGELOG.md) · Want to help: [CONTRIBUTING.md](CONTRIBUTING.md)

---

<a id="en-dev"></a>

## 👨‍💻 For Developers & the Curious

- **Language / framework:** C# on **.NET Framework 4.8** (WinForms). The exe is a few hundred KB and doesn't bundle a runtime
- **Web server:** hand-written on raw TCP + **WebSocket** (`WebServer.cs`, `WsStream.cs`), no Kestrel / ASP.NET
- **Virtual controllers:** [ViGEmBus](https://github.com/nefarius/ViGEmBus) via the `Nefarius.ViGEm.Client` package
- **Mouse & keyboard:** `SendInput` (`InputInjector.cs`), including Unicode typing
- **Touch Screen:** DXGI Desktop Duplication (GDI fallback) → JPEG or H.264 (Media Foundation, fMP4 over MSE) over the same WebSocket
- **Phone UI:** a single `index.html` (plain HTML + CSS + JS, no libraries, no requests to the internet) embedded in the exe
- **Protocol:** lightweight WebSocket messages (`Protocol.cs`), slot management in `Hub.cs` and `Slot.cs`
- **Project layout:**

```
Gamepad/
├─ index.html          phone UI (controller, editor, themes, wheel, mouse/keyboard)
├─ MainForm.cs         main window, tray, QR
├─ GamepadTesterForm.cs  3D controller tester
├─ InputInjector.cs    virtual mouse/keyboard
├─ PortGuard.cs        port selection and the "port 80 is busy" helper
├─ AccessGuard.cs     secret address (key) and auto ban
├─ FirewallGuard.cs   firewall check for the current network
├─ Lang.cs, LanguageForm.cs   language system and language window
├─ lang/              language files (JSON, 12 built in)
├─ ScreenCast.cs      Touch Screen: capture, streaming, touch commands
├─ DxgiCapture.cs     fast screen capture (Desktop Duplication)
├─ H264Encoder.cs     H.264 live video (Media Foundation)
├─ JumpList.cs         taskbar right-click menu
├─ Elevation.cs        admin check and UAC relaunch
├─ net48/              .NET Framework 4.8 project (server, protocol, hub, ...)
├─ build.bat           build the app + installers
└─ installer/          Inno Setup script
```

**Build from source** (needs .NET SDK 6+, Inno Setup 6.3+, internet on first run):

```bat
build.bat net48     :: small installers
build.bat full      :: "Full" installers (offline .NET 4.8 bundled)
build.bat           :: everything
```

Output goes to `dist\`. Just the app:

```bat
dotnet build net48\GamepadServer.Net48.csproj -c Release -p:Arch=x64
```

Pull requests and suggestions are always welcome ⭐ (see [CONTRIBUTING.md](CONTRIBUTING.md))

---

<a id="fa"></a>

<div dir="rtl">

# 🇮🇷 فارسی

**فهرست:** [شروع سریع](#fa-quickstart) · [تازه‌ها](#fa-new) · [امکانات](#fa-features) · [تاخیر](#fa-latency) · [ماکرو](#fa-macros) · [نصب و استفاده](#fa-install) · [سوالات متداول](#fa-faq) · [نقشه‌ی راه](#fa-roadmap) · [برنامه‌نویس‌ها](#fa-dev)

## HSH Gamepad چیه؟

یه برنامه‌ی ساده برای ویندوز که گوشی‌ت رو تبدیل می‌کنه به **دسته‌ی بازی واقعی**. نیازی به نصب اپ روی گوشی نیست؛ فقط یه آدرس رو تو مرورگر گوشی باز می‌کنی و همین. بازی‌ها فکر می‌کنن یه دسته‌ی واقعی Xbox 360 یا PS4 به کامپیوتر وصل کردی.

**کلمات کلیدی:** گوشی به‌جای دسته · دسته‌ی ژیروسکوپی · کنترل حرکتی (Motion Controls) · فرمون ژیروسکوپی · Xbox 360 و DualShock 4 مجازی · گوشی به‌جای ماوس و کیبورد · بازی چندنفره تا ۹ نفر · گوشی به‌جای نمایشگر لمسی · چندزبانه · ViGEmBus

مناسب وقتی که:
- دسته نداری و می‌خوای با دوستات **دونفره، چهارنفره یا حتی ۹ نفره** بازی کنی 🎮
- می‌خوای از **ژیروسکوپ** گوشی برای کنترل حرکتی و به‌عنوان **فرمون ماشین** استفاده کنی 🏎️
- می‌خوای از گوشی **ماوس و کیبورد** کامپیوتر رو کنترل کنی (مثل لمس‌پد لپ‌تاپ) 🖱️
- می‌خوای تصویر **مانیتور کامپیوتر رو زنده روی گوشی** ببینی و مثل **نمایشگر لمسی** کنترلش کنی 🖥️📱
- می‌خوای برنامه **به زبان خودت** باشه (۱۲ زبان آماده) 🌍
- می‌خوای اتصال **خصوصی** بمونه؛ با آدرس مخفی و بن خودکار کسایی که حدس می‌زنن 🔐

<a id="fa-quickstart"></a>

## 🚀 شروع سریع (۳ مرحله)

1. **دانلود و نصب:** نصب‌کننده‌ی مناسب ویندوزت رو از **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)** بگیر (درایور ViGEmBus رو خودش نصب می‌کنه)
2. **برنامه رو اجرا کن** و دسترسی ادمین رو تایید کن. مطمئن شو **گوشی و کامپیوتر به یک وای‌فای وصلن**
3. **QR Code** داخل برنامه رو اسکن کن (یا آدرس نمایش‌داده‌شده رو تو مرورگر گوشی بزن)، شماره‌ی پلیر و نوع دسته رو انتخاب کن و بازی رو باز کن 🎉

> جزئیات، اتصال با کابل USB و رفع مشکل‌ها پایین‌تر نوشته شده.

---

<a id="fa-new"></a>

## 🆕 تازه‌های نسخه‌ی ۱.۰.۵

> از **۱.۰.۴** آپدیت می‌کنی؟ همه‌ی چیزهایی که اضافه یا درست شده اینجاست. جزئیات کامل تو [CHANGELOG.md](CHANGELOG.md) هست.

| | |
|---|---|
| 🌍 **چندزبانه** | ۱۲ زبان آماده برای برنامه‌ی ویندوز و صفحه‌ی گوشی، با پنجره‌ی انتخاب زبان، ایمپورت / اکسپورت و ترجمه‌ی دلخواه خودت |
| 🔐 **آدرس مخفی + بن خودکار** | کلید اختیاری داخل آدرس، و بن موقت آی‌پی‌هایی که حدس اشتباه می‌زنن |
| 🖥️ **حالت نمایشگر لمسی** | مانیتور کامپیوتر رو زنده روی گوشی ببین و با لمس کنترلش کن (پلیر ۱) |
| 🌐 **تشخیص هوشمند IP** | رفع باگ: آدرس نمایش‌داده‌شده دیگه به کارت شبکه‌ی VPN / VMware / Hyper-V نمی‌پره و گوشی دوباره وصل میشه |
| 🧱 **کمک فایروال** | تشخیص اینکه فایروال ویندوز شبکه‌ی فعلی رو بسته و پیشنهاد رفعش |
| 🧬 **ویرایش JSON چیدمان** | کل چیدمان دسته رو به‌صورت JSON ببین، کپی کن، ویرایش کن و بچسبون؛ برای بکاپ یا فرستادن برای دوستات |
| ⌨️ **تایپ بهتر** | هر متنی (فارسی و بقیه‌ی یونیکدها هم) رو با کیبورد گوشی تایپ کن |

### 🌍 چندزبانه
- **۱۲ زبان آماده:** English، فارسی، Deutsch، Español، Français، Italiano، Português، Русский، Türkçe، 中文، 日本語، 한국어
- **برنامه‌ی ویندوز، منوی tray، پنجره‌ها و صفحه‌ی گوشی** همه با زبان انتخابی نشون داده میشن
- **پنجره‌ی زبان** جدید تو برنامه: هر زبان یه کارت داره + **Import / Export / باز کردن پوشه**
- زبان‌ها فایل‌های **JSON** ساده تو پوشه‌ی `lang` کنار exe هستن. یکی رو کپی کن، ترجمه کن، بنداز تو پوشه و تو لیست ظاهر میشه (اگه کلیدی کم باشه از انگلیسی استفاده میشه، پس هیچ‌وقت متن خالی نمی‌بینی)
- عوض کردن زبان تو برنامه‌ی ویندوز همون لحظه اعمال میشه. تو گوشی فقط کافیه **صفحه رو رفرش کنی** تا زبانش هم عوض بشه
- **نصب‌کننده** هم زبان می‌پرسه و انتخابت اولین بار اجرا، زبان برنامه میشه

**اضافه کردن زبان خودت (JSON دلخواه):**
1. پنجره‌ی زبان ← **باز کردن پوشه** (یا برو سراغ پوشه‌ی `lang` کنار exe)
2. فایل `en.json` رو کپی کن، هر اسمی خواستی بذار و متن‌ها رو ترجمه کن. بخش `meta` (با `code` و `name`) و بخش `strings` رو نگه دار
3. **Import** رو بزن (یا فایل رو بنداز رو پنجره‌ی زبان یا داخل پوشه). زبانت به‌صورت یه کارت جدید میاد؛ روش بزن تا اعمال بشه
4. با **Export** فایل زبان رو ذخیره کن و برای بقیه بفرست

### 🧬 ویرایش JSON چیدمان
علاوه بر JSON تم، حالا **چیدمان** رو هم می‌تونی به‌صورت JSON ویرایش کنی.
- تو ویرایش چیدمان **ویرایش JSON** رو بزن و **همین دسته** یا **همه‌ی دسته‌ها** (PS4 / Xbox و فرمون) رو انتخاب کن
- کد، جای هر دکمه، اندازه و تنظیماتش رو نشون میده. **کپی** کن برای بکاپ یا فرستادن برای دوستت، یا چیدمان یکی دیگه رو **بچسبون**
- اگه خواستی دستی ویرایشش کن، با **اعمال** ببینش، با **برگشت** بر می‌گردی و آخرش **ذخیره** رو بزن
- اگه کد مال نوع دسته‌ی دیگه‌ای باشه، برنامه بهت میگه و چیدمانت رو خراب نمی‌کنه

### 🔐 امنیت اتصال
- **آدرس مخفی (کلید دسترسی):** گوشی فقط با آدرسی وصل میشه که کلیدت توشه، مثل `http://192.168.1.5/q2szc`. هر آدرس دیگه‌ای شبیه سایتی میشه که وجود نداره
- دکمه‌ی **کلید تصادفی** برای ساخت کلید جدید هر وقت خواستی. QR Code و آدرس نمایش‌داده‌شده همیشه کلید رو دارن
- **بن خودکار:** بعد از N تلاش اشتباه، آی‌پی برای M دقیقه بلاک میشه. هر دو عدد قابل تنظیمه و می‌تونی بن رو خاموش کنی
- هر دو گزینه تو پنجره‌ی سرور هستن. جزئیات تو [SECURITY.md](SECURITY.md)

### 🖥️ حالت نمایشگر لمسی (مدیریت مانیتور)
گوشی رو به‌عنوان **نمای زنده‌ی مانیتور کامپیوتر** استفاده کن و با لمس، کامپیوتر رو کنترل کن.
- **مانیتور** مورد نظرت رو انتخاب کن (کامپیوتر چندمانیتوره هم پشتیبانی میشه)
- **کیفیت تصویر** و **FPS** قابل تنظیمه؛ محدودیت واقعی همیشه کمترِ بین کامپیوتر و گوشی (رزولوشن / هرتز) هست
- ویدیوی زنده‌ی **H.264** (حجم کم، fps بالاتر) و اگه مرورگر یا کامپیوتر نتونست، خودکار برمی‌گرده روی **JPEG**
- ضبط سریع با **Desktop Duplication (DXGI)** تو ویندوز ۸ به بالا (تمام‌صفحه‌ی بازی‌ها رو هم می‌گیره) و برگشت خودکار به GDI تو ویندوز ۷
- گزینه‌ی **نمایش / مخفی کردن نشانگر ماوس** تو تصویر و تنظیم **بافر** فریم (بافر بیشتر = روان‌تر و fps بالاتر ولی کمی تاخیر)
- **کنترل لمسی:** لمس کوتاه = کلیک چپ · نگه داشتن = کلیک راست · درگ با یک انگشت = جابه‌جایی پنجره و فایل · دو انگشت = اسکرول · پینچ = زوم (Ctrl + چرخ ماوس)
- **تایپ با کیبورد گوشی** یا **کیبورد کامل**، با متن یونیکد (فارسی هم) و مستقل از زبان کیبورد ویندوز
- فقط برای **پلیر ۱** و فقط وقتی «دسترسی ماوس و کیبورد از گوشی» تو برنامه‌ی ویندوز روشنه

### 🌐 رفع باگ‌ها و بهبودها
- **عوض شدن IP هاست / وصل نشدن گوشی:** برنامه حالا کارت‌های شبکه‌ی مجازی (VPN، VMware، VirtualBox، Hyper-V، WSL، ZeroTier، Hamachi و ...) رو نادیده می‌گیره و IP **وای‌فای / Ethernet واقعی** رو نشون میده. آدرس‌های APIPA (169.254.x.x) هم رد میشن
- **فایروال:** ویندوز برای شبکه‌های Domain / Private / Public قانون جدا نگه می‌داره و با عوض کردن وای‌فای ممکن بود بی‌صدا جلوی گوشی رو بگیره. برنامه حالا شبکه‌ی فعلی رو چک می‌کنه و با اجازه‌ی تو یه قانون برای همه‌ی شبکه‌ها اضافه می‌کنه
- **راهنمای پورت:** پنجره‌های «پورت ۸۰ اشغاله» کامل ترجمه شدن و اسم برنامه‌ای که پورت رو گرفته رو نشون میدن
- **ماوس و کیبورد:** حالت‌های جدید برای حرکت همزمان ماوس و کیبورد، تاخیر تپ‌ودرگ و «نگه داشتن دکمه‌های اصلی مثل Alt با دابل‌کلیک»
- نصب‌کننده‌های ویندوز ۷ / ۸.۱ و ۱۰ / ۱۱ همون قبلین: اسم فایل‌ها عوض نشده

---

<a id="fa-features"></a>

## ✨ همه‌ی امکانات، یکجا

### 🎮 دسته‌ها
- شبیه‌سازی **PS4 (DualShock 4)** و **Xbox 360**؛ هر کدوم رو خواستی انتخاب کن
- **تا ۹ پلیر همزمان**، هر پلیر با رنگ مخصوص خودش
- **لرزش دسته**؛ وقتی بازی لرزش بفرسته، گوشی‌ت می‌لرزه (قابل خاموش کردن)
- **بدون نیاز به نصب اپ روی گوشی**؛ فقط مرورگر
- اسم دلخواه برای هر پلیر و نشانگر **پینگ** زنده (سبز / زرد / قرمز)
- حالت تمام‌صفحه

### 🎯 ژیروسکوپ و کنترل حرکتی
- **پشتیبانی کامل از ژیروسکوپ گوشی**: سنسور حرکتی گوشی به‌عنوان **ورودی حرکتی دسته‌ی PS4 (DualShock 4)** به کامپیوتر فرستاده میشه (حالت PS4)
- **فرمون با ژیروسکوپ**: گوشی رو مثل یه فرمون واقعی بچرخون
  - تنظیم **زاویه‌ی چرخش**
  - **حساسیت** و **ضریب (N-degree)**
  - **ناحیه‌ی مرده** (Dead Zone)
  - **نرمی حرکت** (Smoothing)
  - **تنظیم مرکز** (Center Calibration)
  - **پدال گاز/ترمز** آنالوگ یا دیجیتال
  - ظاهر فرمون (گرادینت) کاملاً قابل شخصی‌سازیه
- **لاگ زنده‌ی ژیروسکوپ** تو تستر دسته‌ی ویندوز؛ برای اینکه مطمئن بشی داده‌ها درست می‌رسن

### 🌍 زبان‌ها
- **۱۲ زبان آماده** برای برنامه‌ی ویندوز و صفحه‌ی گوشی، با امکان ترجمه‌ی JSON دلخواه ([جزئیات](#fa-new))

### 🔐 امنیت
- **آدرس مخفی** و **بن خودکار** تلاش‌های اشتباه ([جزئیات](#fa-new))

### 🖥️ نمایشگر لمسی (فقط پلیر ۱)
- **مانیتور کامپیوتر روی گوشی**، انتخاب مانیتور، H.264 / JPEG، تنظیم کیفیت و FPS و کنترل کامل لمسی ([جزئیات](#fa-new))

### 🖱️ ماوس و کیبورد (فقط پلیر ۱)
- **صفحه‌ی لمسی** مثل لپ‌تاپ روی گوشی
- **تپ** = کلیک · **دو انگشت** = اسکرول · **تپ دو انگشتی** = کلیک راست · **درگ**
- **حساسیت**، **سرعت اسکرول** و **تاخیر تپ‌ودرگ** قابل تنظیم
- **کیبورد کامل** و تایپ با کیبورد گوشی (یونیکد / فارسی هم پشتیبانی میشه)
- **کلید روشن/خاموش** تو برنامه‌ی ویندوز برای دسترسی ماوس و کیبورد از گوشی (برای امنیت بیشتر)

### 🛠️ آزادی کامل در ویرایش دسته
- **ویرایش چیدمان**: هر دکمه رو نگه دار و هرجا خواستی ولش کن
- **اندازه‌ی دقیق** (عرض، طول، اندازه‌ی متن) و **شفافیت** هر دکمه
- **کپی، حذف و اضافه کردن** دکمه
- **حالت‌های مختلف D-Pad و کنترل دوم**: دکمه‌ای، دکمه‌ی جدا (مثل اکشن)، جوی‌استیک ثابت یا دنبال‌کننده، استیک راست ↔ دکمه‌ای، دکمه‌های اکشن جدا یا ضربدری
- **ماکرو**: هر دکمه‌ای می‌تونه یه دنباله از کارها اجرا کنه (`press`، `hold`، `release`، `delay`)
- **ویرایش JSON چیدمان**: چیدمان رو به‌صورت کد کپی، ویرایش و بچسبون؛ برای بکاپ یا اشتراک ([جزئیات](#fa-new))
- **ذخیره، انصراف و بازگردانی چیدمان** تا هر وقت خواستی

### 🎨 ظاهر و تم
- ده‌ها **تم آماده** + ساخت تم خودت
- **ویرایش JSON تم**: کد تم رو کپی کن، دستکاری کن، یا تم دوستت رو بچسبون و اعمال کن
- رنگ هر چیزی جدا تنظیم میشه: پس‌زمینه، دکمه‌ها، متن، بوردر، ضخامت بوردر، سایه، **نئون (درخشش)**، رنگ لحظه‌ی فشردن، دکمه‌های جهت، فرمون (گرادینت)، استیک‌ها، دکمه‌های اکشن، ماکرو و ...
- پس‌زمینه‌ی شطرنجی، **افکت سه‌بعدی** (نورپردازی) و انیمیشن نرم فشردن دکمه‌ها
- **حالت سبک** برای گوشی‌های ضعیف (بلور و بعضی انیمیشن‌ها خاموش میشن تا تاخیر کم بشه)

### 💻 برنامه‌ی ویندوز
- نمایش **آدرس اتصال** + دکمه‌ی کپی + **QR Code**؛ فقط اسکن کن
- لیست پلیرهای وصل‌شده (اسم، مدل گوشی و آی‌پی)
- **تستر دسته**: پنجره‌ی سه‌بعدی که لحظه‌ای نشون میده چه دکمه‌ای زده میشه + لاگ ژیروسکوپ + **تست لرزش**
- **حذف خودکار دسته‌ی رها شده** بعد از چند ثانیه (اختیاری)
- **آیکون tray** کنار ساعت؛ با بستن پنجره برنامه همون‌جا می‌مونه
- منوی راست‌کلیک آیکون تسک‌بار با گزینه‌ی **«خروج از برنامه»**
- **فقط یک نسخه** اجرا میشه (اجرای دوباره، همون پنجره رو نشون میده)
- **انتخاب خودکار پورت** وقتی پورت ۸۰ اشغاله (IIS، Apache، Skype، ...) + نمایش راهنمای حل مشکل
- **پنجره‌ی زبان** (۱۲ زبان، ایمپورت / اکسپورت زبان خودت)
- **تشخیص هوشمند IP** که VPN و کارت‌های مجازی رو رد می‌کنه + **چک فایروال** برای شبکه‌ی فعلی
- اجرای خودکار با **دسترسی ادمین** (برای اینکه ماوس/کیبورد به برنامه‌های ادمین مثل Task Manager هم برسه)

### ⚡ اتصال
- تاخیر **۲ تا ۶ میلی‌ثانیه** (اندازه‌گیری سمت دستگاه، بخش [تاخیر](#fa-latency) رو ببین)
- اتصال با **وای‌فای** یا **کابل USB (USB Tethering)** برای کمترین تاخیر
- پشتیبانی از ویندوز **۷، ۸.۱، ۱۰ و ۱۱** (x86 و x64)

---

<a id="fa-latency"></a>

## ⚡ تاخیر (Latency)

تاخیر خود برنامه، از لحظه‌ی لمس صفحه‌ی گوشی تا اجرا شدن توی کامپیوتر، بین **۲ تا ۶ میلی‌ثانیه** هست. این عدد با **دوربین ۲۴۰ هرتز** اندازه‌گیری شده و **زمان رفت‌وآمد داخل شبکه رو شامل نمیشه** (یعنی تاخیر شبکه‌ی لوکال = ۰ فرض شده).

**تاخیر واقعی‌ای که حس می‌کنی = این عدد + تاخیر شبکه‌ی خودت.** تاخیر وای‌فای به قدرت سیگنال، تداخل و روتر بستگی داره، ولی کابل USB خیلی ثابت‌تره.

> 🎥 فیلم تست با دوربین **۲۴۰ هرتز** تو کانال تلگرام آپلود شده: [@HSHGamepad](https://t.me/HSHGamepad)

برای **کمترین تاخیر ممکن**، گوشی رو با **کابل USB** به کامپیوتر وصل کن و **USB Tethering** رو روشن کن (جزئیات پایین‌تر). اگه نخواستی از کابل استفاده کنی:
- گوشی رو به **وای‌فای ۵ گیگاهرتز** وصل کن و تا جای ممکن نزدیک روتر باش
- کامپیوتر رو اگه میشه با **کابل شبکه** به روتر وصل کن
- تو گوشی «**حالت سبک**» رو روشن کن
- برای ماوس، «تاخیر تپ‌ودرگ» رو روی ۰ بذار

### 🔌 اتصال با کابل USB (کمترین تاخیر)
1. گوشی رو با کابل USB به کامپیوتر وصل کن
2. **اندروید:** تنظیمات ← اتصال و اشتراک (Hotspot & tethering) ← **USB Tethering** رو روشن کن
   **آیفون:** تنظیمات ← Personal Hotspot رو روشن کن (برای اتصال با کابل، درایور Apple/iTunes باید روی ویندوز نصب باشه)
3. ویندوز یه شبکه‌ی جدید می‌سازه. آدرسی که برنامه نشون میده (یا آی‌پی کارت شبکه‌ی جدید تو `ipconfig`) رو تو مرورگر گوشی باز کن
4. اگه آدرس برنامه مال شبکه‌ی قبلیته و باز نشد، آی‌پی همون کارت شبکه‌ی جدید (معمولاً اسمش Ethernet یا Remote NDIS داره) رو دستی تو مرورگر گوشی وارد کن

> کابل تو اتصال وای‌فای (هوا، تداخل، روتر) نیست و تاخیرش ثابت‌تره؛ برای بازی‌های سریع (مبارزه‌ای، شوتر، ریتمیک) بهترین انتخابه.

### 📶 چرا بلوتوث نداره؟
عمداً بلوتوث اضافه نشده. تاخیر بلوتوث نسبت به وای‌فای و کابل **خیلی بیشتره** و تو تست‌ها حسش به‌خاطر لگ و تاخیر خوب نبود، و کاربرای زیادی هم اصلاً ازش استفاده نمی‌کنن. برای همین پروژه رو روی روش‌هایی (وای‌فای و USB) گذاشتیم که تاخیر خیلی کمی دارن.

---

<a id="fa-macros"></a>

## 🧩 آموزش ماکرو

ماکرو یعنی **با یه لمس، یه دنباله از کارها خودکار اجرا بشه**؛ مثلاً یه کمبو تو بازی مبارزه‌ای، یا فشردن چند دکمه پشت هم.

**ساخت ماکرو:**
1. **ویرایش چیدمان** رو بزن ← **افزودن ماکرو**؛ یه دکمه‌ی جدید با اسم `M1` (و `M2` و ...) ساخته میشه
2. روی دکمه‌ی ماکرو بزن ← **ویرایش ماکرو**
3. خط‌های دستور رو اضافه کن (با دکمه‌های **Press / Hold / Release / Delay** یا دستی بنویس)
4. **ذخیره** رو بزن و از حالت ویرایش خارج شو. حالا با لمس اون دکمه، ماکرو اجرا میشه

**هر خط یه دستوره و به ترتیب، از بالا به پایین اجرا میشه:**

| دستور | کارش | مثال |
|---|---|---|
| `press` | دکمه رو فشار میده و ولش می‌کنه (پیش‌فرض ۶۰ میلی‌ثانیه) | `press A` · `press A,B` · `press X 150ms` |
| `hold` | دکمه رو **نگه می‌داره** تا وقتی که `release` بشه (یا ماکرو تموم بشه) | `hold LT` |
| `release` | دکمه‌ای که `hold` شده رو ول می‌کنه | `release LT` |
| `delay` | صبر می‌کنه؛ بعد میره خط بعدی (اسم‌های `wait` هم قبول میشه) | `delay 200ms` · `delay 1.5s` |

نکته‌ها:
- چند دکمه رو **همزمان** بزن: با کاما بنویس، بدون فاصله‌ی اضافه: `press A,B,X`
- زمان رو با `ms` (میلی‌ثانیه) یا `s` (ثانیه) بنویس. اگه یکا ننویسی میلی‌ثانیه حساب میشه. بیشترین زمان هر خط ۵ دقیقه‌ست
- بین هر دو خط یه فاصله‌ی خیلی کوتاه (۲۰ میلی‌ثانیه) هست
- دکمه‌هایی که `hold` بشن و `release` نشن، آخر ماکرو (یا اگه ماکرو رو قطع کنی) **خودکار ول میشن**؛ جایی گیر نمی‌کنن
- تا ماکرو تموم نشده، لمس دوباره‌ی همون دکمه کاری نمی‌کنه. دور دکمه یه **حلقه‌ی پیشرفت** پر میشه تا ببینی چقدر مونده
- هر ماکرو تا ۱۰۰ خط و هر دسته تا ۳۰ ماکرو داره

**اسم دکمه‌هایی که می‌تونی بنویسی:**
`A` `B` `X` `Y` · `LB` `RB` · `LT` `RT` · `L3` `R3` · `BACK` `START` `GUIDE` · `DU` `DD` `DL` `DR` (جهت‌های بالا، پایین، چپ، راست)

اسم‌های PS4 هم قبول میشن: `cross`=A · `circle`=B · `square`=X · `triangle`=Y · `L1`=LB · `R1`=RB · `L2`=LT · `R2`=RT · `share`/`select`=BACK · `options`=START · `ps`/`home`=GUIDE · `up` `down` `left` `right`

**مثال ۱: یه کمبو ساده**
```
press X
delay 100ms
press X
delay 100ms
press Y 120ms
```

**مثال ۲: دویدن (ماشه رو نگه دار)، بپر و ولش کن**
```
hold LT
delay 800ms
press A
delay 300ms
release LT
```

**مثال ۳: چند دکمه همزمان (مثلاً حمله‌ی ویژه)**
```
press A,B
delay 50ms
press DL,X 200ms
```

> ماکرو رو می‌تونی رنگ‌بندی هم بکنی (رنگ دکمه، بوردر، متن و رنگ پر شدن) از «تنظیمات ظاهری ← ماکرو».

---

<a id="fa-install"></a>

## 📥 نصب و استفاده

### ۱) نصب
از بخش **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)** نصب‌کننده‌ی مناسب سیستمت رو دانلود کن:

| سیستم‌عامل | نسخه |
|---|---|
| ویندوز ۱۰ و ۱۱ | `HSHGamepad-Setup-Win10-11-x64.exe` یا `...-x86.exe` |
| ویندوز ۷ و ۸.۱ | `HSHGamepad-Setup-Win7-8.1-x64.exe` یا `...-x86.exe` |

نصب‌کننده درایور **ViGEmBus** (لازم برای ساخت دسته‌ی مجازی) و بقیه‌ی پیش‌نیازها رو خودش نصب می‌کنه. اگه نمی‌دونی ۳۲ یا ۶۴ بیتی هستی، معمولاً `x64` درسته.

> ویندوز ۱۰ و ۱۱ از قبل .NET Framework 4.8 دارن. ویندوز ۷ و ۸.۱ رو نصب‌کننده خودش دانلود می‌کنه.

### ۲) اجرا
1. برنامه رو باز کن و دسترسی ادمین رو تایید کن
2. **گوشی و کامپیوتر باید به یک وای‌فای (یک شبکه) وصل باشن**
3. آدرسی که تو پنجره‌ی برنامه نوشته رو تو مرورگر گوشی وارد کن، یا **QR Code** رو اسکن کن
4. تو تنظیمات گوشی اسم، شماره‌ی پلیر و نوع دسته رو انتخاب کن
5. بازی رو باز کن؛ تموم! 🎉

### ۳) نکته‌ها
- اگه **آدرس مخفی** رو روشن کردی، آدرس کامل با کلید رو وارد کن (QR Code از قبل کلید رو داره)
- هر شماره‌ی پلیر فقط **یه گوشی** می‌تونه داشته باشه
- محدودیت ویندوز: **Xbox 360 فقط ۴ دسته** داره. پلیر ۵ تا ۹ باید PS4 باشن (اگه Xbox ساخته نشه، خودکار PS4 میشه)
- ماوس و کیبورد و **نمایشگر لمسی** فقط برای **پلیر ۱** هست
- ژیروسکوپ تو **حالت PS4** و **حالت فرمون** استفاده میشه
- باز کردن آدرس روی خود کامپیوتر (همون سیستمی که برنامه روشه) عمداً بلاک شده؛ از گوشی وارد شو

---

<a id="fa-faq"></a>

## ❓ سوالات متداول

<details>
<summary><b>برنامه نصب شد ولی بازی دسته رو نمی‌شناسه</b></summary>

مطمئن شو درایور ViGEmBus نصب شده و کامپیوتر بعد از نصب ریستارت شده. تو بازی هم گزینه‌ی کنترلر رو چک کن؛ بعضی بازی‌ها دسته‌ی Xbox رو راحت‌تر می‌شناسن.
</details>

<details>
<summary><b>گوشی به آدرس وصل نمیشه</b></summary>

- گوشی و کامپیوتر باید به یک وای‌فای وصل باشن
- فایروال ویندوز ممکنه جلوی اتصال رو بگیره؛ تو پنجره‌ی اجازه‌ی فایروال «Private networks» رو تایید کن (برنامه خودش هم چک می‌کنه و پیشنهاد میده یه قانون برای همه‌ی شبکه‌ها اضافه کنه)
- اگه پورت ۸۰ اشغاله، برنامه پورت دیگه‌ای (مثلاً ۱۱۱۱) انتخاب می‌کنه؛ آدرس کامل رو با پورت وارد کن
- بعضی وای‌فای‌ها (مثل مهمان یا عمومی) ارتباط بین دستگاه‌ها رو می‌بندن
- اگه **VPN** یا ماشین مجازی داری، برنامه IP واقعی وای‌فای / Ethernet رو نشون میده؛ اگه بازم اشتباه بود، IP رو از `ipconfig` خودت وارد کن
- وقتی **آدرس مخفی** روشنه، آدرس باید کلید رو داشته باشه وگرنه صفحه وجود نداره
- تلاش اشتباه زیاد باعث **بن موقت** آی‌پی میشه؛ صبر کن یا بن رو تو پنجره‌ی سرور خاموش کن
</details>

<details>
<summary><b>آنتی‌ویروس یا SmartScreen هشدار میده</b></summary>

برنامه برای ساخت دسته‌ی مجازی و شبیه‌سازی ماوس و کیبورد از APIهای سطح پایین ویندوز استفاده می‌کنه و با دسترسی ادمین اجرا میشه، برای همین ممکنه بعضی آنتی‌ویروس‌ها یا SmartScreen هشدار بدن. کدش کاملاً بازه و می‌تونی خودت بررسی و بیلد کنی. فقط از همین صفحه‌ی گیت‌هاب دانلود کن.
</details>

<details>
<summary><b>امنیت برنامه چطوره؟</b></summary>

ارتباط گوشی و کامپیوتر فقط تو شبکه‌ی خودته. از نسخه‌ی ۱.۰.۵ می‌تونی **آدرس مخفی (کلید دسترسی)** رو روشن کنی تا فقط کسایی که آدرس کامل رو دارن وصل بشن، و **بن خودکار** آی‌پی‌هایی که مدام حدس می‌زنن رو بلاک می‌کنه. بدون کلید، هر کسی که تو همون وای‌فای باشه و آدرس رو بدونه می‌تونه وصل بشه. جزئیات و راهکار تو [SECURITY.md](SECURITY.md) نوشته شده. اگه تو شبکه‌ی عمومی یا شلوغ بازی می‌کنی، کلید رو روشن کن و گزینه‌ی «دسترسی ماوس و کیبورد از گوشی» رو خاموش کن.
</details>

---

<a id="fa-roadmap"></a>

## 🗺️ نقشه‌ی راه / ایده‌ها

- [x] **آدرس مخفی / کلید** اختیاری برای اتصال گوشی + بن خودکار *(تو نسخه‌ی ۱.۰.۵ اضافه شد)*
- [x] پشتیبانی **چندزبانه** *(تو نسخه‌ی ۱.۰.۵ اضافه شد)*
- [x] حالت **نمایشگر لمسی** *(تو نسخه‌ی ۱.۰.۵ اضافه شد)*
- [ ] امضای دیجیتال (Code Signing) برای نصب‌کننده و برنامه (هشدار کمتر SmartScreen و آنتی‌ویروس)
- [ ] ویدیوی دمو و اسکرین‌شات‌های بیشتر

ایده‌ای داری؟ [یه Issue باز کن](https://github.com/Amirhoshein/hshgamepad/issues/new/choose) یا تو تلگرام بگو.

---

## 💬 ارتباط و پشتیبانی

- کانال تلگرام (آموزش، فیلم‌ها و آپدیت‌ها): **[@HSHGamepad](https://t.me/HSHGamepad)**
- باگ و پیشنهاد: [Issues](https://github.com/Amirhoshein/hshgamepad/issues)
- مشکل امنیتی: [SECURITY.md](SECURITY.md) رو بخون
- تغییرات نسخه‌ها: [CHANGELOG.md](CHANGELOG.md) · می‌خوای کمک کنی: [CONTRIBUTING.md](CONTRIBUTING.md)

---

<a id="fa-dev"></a>

## 👨‍💻 بخش مخصوص برنامه‌نویس‌ها و کنجکاوها

- **زبان و فریم‌ورک:** C# روی **.NET Framework 4.8** (WinForms). فایل exe فقط چند صد کیلوبایته و ران‌تایم همراهش نیست
- **وب‌سرور:** پیاده‌سازی دستی روی TCP خام + **WebSocket** (`WebServer.cs`، `WsStream.cs`)؛ بدون Kestrel / ASP.NET
- **دسته‌ی مجازی:** [ViGEmBus](https://github.com/nefarius/ViGEmBus) از طریق پکیج `Nefarius.ViGEm.Client`
- **ماوس و کیبورد:** `SendInput` (`InputInjector.cs`) با تایپ یونیکد
- **نمایشگر لمسی:** DXGI Desktop Duplication (با GDI به‌عنوان جایگزین) ← JPEG یا H.264 (Media Foundation، fMP4 روی MSE) روی همون WebSocket
- **رابط گوشی:** یک فایل `index.html` (HTML + CSS + JS خالص، بدون کتابخونه و بدون هیچ درخواستی به اینترنت) که داخل exe جاسازی (Embedded) میشه
- **پروتکل:** پیام‌های سبک روی WebSocket (`Protocol.cs`)، مدیریت اسلات‌ها در `Hub.cs` و `Slot.cs`
- **ساختار پروژه:**

```
Gamepad/
├─ index.html          رابط گوشی (دسته، ویرایشگر، تم‌ها، فرمون، ماوس/کیبورد)
├─ MainForm.cs         پنجره‌ی اصلی، tray، QR
├─ GamepadTesterForm.cs  تستر سه‌بعدی دسته
├─ InputInjector.cs    ماوس/کیبورد مجازی
├─ PortGuard.cs        انتخاب پورت و راهنمای «پورت ۸۰ اشغاله»
├─ AccessGuard.cs     آدرس مخفی (کلید) و بن خودکار
├─ FirewallGuard.cs   چک فایروال برای شبکه‌ی فعلی
├─ Lang.cs, LanguageForm.cs   سیستم زبان و پنجره‌ی زبان
├─ lang/              فایل‌های زبان (JSON، ۱۲ زبان آماده)
├─ ScreenCast.cs      نمایشگر لمسی: ضبط، پخش، دستورهای لمسی
├─ DxgiCapture.cs     ضبط سریع صفحه (Desktop Duplication)
├─ H264Encoder.cs     ویدیوی زنده‌ی H.264 (Media Foundation)
├─ JumpList.cs         منوی راست‌کلیک تسک‌بار
├─ Elevation.cs        چک ادمین و اجرای دوباره با UAC
├─ net48/              پروژه‌ی .NET Framework 4.8 (سرور، پروتکل، Hub، ...)
├─ build.bat           بیلد برنامه + ساخت نصب‌کننده‌ها
└─ installer/          اسکریپت Inno Setup
```

**ساخت از سورس** (نیاز: .NET SDK 6 یا جدیدتر، Inno Setup 6.3+، اینترنت برای بار اول):

```bat
build.bat net48     :: نصب‌کننده‌های کوچک
build.bat full      :: نصب‌کننده‌های Full (با .NET 4.8 آفلاین)
build.bat           :: همه
```

خروجی تو پوشه‌ی `dist\` ساخته میشه. فقط خود برنامه:

```bat
dotnet build net48\GamepadServer.Net48.csproj -c Release -p:Arch=x64
```

Pull Request و پیشنهاد همیشه خوش‌آمده ⭐ (فایل [CONTRIBUTING.md](CONTRIBUTING.md) رو ببین)

</div>

---

<div align="center">

**HSH Gamepad v1.0.5** · [Telegram @HSHGamepad](https://t.me/HSHGamepad) · [GitHub](https://github.com/Amirhoshein/hshgamepad)

</div>
