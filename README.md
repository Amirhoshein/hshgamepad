<div align="center">

<img src="icon.ico" width="96" alt="HSH Gamepad">

# HSH Gamepad

**Turn your phone into a Gamepad (PS4 / Xbox 360 controller), gyroscope steering wheel, mouse & keyboard for your PC**
**گوشی‌ت رو تبدیل کن به دسته‌ی بازی PS4 / Xbox 360 / فرمون ژیروسکوپی / ماوس و کیبورد برای کامپیوتر**

![Version](https://img.shields.io/badge/version-1.0.4-blue)
[![Downloads](https://img.shields.io/github/downloads/Amirhoshein/hshgamepad/total?color=3b5a99)](https://github.com/Amirhoshein/hshgamepad/releases)
![Platform](https://img.shields.io/badge/Windows-7%20%7C%208.1%20%7C%2010%20%7C%2011-0078D6)
![Players](https://img.shields.io/badge/players-up%20to%209-brightgreen)
![Latency](https://img.shields.io/badge/latency-2--6%20ms-orange)
![Gyroscope](https://img.shields.io/badge/gyroscope-supported-purple)

[**English**](#en) · [**فارسی**](#fa) · [Download / دانلود](https://github.com/Amirhoshein/hshgamepad/releases/latest) · [Telegram @HSHGamepad](https://t.me/HSHGamepad)

</div>

---

<a id="en"></a>

# 🇬🇧 English

## What is HSH Gamepad?

A simple Windows app that turns your phone into a **real game controller** for your PC. No phone app needed: just open an address in your phone's browser. Games see a genuine Xbox 360 or PS4 controller plugged into the computer.

**Keywords:** phone as gamepad · gyroscope controller · motion controls · gyro steering wheel · virtual Xbox 360 / DualShock 4 · phone as mouse & keyboard · local multiplayer up to 9 players · ViGEmBus

Great for:
- Playing **local multiplayer with up to 9 players** without owning any controllers 🎮
- Using your phone's **gyroscope** for motion controls and as a **steering wheel** 🏎️
- Controlling your PC's **mouse & keyboard** from the phone, like a laptop touchpad 🖱️

---

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

### 🖱️ Mouse & Keyboard (Player 1 only)
- Laptop-style **touchpad** on the phone
- **Tap** = click · **two fingers** = scroll · **two-finger tap** = right click · **tap-and-drag**
- Adjustable **sensitivity**, **scroll speed** and **tap-and-drag delay**
- **On/off switch** on the Windows app for phone mouse & keyboard access (extra safety)

### 🛠️ Total Freedom in Layout Editing
- **Layout editor**: press and hold any button, drop it wherever you want
- **Exact size** (width, height, font size) and **opacity** per button
- **Duplicate, delete and add** buttons
- **Different D-Pad / second-control modes**: buttons, separated buttons (action style), fixed or following joystick, right-stick ↔ buttons, separated or diamond action buttons
- **Macros**: any button can run a sequence of actions (`press`, `hold`, `release`, `delay`)
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
- Runs with **administrator rights**, so mouse/keyboard input can reach admin apps like Task Manager

### ⚡ Connection
- **2 to 6 ms** end-to-end latency
- Works over **Wi-Fi** or a **USB cable (USB tethering)** for the lowest latency
- Supports Windows **7, 8.1, 10 and 11** (x86 and x64)

---

## ⚡ Latency

End-to-end latency from touching the phone screen to the input being applied on the PC is **2 ms to 6 ms**, assuming **zero local network latency**.

> 🎥 A test video recorded with a **240 Hz** camera is available on the Telegram channel: [@HSHGamepad](https://t.me/HSHGamepad)

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
- Each player number can only be used by **one phone**
- Windows limit: **Xbox 360 has only 4 slots**. Players 5–9 must be PS4 (if Xbox can't be created, it automatically falls back to PS4)
- Mouse & keyboard is **Player 1 only**
- The gyroscope is used in **PS4 mode** and in the **steering wheel** mode
- Opening the address on the PC that runs the server is blocked on purpose, use your phone

---

## ❓ FAQ

<details>
<summary><b>Installed, but the game doesn't detect the controller</b></summary>

Make sure the ViGEmBus driver is installed and you restarted the PC after installation. Check the game's controller settings too, since some games detect Xbox controllers more reliably.
</details>

<details>
<summary><b>My phone can't connect</b></summary>

- Phone and PC must be on the same Wi-Fi
- Windows Firewall may block it, so allow the app on **Private networks**
- If port 80 is busy the app picks another port (e.g. 1111); type the full address including the port
- Guest/public Wi-Fi networks often block device-to-device traffic
</details>

<details>
<summary><b>Antivirus / SmartScreen shows a warning</b></summary>

The app uses low-level Windows APIs to create virtual controllers and simulate mouse/keyboard input, and runs as administrator, so some antivirus products or SmartScreen may warn. The code is fully open, so you can review and build it yourself. Only download from this GitHub page.
</details>

<details>
<summary><b>How secure is it?</b></summary>

The phone ↔ PC link stays on your local network and has **no password**; anyone on the same Wi-Fi who knows the address can connect. See [SECURITY.md](SECURITY.md) for details. On public or crowded networks, turn off "mouse & keyboard access from phone".
</details>

---

## 💬 Contact & Support

- Telegram channel (tutorials, videos, updates): **[@HSHGamepad](https://t.me/HSHGamepad)**
- Bugs & suggestions: [Issues](https://github.com/Amirhoshein/hshgamepad/issues)
- Security problems: see [SECURITY.md](SECURITY.md)

---

## 👨‍💻 For Developers & the Curious

- **Language / framework:** C# on **.NET Framework 4.8** (WinForms). The exe is a few hundred KB and doesn't bundle a runtime
- **Web server:** hand-written on raw TCP + **WebSocket** (`WebServer.cs`, `WsStream.cs`), no Kestrel / ASP.NET
- **Virtual controllers:** [ViGEmBus](https://github.com/nefarius/ViGEmBus) via the `Nefarius.ViGEm.Client` package
- **Mouse & keyboard:** `SendInput` (`InputInjector.cs`)
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

Pull requests and suggestions are always welcome ⭐

---

<a id="fa"></a>

<div dir="rtl">

# 🇮🇷 فارسی

## HSH Gamepad چیه؟

یه برنامه‌ی ساده برای ویندوز که گوشی‌ت رو تبدیل می‌کنه به **دسته‌ی بازی واقعی**. نیازی به نصب اپ روی گوشی نیست؛ فقط یه آدرس رو تو مرورگر گوشی باز می‌کنی و همین. بازی‌ها فکر می‌کنن یه دسته‌ی واقعی Xbox 360 یا PS4 به کامپیوتر وصل کردی.

**کلمات کلیدی:** گوشی به‌جای دسته · دسته‌ی ژیروسکوپی · کنترل حرکتی (Motion Controls) · فرمون ژیروسکوپی · Xbox 360 و DualShock 4 مجازی · گوشی به‌جای ماوس و کیبورد · بازی چندنفره تا ۹ نفر · ViGEmBus

مناسب وقتی که:
- دسته نداری و می‌خوای با دوستات **دونفره، چهارنفره یا حتی ۹ نفره** بازی کنی 🎮
- می‌خوای از **ژیروسکوپ** گوشی برای کنترل حرکتی و به‌عنوان **فرمون ماشین** استفاده کنی 🏎️
- می‌خوای از گوشی **ماوس و کیبورد** کامپیوتر رو کنترل کنی (مثل لمس‌پد لپ‌تاپ) 🖱️

---

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

### 🖱️ ماوس و کیبورد (فقط پلیر ۱)
- **صفحه‌ی لمسی** مثل لپ‌تاپ روی گوشی
- **تپ** = کلیک · **دو انگشت** = اسکرول · **تپ دو انگشتی** = کلیک راست · **درگ**
- **حساسیت**، **سرعت اسکرول** و **تاخیر تپ‌ودرگ** قابل تنظیم
- **کلید روشن/خاموش** تو برنامه‌ی ویندوز برای دسترسی ماوس و کیبورد از گوشی (برای امنیت بیشتر)

### 🛠️ آزادی کامل در ویرایش دسته
- **ویرایش چیدمان**: هر دکمه رو نگه دار و هرجا خواستی ولش کن
- **اندازه‌ی دقیق** (عرض، طول، اندازه‌ی متن) و **شفافیت** هر دکمه
- **کپی، حذف و اضافه کردن** دکمه
- **حالت‌های مختلف D-Pad و کنترل دوم**: دکمه‌ای، دکمه‌ی جدا (مثل اکشن)، جوی‌استیک ثابت یا دنبال‌کننده، استیک راست ↔ دکمه‌ای، دکمه‌های اکشن جدا یا ضربدری
- **ماکرو (میکرو)**: هر دکمه‌ای می‌تونه یه دنباله از کارها اجرا کنه (`press`، `hold`، `release`، `delay`)
- **ذخیره، انصراف و بازگردانی چیدمان** تا هر وقت خواستی

### 🎨 ظاهر و تم
- ده‌ها **تم آماده** + ساخت تم خودت
- **ویرایش JSON تم**: کد تم رو کپی کن، دستکاری کن، یا تم دوستت رو بچسبون و اعمال کن
- رنگ هر چیزی جدا تنظیم میشه: پس‌زمینه، دکمه‌ها، متن، بوردر، ضخامت بوردر، سایه، **نئون (درخشش)**، رنگ لحظه‌ی فشردن، دکمه‌های جهت، فرمون (گرادینت)، استیک‌ها، دکمه‌های اکشن، میکرو و ...
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
- اجرای خودکار با **دسترسی ادمین** (برای اینکه ماوس/کیبورد به برنامه‌های ادمین مثل Task Manager هم برسه)

### ⚡ اتصال
- تاخیر **۲ تا ۶ میلی‌ثانیه**
- اتصال با **وای‌فای** یا **کابل USB (USB Tethering)** برای کمترین تاخیر
- پشتیبانی از ویندوز **۷، ۸.۱، ۱۰ و ۱۱** (x86 و x64)

---

## ⚡ تاخیر (Latency)

تاخیر کلی از لحظه‌ی لمس صفحه‌ی گوشی تا اجرا شدن توی کامپیوتر، **با فرض تاخیر شبکه‌ی لوکال = ۰**، بین **۲ تا ۶ میلی‌ثانیه** هست.

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

## 🧩 آموزش ماکرو (میکرو)

ماکرو یعنی **با یه لمس، یه دنباله از کارها خودکار اجرا بشه**؛ مثلاً یه کمبو تو بازی مبارزه‌ای، یا فشردن چند دکمه پشت هم.

**ساخت ماکرو:**
1. **ویرایش چیدمان** رو بزن ← **افزودن میکرو**؛ یه دکمه‌ی جدید با اسم `M1` (و `M2` و ...) ساخته میشه
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

> ماکرو رو می‌تونی رنگ‌بندی هم بکنی (رنگ دکمه، بوردر، متن و رنگ پر شدن) از «تنظیمات ظاهری ← میکرو».

---

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
- هر شماره‌ی پلیر فقط **یه گوشی** می‌تونه داشته باشه
- محدودیت ویندوز: **Xbox 360 فقط ۴ دسته** داره. پلیر ۵ تا ۹ باید PS4 باشن (اگه Xbox ساخته نشه، خودکار PS4 میشه)
- ماوس و کیبورد فقط برای **پلیر ۱** هست
- ژیروسکوپ تو **حالت PS4** و **حالت فرمون** استفاده میشه
- باز کردن آدرس روی خود کامپیوتر (همون سیستمی که برنامه روشه) عمداً بلاک شده؛ از گوشی وارد شو

---

## ❓ سوالات متداول

<details>
<summary><b>برنامه نصب شد ولی بازی دسته رو نمی‌شناسه</b></summary>

مطمئن شو درایور ViGEmBus نصب شده و کامپیوتر بعد از نصب ریستارت شده. تو بازی هم گزینه‌ی کنترلر رو چک کن؛ بعضی بازی‌ها دسته‌ی Xbox رو راحت‌تر می‌شناسن.
</details>

<details>
<summary><b>گوشی به آدرس وصل نمیشه</b></summary>

- گوشی و کامپیوتر باید به یک وای‌فای وصل باشن
- فایروال ویندوز ممکنه جلوی اتصال رو بگیره؛ تو پنجره‌ی اجازه‌ی فایروال «Private networks» رو تایید کن
- اگه پورت ۸۰ اشغاله، برنامه پورت دیگه‌ای (مثلاً ۱۱۱۱) انتخاب می‌کنه؛ آدرس کامل رو با پورت وارد کن
- بعضی وای‌فای‌ها (مثل مهمان یا عمومی) ارتباط بین دستگاه‌ها رو می‌بندن
</details>

<details>
<summary><b>آنتی‌ویروس یا SmartScreen هشدار میده</b></summary>

برنامه برای ساخت دسته‌ی مجازی و شبیه‌سازی ماوس و کیبورد از APIهای سطح پایین ویندوز استفاده می‌کنه و با دسترسی ادمین اجرا میشه، برای همین ممکنه بعضی آنتی‌ویروس‌ها یا SmartScreen هشدار بدن. کدش کاملاً بازه و می‌تونی خودت بررسی و بیلد کنی. فقط از همین صفحه‌ی گیت‌هاب دانلود کن.
</details>

<details>
<summary><b>امنیت برنامه چطوره؟</b></summary>

ارتباط گوشی و کامپیوتر فقط تو شبکه‌ی خودته و رمز ندارد؛ هر کسی که تو همون وای‌فای باشه و آدرس رو بدونه می‌تونه وصل بشه. جزئیات و راهکار تو [SECURITY.md](SECURITY.md) نوشته شده. اگه تو شبکه‌ی عمومی یا شلوغ بازی می‌کنی، گزینه‌ی «دسترسی ماوس و کیبورد از گوشی» رو خاموش کن.
</details>

---

## 💬 ارتباط و پشتیبانی

- کانال تلگرام (آموزش، فیلم‌ها و آپدیت‌ها): **[@HSHGamepad](https://t.me/HSHGamepad)**
- باگ و پیشنهاد: [Issues](https://github.com/Amirhoshein/hshgamepad/issues)
- مشکل امنیتی: [SECURITY.md](SECURITY.md) رو بخون

---

## 👨‍💻 بخش مخصوص برنامه‌نویس‌ها و کنجکاوها

- **زبان و فریم‌ورک:** C# روی **.NET Framework 4.8** (WinForms). اکسه فقط چند صد کیلوبایته و ران‌تایم همراهش نیست
- **وب‌سرور:** پیاده‌سازی دستی روی TCP خام + **WebSocket** (`WebServer.cs`، `WsStream.cs`)؛ بدون Kestrel / ASP.NET
- **دسته‌ی مجازی:** [ViGEmBus](https://github.com/nefarius/ViGEmBus) از طریق پکیج `Nefarius.ViGEm.Client`
- **ماوس و کیبورد:** `SendInput` (`InputInjector.cs`)
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

Pull Request و پیشنهاد همیشه خوش‌آمده ⭐

</div>

---

<div align="center">

**HSH Gamepad v1.0.4** · [Telegram @HSHGamepad](https://t.me/HSHGamepad) · [GitHub](https://github.com/Amirhoshein/hshgamepad)

</div>
