<div align="center">

# 🎮 HSH Gamepad

**گوشی‌ت رو تبدیل کن به دسته‌ی بازی ویندوز؛ بدون نصب هیچ برنامه‌ای روی گوشی!**

دسته‌ی مجازی **Xbox 360** و **PS4 (DualShock 4)** تا **۹ پلیر** همزمان، با کمترین تاخیر ممکن

[![Release](https://img.shields.io/github/v/release/Amirhoshein/hshgamepad?label=release&color=5b92ff)](https://github.com/Amirhoshein/hshgamepad/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Amirhoshein/hshgamepad/total?color=3b5a99)](https://github.com/Amirhoshein/hshgamepad/releases)
[![Telegram](https://img.shields.io/badge/Telegram-@HSHGamepad-26A5E4?logo=telegram&logoColor=white)](https://t.me/HSHGamepad)

[📥 دانلود](#-دانلود) · [💬 کانال تلگرام](https://t.me/HSHGamepad) · [English](#-english-summary)

</div>

<!--
عکس‌های برنامه رو اینجا بذار:
![صفحه‌ی گوشی](docs/phone.png)
![پنجره‌ی ویندوز](docs/server.png)
-->

---

## 📖 این برنامه چیه؟

**HSH Gamepad** یک برنامه‌ی دو‌تکه‌ست:

1. **برنامه‌ی ویندوز** که با درایور [ViGEmBus](https://github.com/nefarius/ViGEmBus) یک یا چند **دسته‌ی مجازی** می‌سازد؛ برای ویندوز و بازی‌ها دقیقاً مثل یک دسته‌ی واقعی Xbox 360 یا DualShock 4 دیده می‌شود.
2. **صفحه‌ی وب روی گوشی** که خودِ برنامه به گوشی می‌دهد. کافیه آدرسی که برنامه نشان می‌دهد را توی مرورگر گوشی باز کنی؛ **هیچ اپی روی گوشی نصب نمی‌شود**.

هر چیزی که روی صفحه‌ی گوشی لمس کنی (دکمه، استیک، دکمه‌های جهت و...) با پیام‌های متنیِ خیلی کوچک از طریق **WebSocket** به کامپیوتر می‌رسد و همان لحظه روی دسته‌ی مجازی اعمال می‌شود. هر بازی یا شبیه‌سازی که از دسته پشتیبانی کند (Steam، emulatorها و...) بدون هیچ تنظیم خاصی با آن کار می‌کند.

> 💡 مثال کاربرد: بازی با دوستات روی یک کامپیوتر (بازی‌های کاناپه‌ای)، وقتی دسته نداری یا تعداد دسته‌هات کمه. هر کس با گوشی خودش یک پلیر می‌شود.

---

## ✨ امکانات

### 🖥️ سمت کامپیوتر

| قابلیت | توضیح |
|---|---|
| 🎮 **تا ۹ پلیر** | هر شماره‌ی پلیر فقط یک گوشی می‌تواند داشته باشد؛ بقیه نمی‌توانند شماره‌ی گرفته‌شده را بردارند و در پنجره هم دیده می‌شود چه کسی کدام شماره را دارد |
| 🕹️ **Xbox 360 و PS4** | نوع دسته برای هر پلیر جدا انتخاب می‌شود. ⚠️ ویندوز (XInput) فقط **۴ دسته‌ی Xbox** پشتیبانی می‌کند؛ پلیر ۵ تا ۹ باید PS4 باشند. اگر Xbox ساخته نشود، برنامه خودکار PS4 می‌سازد |
| 🪟 **پنجره‌ی وضعیت** | تم تیره؛ جدول ۹ پلیر با وضعیت، اسم، نوع دسته، مدل دستگاه و IP هر گوشی |
| 🌐 **آدرس آماده‌ی اتصال** | بالای پنجره آدرس شبکه‌ی محلی نشان داده می‌شود و با یک دکمه کپی می‌شود |
| 🧹 **حذف دسته‌ی آزاد** | وقتی گوشی قطع می‌شود دسته‌ی مجازی می‌ماند؛ با دکمه‌ی «حذف» دستی یا با گزینه‌ی **حذف خودکار بعد از N ثانیه** پاک می‌شود (پیش‌فرض: هیچ‌وقت) |
| 🔌 **مدیریت هوشمند پورت** | پورت پیش‌فرض ۸۰ است (پس لازم نیست پورت را در آدرس بنویسی). اگر اشغال باشد برنامه به‌جای ارور، می‌گوید **کدام برنامه** پورت را گرفته، آموزش آزاد کردنش را نشان می‌دهد و اجازه می‌دهد آن را ببندی. اگر نخواستی، برنامه با پورت **۱۱۱۱** (و بعدی‌ها) ادامه می‌دهد و بدون بستن برنامه می‌تواند روی ۸۰ برگردد |
| 🔒 **امنیت پایه** | خودِ کامپیوتر سرور اجازه‌ی وصل شدن به دسته را ندارد (فقط گوشی‌ها) |
| 🔔 **Tray و Taskbar** | بستن پنجره برنامه را به کنار ساعت می‌برد؛ اجرای دوباره همان پنجره را نشان می‌دهد (فقط یک نسخه اجرا می‌شود). راست‌کلیک روی آیکون taskbar → «خروج از برنامه» |
| ⚡ **اولویت بالا** | برنامه با اولویت High اجرا می‌شود تا وقتی بازی CPU را می‌گیرد، عقب نماند |
| 🪟 **ویندوز ۷ تا ۱۱** | هم 32 بیتی هم 64 بیتی |

### 📱 سمت گوشی (صفحه‌ی وب)

- 🖐️ **چیدمان شبیه دسته‌ی Xbox**: دو استیک، D-Pad، دکمه‌های A/B/X/Y، LB/RB، LT/RT، L3/R3، Back/Start/Guide و (در حالت PS4) تاچ‌پد
- ✏️ **ویرایش چیدمان**: هر دکمه را جابه‌جا کن، اندازه‌اش را تا **۳۰۰٪** زیاد کن، **محدوده‌ی لمس** را (تا ۵۰۰٪) جدا تنظیم کن و **شفافیتش** را تا ۰٪ کم کن
- 🕹️ **حالت‌های استیک و D-Pad**: حالت دکمه‌ای، استیک کشیدنی و حالت **دنبال‌کننده** (مثل بازی‌های موبایل) که پایه‌ی استیک دنبال انگشت می‌آید؛ با **محدوده‌ی لمس قابل تنظیم**
- ❌ **دکمه‌های اکشن ضربدری (X-layout)**: چهار دکمه‌ی اکشن به‌جای چهار دکمه‌ی جدا، یک ناحیه‌ی لمسی می‌شوند که با قطرها به چهار قسمت تقسیم شده؛ انگشت می‌تواند بین‌شان بلغزد و دو دکمه‌ی کنارهم هم‌زمان فشرده شوند
- 🎨 **شخصی‌سازی کامل ظاهر**: رنگ دکمه‌ها، متن، بوردر و ضخامتش، سایه/نئون، رنگ پایه و توپ هر استیک، رنگ تک‌تک دکمه‌های اکشن، رنگ‌های ضربدری، رنگ و بلور پنل تنظیمات و رنگ پس‌زمینه؛ هر مورد دکمه‌ی بازگشت به پیش‌فرض دارد
- ↩️ **بازگشت به عقب (Undo)**: تا ۶۰ تغییر چیدمان ذخیره می‌شود. همچنین «کپی از بقیه» (کپی چیدمان گوشی‌های دیگر) و «بازگردانی چیدمان» به حالت کارخانه
- 📶 **نمایش پینگ** کنار دکمه‌ی تنظیمات (قابل جابه‌جایی)
- 📳 **لرزش** هنگام لمس (قابل خاموش کردن)
- 🖼️ **تمام‌صفحه‌ی خودکار** هنگام چرخاندن گوشی به حالت افقی
- 🔆 **روشن ماندن صفحه** تا گوشی وسط بازی خاموش نشود
- 👤 **اسم دلخواه** برای هر گوشی (فقط برای نمایش در پنجره‌ی برنامه)
- 💾 همه‌ی تنظیمات و چیدمان‌ها در خود مرورگر گوشی ذخیره می‌شوند

---

## 📥 دانلود

آخرین نسخه: **[v1.0.2](https://github.com/Amirhoshein/hshgamepad/releases/tag/hsh-gamepad-v1.0.2)**

فایل‌ها با فرمت **RAR** هستند؛ برای باز کردنشان به [7-Zip](https://www.7-zip.org/) یا WinRAR نیاز داری.

### کدوم نسخه رو بگیرم؟

- ویندوز ۱۰ یا ۱۱ → نسخه‌ی **Win10-11**
- ویندوز ۷ یا ۸.۱ → نسخه‌ی **Win7-8.1**
- ویندوزت ۶۴ بیتی است یا ۳۲ بیتی؟ (Settings ← System ← About) → `x64` یا `x86`
- نسخه‌ی **Full** همه‌ی پیش‌نیازها (مثل .NET Framework 4.8 و درایورها) را داخل خودش دارد و بدون اینترنت نصب می‌شود؛ حجمش بیشتر است. اگر اینترنت داری، نسخه‌ی معمولی (کم‌حجم) کافی است.

| سیستم | معمولی (کم‌حجم) | Full (آفلاین) |
|---|---|---|
| **ویندوز ۱۰/۱۱ — ۶۴ بیت** (رایج‌ترین) | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win10-11-x64.rar) | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win10-11-x64-Full.rar) |
| **ویندوز ۱۰/۱۱ — ۳۲ بیت** | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win10-11-x86.rar) | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win10-11-x86-Full.rar) |
| **ویندوز ۷/۸.۱ — ۶۴ بیت** | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win7-8.1-x64.rar) | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win7-8.1-x64-Full.rar) |
| **ویندوز ۷/۸.۱ — ۳۲ بیت** | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win7-8.1-x86.rar) | [دانلود](https://github.com/Amirhoshein/hshgamepad/releases/download/hsh-gamepad-v1.0.2/HSHGamepad-Setup-Win7-8.1-x86-Full.rar) |

> نسخه‌های ویندوز ۷/۸.۱ از ViGEmBus نسخه‌ی قدیمی‌تر (1.16.x) و بسته‌های لازم ویندوز ۷ استفاده می‌کنند.

<details>
<summary>🔐 چک‌سام SHA-256 فایل‌ها (برای مطمئن شدن از سالم بودن دانلود)</summary>

| فایل | SHA-256 |
|---|---|
| `HSHGamepad-Setup-Win10-11-x64.rar` | `3e59c8fdda5f43a433493a836f3a82f6b3b906fa76534f1b7f7842f16da051da` |
| `HSHGamepad-Setup-Win10-11-x64-Full.rar` | `130c2c432a03c266ed667a5ad8c5ef01200589e8a8278dd45a1d389f604edef6` |
| `HSHGamepad-Setup-Win10-11-x86.rar` | `cde6543dc6818c84a76fc13daceff096497a4635670942e811b78973a8d4962e` |
| `HSHGamepad-Setup-Win10-11-x86-Full.rar` | `df3d6bc85ff54d7bb99272de2707e365bca951686cd0e3d05e04c87be68b8083` |
| `HSHGamepad-Setup-Win7-8.1-x64.rar` | `4fbbf30053d21b030718c883a83b7a5aca7f0a94e985cc931f08794a7745696e` |
| `HSHGamepad-Setup-Win7-8.1-x64-Full.rar` | `172f8188554263367c763614162e3a83ce858bbfd0ed2bf53151c49d3519435d` |
| `HSHGamepad-Setup-Win7-8.1-x86-Full.rar` | `bc4c82a5e5ad0d9139881dffbea6e2fa5b8776dc2bf133d01097e4ee869c9cf9` |

برای بررسی در PowerShell:

```powershell
Get-FileHash .\HSHGamepad-Setup-Win10-11-x64.rar -Algorithm SHA256
```

</details>

---

## 🚀 راه‌اندازی سریع

1. نسخه‌ی مناسب را از بالا دانلود و با 7-Zip/WinRAR باز کن، بعد نصبش کن.
2. برنامه را باز کن؛ یک پنجره با **آدرس** (مثلاً `http://192.168.1.10`) نشان داده می‌شود.
3. گوشی و کامپیوتر باید روی **یک شبکه** باشند. آدرس را توی **مرورگر گوشی** باز کن و اسمت را بنویس.
4. از تنظیمات صفحه، شماره‌ی **پلیر** و **نوع دسته** (Xbox 360 / PS4) را انتخاب کن.
5. بازی را باز کن و لذت ببر 🎉

> 🔥 اگر گوشی به آدرس وصل نشد، فایروال ویندوز را برای برنامه (یا پورت ۸۰ / ۱۱۱۱) باز کن و مطمئن شو شبکه روی حالت «Private» است. برای کمترین تاخیر، گوشی را با **کابل USB** (Tethering) وصل کن.

### ⚙️ سیستم موردنیاز

| | |
|---|---|
| **سیستم‌عامل** | ویندوز ۷ SP1 تا ویندوز ۱۱ (۳۲ و ۶۴ بیت) |
| **CPU** | در بهترین حالت حدود ۰.۲ هسته |
| **RAM** | ۶۴ مگابایت |
| **شبکه** | Wi-Fi مناسب ۵GHz (یا کمی ضعیف‌تر ۲.۴GHz)؛ ترجیحاً گوشی را با کابل USB وصل کن |
| **درایور** | ViGEmBus (در نصب‌کننده‌ها همراه برنامه است) |

---

## 🧠 برنامه چطور کار می‌کند؟

```
┌──────────────┐   WebSocket (متن کوتاه)   ┌───────────────────┐   ViGEmBus   ┌──────────┐
│  گوشی (مرورگر) │ ───────────────────────▶ │  HSH Gamepad       │ ───────────▶ │  بازی‌ها   │
│               │ ◀─────────────────────── │  (روی ویندوز)       │  دسته‌ی مجازی │  Steam... │
└──────────────┘    وضعیت پلیرها / پینگ     └───────────────────┘              └──────────┘
```

- صفحه‌ی گوشی داخل خود برنامه جاسازی شده؛ پس لازم نیست چیزی جداگانه نصب یا دانلود کنی.
- ورودی‌ها بسته‌های متنیِ خیلی کوچک هستند و بعد از دریافت، یک‌جا به دسته‌ی مجازی اعمال می‌شوند؛ سوکت‌ها برای کمترین تاخیر تنظیم شده‌اند.
- اگر اتصال گوشی قطع شود، **همه‌ی دکمه‌های آن پلیر خودکار رها می‌شوند** (دکمه‌ی گیرکرده نداریم) و شماره‌ی پلیر آزاد می‌شود. اگر همان گوشی دوباره وصل شود، اتصال قبلی کنار می‌رود.
- دسته‌ی مجازی فقط وقتی ساخته می‌شود که لازم باشد.
- در حالت PS4 دکمه‌ها به معادل خودشان نگاشت می‌شوند (A→✕، B→○، X→□، Y→△، Back→Share، Start→Options، Guide→PS)، D-Pad به‌صورت ۸ جهت و تاچ‌پد هم قابل استفاده است.
- تنظیمات پنجره‌ی برنامه در `%AppData%\GamepadServer` ذخیره می‌شود.

---

## ❓ سوالات پرتکرار

<details>
<summary><b>گوشی به آدرس وصل نمی‌شود</b></summary>

- هر دو دستگاه باید روی یک شبکه باشند.
- فایروال ویندوز را بررسی کن و شبکه را روی Private بگذار.
- آدرسی را که **پنجره‌ی برنامه** نشان می‌دهد وارد کن (اگر پورت ۸۰ نبود، پورتش هم در آدرس هست).
- از روی خود کامپیوتر نمی‌شود به دسته وصل شد؛ فقط از گوشی.
</details>

<details>
<summary><b>پیام «ViGEmBus driver not found» می‌دهد</b></summary>

درایور ViGEmBus نصب نیست یا درست بالا نیامده؛ آن را از [اینجا](https://github.com/nefarius/ViGEmBus/releases) نصب کن و کامپیوتر را ری‌استارت کن.
</details>

<details>
<summary><b>پورت ۸۰ اشغال است</b></summary>

معمولاً IIS، Skype، XAMPP/Apache یا یک کپی دیگر همین برنامه است. برنامه خودش اسم و مسیر برنامه‌ی پورت‌گیر را نشان می‌دهد؛ می‌توانی آن را ببندی (برنامه دوباره روی ۸۰ می‌آید) یا با پورت ۱۱۱۱ ادامه بدهی.
</details>

<details>
<summary><b>چرا پلیر ۵ به بعد فقط PS4 است؟</b></summary>

ویندوز از طریق XInput بیشتر از ۴ دسته‌ی Xbox 360 را پشتیبانی نمی‌کند. این محدودیت ویندوز است، نه برنامه.
</details>

<details>
<summary><b>دکمه‌ها گیر می‌کنند یا دسته‌ی اضافه می‌ماند</b></summary>

با قطع شدن گوشی، همه‌ی دکمه‌ها خودکار رها می‌شوند. دسته‌ی مجازی بدون گوشی را می‌توانی با دکمه‌ی «حذف» پاک کنی یا «حذف خودکار» را در پنجره روشن کنی.
</details>

---

## 📜 مجوز و حقوق

© HSH Gamepad — **تمام حقوق محفوظ است.**

این برنامه **متن‌باز نیست** و سورس‌کد آن منتشر نمی‌شود. استفاده‌ی شخصی از آن رایگان است، اما بدون اجازه‌ی نویسنده:

- ❌ تغییر، دیکامپایل یا مهندسی معکوس برنامه
- ❌ انتشار مجدد، بازآپلود یا ارائه به نام دیگران
- ❌ فروش یا استفاده‌ی تجاری

انتشار لینک همین صفحه‌ی دانلود مجاز و پیشنهاد می‌شود. برای هر استفاده‌ی دیگر، در تلگرام پیام بده.

## 🙏 قدردانی

- [ViGEmBus](https://github.com/nefarius/ViGEmBus) و [ViGEm.Client](https://github.com/nefarius/ViGEm.NET) از Nefarius، موتور ساخت دسته‌ی مجازی

## 📬 ارتباط

- 💬 کانال تلگرام: [@HSHGamepad](https://t.me/HSHGamepad)
- 🐛 باگ یا پیشنهاد؟ از بخش [Issues](https://github.com/Amirhoshein/hshgamepad/issues) یا تلگرام بگو

---

## 🌍 English summary

**HSH Gamepad** turns your phone into a Windows game controller. A small Windows app creates virtual **Xbox 360** / **DualShock 4** gamepads (up to **9 players**, via [ViGEmBus](https://github.com/nefarius/ViGEmBus)) and serves a touch-controller web page to your phone's browser. No app is needed on the phone. Inputs travel over a WebSocket as tiny text messages for minimal latency.

**Highlights:** fully editable layout (move/resize buttons up to 300%, separate touch area up to 500%, per-button opacity), floating "follow" joystick, X-shaped face-button mode, full theme/skin customization with neon shadows, 60-step undo, copy another player's layout, ping display, haptics, auto fullscreen in landscape, one-phone-per-player-slot locking, smart port-80 handling (falls back to 1111 and helps you free the port), tray icon, auto-removal of idle virtual pads, Windows 7 – 11 (x86/x64).

**Quick start:** download the installer for your Windows version from the [release page](https://github.com/Amirhoshein/hshgamepad/releases/tag/hsh-gamepad-v1.0.2) (files are `.rar`, use 7-Zip) → run HSH Gamepad → open the shown address in your phone's browser (same network) → pick a player number and pad type → play.

> Windows XInput only supports 4 Xbox 360 pads, so players 5–9 are always PS4 pads.
>
> Closed source, all rights reserved. Free for personal use; no modification, reverse engineering, redistribution or commercial use without permission.
