<div align="center">

<img src="../HSH Gamepad/icon.ico" width="96" alt="HSH Gamepad">

# HSH Gamepad

**Turn your phone into a Gamepad (PS4 / Xbox 360 controller), gyroscope steering wheel, mouse & keyboard and a touch screen for your PC**

![Version](https://img.shields.io/badge/version-1.0.6-blue)
[![Downloads](https://img.shields.io/github/downloads/Amirhoshein/hshgamepad/total?color=3b5a99)](https://github.com/Amirhoshein/hshgamepad/releases)
![Platform](https://img.shields.io/badge/Windows-7%20%7C%208.1%20%7C%2010%20%7C%2011-0078D6)
![Players](https://img.shields.io/badge/players-up%20to%209-brightgreen)
![Latency](https://img.shields.io/badge/latency-2--6%20ms-orange)
![Gyroscope](https://img.shields.io/badge/gyroscope-supported-purple)
![Languages](https://img.shields.io/badge/languages-12-teal)
![Touch Screen](https://img.shields.io/badge/phone%20as%20monitor-new-red)
![Android App](https://img.shields.io/badge/Android%20app-APK-3DDC84)

[**🇬🇧 English**](README.md) · [**🇮🇷 فارسی**](README.fa.md) · [🔗 Telegram @HSHGamepad](https://t.me/HSHGamepad)

<br>

<img src="https://github.com/user-attachments/assets/3290b7a3-bf2c-4dee-b915-f1c5bd43968a" alt="HSH Gamepad Demo" width="900">

<!-- Demo video: drag the mp4 file into the README editor on GitHub, then put the link it creates here:
<video src="VIDEO_LINK_HERE" controls width="700"></video>
-->

<br>

[![Download for Windows](https://img.shields.io/badge/_-Download_Now-2ea44f?style=for-the-badge)](https://github.com/Amirhoshein/hshgamepad/releases/latest)

</div>

---

## ⚡ HSH Gamepad at a glance

- 🎮 **Virtual PS4 (DualShock 4) and Xbox 360 controllers**, up to **9 players**
- 🎯 **Gyroscope:** motion controls, a steering wheel, and tilt-to-steer for any controller
- 🖱️ **Mouse & keyboard** from the phone, like a laptop touchpad
- 🖥️ **Touch Screen:** your PC screen live on the phone (H.264 / JPEG, multi-monitor), with **PC sound** on the phone
- 🎤 **Phone as a PC microphone**
- 🛠️ **Layout editor** with macros, ready-made themes, layout / theme **JSON**, and saving them **on the PC**
- 📱 **Optional Android app** that finds your PC, scans the QR and plays fullscreen (or just use the browser)
- 🌍 **12 languages** · 🔐 **secret address + auto ban** · ⚡ **2–6 ms** latency over Wi-Fi or USB

**Contents:** [Quick Start](#en-quickstart) · [What's New](#en-new-106) · [Android App](#en-android) · [Features](#en-features) · [Latency](#en-latency) · [Macros](#en-macros) · [Install & Use](#en-install) · [FAQ](#en-faq) · [Roadmap](#en-roadmap)

## What is HSH Gamepad?

A simple Windows app that turns your phone into a **real game controller** for your PC. No phone app needed: just open an address in your phone's browser (or use the optional [Android app](#en-android)). Games see a genuine Xbox 360 or PS4 controller plugged into the computer.

**Keywords:** phone as gamepad · gyroscope controller · motion controls · gyro steering wheel · virtual Xbox 360 / DualShock 4 · phone as mouse & keyboard · local multiplayer up to 9 players · phone as touch screen / second monitor · multi-language · Android app · ViGEmBus

Great for:
- Playing **local multiplayer with up to 9 players** without owning any controllers 🎮
- Using your phone's **gyroscope** for motion controls and as a **steering wheel** 🏎️
- Controlling your PC's **mouse & keyboard** from the phone, like a laptop touchpad 🖱️
- Seeing your PC screen **live on the phone** and using it as a **touch screen** 🖥️📱
- Using the app in **your own language** (12 built-in) 🌍
- Hearing your **PC's sound on the phone** and using the phone as a **PC microphone** 🔊🎤
- Keeping the connection **private** with a secret address and automatic ban of guessers 🔐

<a id="en-quickstart"></a>

## 🚀 Quick Start (3 steps)

1. **Download & install:** get the installer for your Windows from **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)** (it installs the ViGEmBus driver for you)
2. **Run the app** and accept the administrator prompt. Make sure the **phone and PC are on the same Wi-Fi**
3. **Scan the QR code** in the app (or type the shown address into your phone's browser, or use the [Android app](#en-android)), choose your player and controller type, and launch your game 🎉

> Details, USB-cable mode and troubleshooting are below.

---

<a id="en-new-106"></a>

## 🆕 What's New in v1.0.6

> Upgrading from **v1.0.5**? Here is what was added. Full details are in [CHANGELOG.md](CHANGELOG.md).

| | |
|---|---|
| 📱 **Android app** | A small app that finds your PC, scans the QR code and plays fullscreen ([details](#en-android)) |
| 🔊 **PC sound on the phone** | New speaker button in Touch Screen mode, plus a sound-output picker in the Windows app |
| 🎤 **Phone microphone** | Use the phone as a PC microphone (player 1) |
| 💾 **Layouts & themes on the PC** | Save them on the computer; copy, rename, delete or share the files |
| 🏎️ **Gyro for left / right** | Tilt the phone to push the left stick left / right; works with every controller type |
| 🏎️ **Wheel: add after N degrees** | Adds a small value (e.g. 0.10) as soon as the wheel leaves the center |
| 🔀 **Swap buttons** | Tap a button 3 times within a second, then tap another one to swap places |
| 📳 **Vibrate on touch** | Optional phone vibration when you press a button, with adjustable strength |
| 🧪 **Test button** | A **Test** button for each phone in the player list |
| 👆 **Accurate touch areas** | Sticks, D-Pad and wheel only react inside their real shape |

### 🔊 PC sound on the phone
- In **Touch Screen** mode there is a **speaker button**; you hear the computer's sound on your phone
- In the Windows app choose **which sound output** is sent to the phone (default: the Windows default output)

### 🎤 Phone as a PC microphone (player 1 only)
- The phone's microphone shows up in Windows as a **microphone input**
- It uses a virtual microphone driver on the PC; if it is missing, the phone page tells you
- Option **"Keep the virtual microphone always connected"**: on = it stays in Windows even when the app is closed or the PC restarts; off = it appears only while it's in use
- Browsers block the microphone on `http` addresses: turn on **Insecure origins treated as secure** in `chrome://flags`, or use the [Android app](#en-android), which doesn't need it

### 💾 Layouts & themes on the PC
- Save your layouts and themes from the phone **on the computer**, so changing phone or browser doesn't lose them
- Open their folder from the Windows app to **copy, rename, delete or share** the files
- Any file you put in that folder also shows up in the phone's list

### 🏎️ Gyro for left / right and wheel
- **Gyro for left / right:** tilt the phone like a steering wheel and the left stick goes left / right. Great for racing games (like Need for Speed) with any controller type
- **Wheel:** the new **"Add after N degrees"** and **"Added value"** options add a small value to the output as soon as the wheel leaves the center (e.g. `0.10` = 10%). Useful for games that need a bit of input before they react

### 🛠️ Layout editing
- **Swap buttons:** in layout editing tap a button **3 times within 1 second**, then tap the second button to swap their places
- **Vibrate when touching buttons** is optional and its **strength** is adjustable
- **Touch areas fixed:** the empty square corners around the sticks, wheel and D-Pad used to catch touches and block nearby buttons. Touch areas now match the shapes you see
- The dashed outlines in layout editing are smaller and thinner on phones

---

<a id="en-new"></a>

## 🆕 Added in v1.0.5 (details)

> Upgrading from **v1.0.4**? Here is everything that was added and fixed. Full details are in [CHANGELOG.md](CHANGELOG.md).

| | |
|---|---|
| 🌍 **Multi-language** | 12 built-in languages for both the Windows app and the phone page, with a language picker, import / export and your own translations |
| 🔐 **Secret address + auto ban** | Optional secret key in the address, and automatic temporary ban of IPs that guess wrong |
| 🖥️ **Touch Screen mode** | See your PC monitor live on the phone and control it by touch (player 1) |
| 🌐 **Smarter IP detection** | Fixed: the shown address no longer jumps to a VPN / VMware / Hyper-V adapter, so the phone connects again |
| 🧱 **Firewall helper** | Detects when Windows Firewall blocks the current network and offers to fix it |
| 🎚️ **Analog triggers** | L2 / R2 can be slid like real triggers, so how far you press matters (great for gas in racing games) |
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

### 🎚️ Analog triggers (slidable L2 / R2)
On a real controller, how far you press L2 / R2 matters, for example in *Need for Speed* it sets how much gas you give. Now your phone can do it too.
- In the **layout editor**, tap **L2 / R2** and turn on **slide (drag) mode**
- Choose the **slide direction** that feels right
- The value shows live in the **controller tester** on Windows

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
- Choose **Touch Screen** from the controller type picker and the PC screen appears on the phone
- Pick the **monitor** you want to see. With **two or more monitors** a **switch button** appears to jump between them
- A **custom keyboard** is available right there from a button, and you can use it while viewing the screen
- Choose **image quality** and **FPS**; the real limit is always the lower of PC and phone resolution / Hz
- **H.264** live video (low bandwidth, higher fps) with automatic fallback to **JPEG** if the browser or PC can't do it
- Fast capture with **Desktop Duplication (DXGI)** on Windows 8 and later (it also captures fullscreen games), and an automatic GDI fallback on Windows 7
- Option to **show or hide the mouse pointer** in the image, and a frame **buffer** setting (more buffer = smoother and higher fps, a bit more delay)
- **Touch controls:** short touch = left click · hold = right click · one-finger drag = drag windows and files · two fingers = scroll · pinch = zoom (Ctrl + wheel)
- **Type with the phone keyboard** or use the **full keyboard**, with Unicode text (Persian too) independent of the Windows keyboard layout
- For **player 1 only**, and only when "Mouse & keyboard access from phone" is on in the Windows app

### 🌐 Fixes & improvements
- **Host IP changing / phone can't connect:** it no longer matters whether your **VPN is on or off**. The app now ignores VPN, VMware, VirtualBox, Hyper-V, WSL, ZeroTier, Hamachi and other virtual adapters, and shows the IP of your **real Wi-Fi / Ethernet** card. APIPA (169.254.x.x) addresses are skipped too
- **Firewall:** Windows keeps separate rules for Domain / Private / Public networks, so switching Wi-Fi could silently block the phone. The app now checks the current network and, with your permission, adds a rule for all networks
- **Port helper:** the "port 80 is busy" dialogs are fully translated and name the program that holds the port
- **Mouse & keyboard:** connect a keyboard to the phone (with a converter), choose **Mouse** as the controller type, and the keyboard now works **at the same time** as the mouse. Also new: tap-and-drag delay and "hold main keys like Alt on double-click"
- Windows 7 / 8.1 and 10 / 11 installers are unchanged: same file names as before

---

<a id="en-features"></a>

## ✨ All Features at a Glance

### 🎮 Controllers
- **PS4 (DualShock 4)** and **Xbox 360** emulation, pick whichever you like
- **Up to 9 players** at once, each with their own color
- **Analog L2 / R2:** slide the triggers like a real controller (direction adjustable)
- **Rumble / vibration**: when the game vibrates, your phone does too (can be turned off)
- **No app needed on the phone**: it works in the phone's browser (an optional [Android app](#en-android) is available too)
- Custom **name per player** and a live **ping indicator** (green / yellow / red)
- Fullscreen mode

### 🎯 Gyroscope & Motion Controls
- **Phone gyroscope support**: the phone's motion sensor is sent to the PC as the **DualShock 4 (PS4) motion input** (PS4 mode)
- **Gyroscope steering wheel**: tilt and rotate your phone like a real wheel
  - Full **rotation angle** setting
  - **Sensitivity** and **N-degree multiplier**
  - **Dead zone**
  - **Add after N degrees:** adds a small value (e.g. 0.10) as soon as the wheel leaves the center
  - **Smoothing** for soft, stable movement
  - **Center calibration**
  - **Gas / brake pedals**, analog or digital
  - Wheel look is fully customizable (gradient colors)
- **Live gyro log** in the Windows controller tester, so you can verify the data is arriving correctly
- **Gyro for left / right:** tilt the phone to push the left stick left / right, with any controller type (great for racing games)

### 🌍 Languages
- **12 built-in languages** for the Windows app and the phone page, with custom JSON translations ([details](#en-new))

### 🔐 Security
- **Secret address** and **auto ban** for wrong attempts ([details](#en-new))

### 🖥️ Touch Screen (Player 1 only)
- Live **PC monitor on the phone**, monitor picker, H.264 / JPEG, quality and FPS control, full touch control ([details](#en-new))

### 🔊 Sound & Microphone (Player 1 only)
- **PC sound on the phone** from the speaker button in Touch Screen mode, with a choice of sound output in the Windows app
- **Phone as a PC microphone**, with an option to keep the virtual microphone always connected ([details](#en-new-106))

### 💾 Layouts & Themes on the PC
- Save layouts and themes on the computer; copy, rename, delete or share the files from the folder the Windows app opens ([details](#en-new-106))

### 📱 Android App (optional)
- Finds your PC automatically, scans the QR code, remembers the last PC, plays fullscreen, and makes the microphone / gyroscope / vibration work without browser flags ([details](#en-android))

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
- **Swap two buttons** by tapping one 3 times within a second
- **Vibrate when touching a button** (optional), with adjustable strength
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
  > double click on a player, or press its **Test** button, to show the Controller tester
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
- Mouse & keyboard, **Touch Screen** (with its sound) and the **phone microphone** are **Player 1 only**
- The gyroscope is used in **PS4 mode** and in the **steering wheel** mode; **Gyro for left / right** works with every controller type
- Opening the address on the PC that runs the server is blocked on purpose, use your phone

---

<a id="en-android"></a>

## 📱 Android App (optional)

HSH Gamepad works in any phone browser, but there is also a small **Android app** that makes connecting and playing easier. It does **not** replace the Windows app: the PC app still has to be running.

### What it gives you
| | |
|---|---|
| 🔎 **Finds your PC by itself** | Scans your Wi-Fi / hotspot for the HSH Gamepad server and lists every PC it finds. Tap one to connect |
| 📷 **QR scanner** | Scan the QR code from the Windows app (the secret key is included) |
| ⌨️ **Manual address** | Type the IP, or the full address with the secret key (e.g. `192.168.1.5/q2szc`) |
| 🕘 **Remembers your last PC** | It is shown at the top of the list for one-tap reconnecting |
| 🖥️ **Fullscreen landscape** | Hides the status and navigation bars and keeps the screen on while you play |
| 🎤 🎯 📳 **Microphone, gyroscope and vibration just work** | No `chrome://flags` needed: the app opens the controller page through a small local proxy on the phone, so the browser treats it as a secure page |
| ⏳ **Loading screen** | A progress bar while the page loads and connects |
| ⬅️ **Smart Back button** | Closes the open panel (settings, appearance, macro editor...); press Back **twice** to leave the app |
| ❓ **Built-in guide** | The "?" button explains how to connect and has a button to download the Windows app |
| 🌍 **5 languages** | فارسی, English, Türkçe, Русский, 中文 |

### Install
1. Download the `.apk` file from **[Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest)**
2. Open it. Android may ask you to allow **installing from this source** (your browser or file manager)
3. If **Play Protect** says the app is unknown, that's because it isn't from a store: tap **Send this time** (send for a security check) and continue
4. It needs **Android 7.0 or newer**. On an iPhone, open the address in Safari instead

### Connect
1. Start HSH Gamepad on the PC. The phone and PC must be on the same Wi-Fi (or the PC connected to the phone's hotspot)
2. Open the app: your PC shows up in the list, tap it. If it doesn't, tap **Scan QR** or **Enter address**
3. Choose your player and controller type, and start the game 🎉

The app may ask for the **Camera** (to scan the QR) and the **Microphone** (only if you use the phone as a PC microphone).

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
<summary><b>Play Protect says the Android app is unknown / unsafe</b></summary>

Play Protect shows this for apps it hasn't scanned before and for apps that don't come from a store; it doesn't mean the app was found harmful. Tap **Send this time** (or **Scan app**) and continue the install. Only download the `.apk` from this GitHub page and compare its SHA-256 checksum. If your phone asks for "install from unknown sources", allow it for your browser or file manager.
</details>

<details>
<summary><b>The phone microphone doesn't work in my browser</b></summary>

Browsers block the microphone on `http` addresses. Either use the [Android app](#en-android) or open `chrome://flags`, turn on **Insecure origins treated as secure** and add the address the app shows. The microphone is for **player 1** only, and the PC needs its virtual microphone driver (the phone page tells you if it's missing).
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

Have an idea? [Open an issue](https://github.com/Amirhoshein/hshgamepad/issues/new/choose) or tell us on Telegram.

---

## 💬 Contact & Support

- Telegram channel (tutorials, videos, updates): **[@HSHGamepad](https://t.me/HSHGamepad)**
- Bugs & suggestions: [Issues](https://github.com/Amirhoshein/hshgamepad/issues)
- Security problems: see [SECURITY.md](SECURITY.md)
- Changes between versions: [CHANGELOG.md](CHANGELOG.md)

---

<div align="center">

**HSH Gamepad v1.0.6** · [Telegram @HSHGamepad](https://t.me/HSHGamepad) · [GitHub](https://github.com/Amirhoshein/hshgamepad)

</div>
