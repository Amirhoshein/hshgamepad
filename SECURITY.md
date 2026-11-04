# Security Policy · سیاست امنیتی

[**فارسی**](#fa) · [**English**](#en)

---

<a id="fa"></a>

<div dir="rtl">

# 🇮🇷 فارسی

## نسخه‌های پشتیبانی‌شده

| نسخه | پشتیبانی |
|---|---|
| 1.0.4 (آخرین نسخه) | ✅ |
| قدیمی‌تر از 1.0.4 | ❌ لطفاً به آخرین نسخه آپدیت کن |

## گزارش مشکل امنیتی

اگه به یه آسیب‌پذیری رسیدی، لطفاً **تو Issues عمومی گزارشش نکن**. به یکی از این دو روش خصوصی بفرست:

1. **GitHub:** تب [Security](https://github.com/Amirhoshein/hshgamepad/security) ← **Report a vulnerability**
2. **تلگرام:** پیام خصوصی به [@amir_hoshein](https://t.me/amir_hoshein)

تو گزارشت بنویس:
- مشکل چیه و چه تاثیری داره
- مراحل بازتولید (نسخه‌ی برنامه، نسخه‌ی ویندوز، تنظیمات)
- اگه داری: پیشنهاد برای رفع

این یه پروژه‌ی شخصیه؛ سعی می‌کنم **تو چند روز** جواب بدم و بعد از رفع مشکل، توی آپدیت از تو (اگه بخوای) تشکر کنم. لطفاً تا آماده شدن آپدیت، جزئیات رو جایی منتشر نکن.

## مدل امنیتی برنامه (بدون پیچوندن)

برنامه برای **بازی تو شبکه‌ی خانگی** ساخته شده، نه شبکه‌ی عمومی. چیزایی که باید بدونی:

- 🌐 **بدون رمز و بدون رمزنگاری:** ارتباط گوشی و کامپیوتر از طریق HTTP و WebSocket ساده انجام میشه (نه HTTPS). هر کسی که تو **همون شبکه** باشه و آدرس رو بدونه می‌تونه وصل بشه و یه پلیر رو اشغال کنه. ترافیک هم تو همون شبکه قابل شنود هست.
- 🖱️ **ماوس و کیبورد از راه دور:** وقتی گزینه‌ی «دسترسی ماوس و کیبورد از گوشی» روشنه، کسی که به پلیر ۱ وصل بشه می‌تونه ماوس و کیبورد کامپیوتر رو کنترل کنه. این پرریسک‌ترین بخش برنامه‌ست.
- 👑 **اجرا با دسترسی ادمین:** برنامه برای اینکه ورودی به برنامه‌های ادمین هم برسه، با دسترسی ادمین اجرا میشه. پس یه آسیب‌پذیری تو برنامه می‌تونه اثر بیشتری داشته باشه.
- 🚫 **بدون ارتباط با اینترنت:** صفحه‌ی گوشی از خود برنامه سرو میشه و هیچ درخواستی به سرور بیرونی نمیره؛ هیچ آنالیتیکس، تلمتری یا ارسال داده‌ای وجود نداره. تنظیمات برنامه فقط تو `%AppData%\GamepadServer\settings.txt` و تنظیمات ظاهری تو مرورگر گوشی خودت ذخیره میشه.
- 🔒 باز کردن آدرس روی خود کامپیوتر (localhost) عمداً بلاک شده.

## توصیه‌ها برای استفاده‌ی امن

- فقط تو **شبکه‌ی خونگی و مورد اعتماد** استفاده کن؛ نه وای‌فای عمومی، کافه، دانشگاه، هتل یا شبکه‌ی مهمان
- اگه ماوس و کیبورد از گوشی رو لازم نداری، **خاموشش کن** (تیک «دسترسی ماوس و کیبورد از گوشی»)
- وقتی ازش استفاده نمی‌کنی **از برنامه خارج شو** (راست‌کلیک آیکون تسک‌بار ← خروج از برنامه)
- فایروال ویندوز رو فقط روی **Private networks** اجازه بده، نه Public
- آدرس و QR Code رو با غریبه‌ها به اشتراک نذار
- نصب‌کننده رو فقط از **[Releases رسمی](https://github.com/Amirhoshein/hshgamepad/releases)** دانلود کن

## خارج از محدوده

- حمله‌ای که نیاز به دسترسی فیزیکی یا ادمین از قبل روی کامپیوتر داره
- نبودن رمزنگاری و رمز عبور (طراحی فعلیه و بالا توضیح داده شد؛ ولی پیشنهاد برای بهبودش، مثلاً کد جفت‌سازی، خوش‌آمده)
- آسیب‌پذیری‌های خود ViGEmBus یا ویندوز (به پروژه‌ی اصلی‌شون گزارش بده)

</div>

---

<a id="en"></a>

# 🇬🇧 English

## Supported versions

| Version | Supported |
|---|---|
| 1.0.4 (latest) | ✅ |
| Older than 1.0.4 | ❌ please update to the latest release |

## Reporting a vulnerability

If you found a vulnerability, please **do not open a public issue**. Report it privately through either:

1. **GitHub:** the [Security](https://github.com/Amirhoshein/hshgamepad/security) tab → **Report a vulnerability**
2. **Telegram:** private message to [@amir_hoshein](https://t.me/amir_hoshein)

Please include:
- What the problem is and its impact
- Steps to reproduce (app version, Windows version, settings)
- A suggested fix, if you have one

This is a personal project; I'll try to reply **within a few days** and, once fixed, credit you in the release notes if you wish. Please don't publish details until a fixed version is available.

## Security model (plainly)

The app is designed for **gaming on a home network**, not for public networks. You should know:

- 🌐 **No password, no encryption:** the phone ↔ PC link uses plain HTTP and WebSocket (not HTTPS). Anyone on the **same network** who knows the address can connect and take a player slot. Traffic can be sniffed on that network.
- 🖱️ **Remote mouse & keyboard:** while "mouse & keyboard access from phone" is on, whoever connects as Player 1 can control the PC's mouse and keyboard. This is the highest-risk feature.
- 👑 **Runs as administrator:** so that input can reach admin apps too. A vulnerability in the app can therefore have a bigger impact.
- 🚫 **No internet communication:** the phone page is served by the app itself and makes no requests to outside servers; there is no analytics, telemetry or data upload. App settings are stored only in `%AppData%\GamepadServer\settings.txt`, and look/layout settings stay in your phone's browser.
- 🔒 Opening the address on the PC itself (localhost) is blocked on purpose.

## Safe-use recommendations

- Use it only on a **trusted home network** — not public Wi-Fi, cafés, universities, hotels or guest networks
- If you don't need mouse & keyboard from the phone, **turn it off** (the "mouse & keyboard access from phone" checkbox)
- **Exit the app** when you're not using it (taskbar icon right-click → Exit)
- Allow the Windows Firewall on **Private networks** only, not Public
- Don't share the address or QR code with strangers
- Download the installer only from the **[official Releases](https://github.com/Amirhoshein/hshgamepad/releases)**

## Out of scope

- Attacks that require prior physical or administrator access to the PC
- The lack of encryption/passwords (this is the current design, explained above — but suggestions such as a pairing code are welcome)
- Vulnerabilities in ViGEmBus or Windows themselves (report those to their own projects)
