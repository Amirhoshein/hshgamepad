# 🔐 Security Policy

The security of **HSH Gamepad** users matters to us. Thank you for reporting any problem you find responsibly.

---

## 📦 Supported Versions

Only the **latest version** receives security support. Before reporting, please make sure you're using the latest version from [Releases](https://github.com/Amirhoshein/hshgamepad/releases/latest).

| Version | Supported |
|---|---|
| 1.0.6 (latest) | ✅ |
| 1.0.5 and older | ❌ |

---

## 🚨 Reporting a Vulnerability

**Please do not open a public Issue or Discussion for security problems.**

Recommended method:

1. Go to the repo's [**Security**](https://github.com/Amirhoshein/hshgamepad/security) tab.
2. Click **Report a vulnerability**.
3. Fill out the report form.

This report is visible only to me and will not be made public until the problem is fixed.

### What to include

- A description of the problem and its impact
- The app version and your Windows version
- Exact steps to reproduce (or proof-of-concept code/script)
- A suggested fix, if you have one

---

## ⏱️ What Happens After You Report

I maintain this project alone, so timelines are approximate:

- 📬 I'll try to review your report and respond **within a few days**.
- 🔧 If the issue is confirmed, I'll release a fix in the next version.
- 📣 After the fixed version is released, the issue will be recorded in [CHANGELOG.md](CHANGELOG.md) and (if you'd like) I'll credit you as the reporter.

---

## 🛡️ Scope

Items within the scope of a security report:

- Unauthorized access to the controller or the computer through the app's connection
- Execution of arbitrary commands or code on the computer
- Disclosure of information (e.g. the contents of the computer's screen or audio) to unauthorized people
- Unauthorized access to the phone's microphone or the computer's virtual microphone
- Issues with language, layout, or theme `json` files (including files saved on the computer) that could lead to code execution
- Issues with the installer and the Android app (APK)

Items that are usually out of scope:

- Ordinary bugs with no security impact (use [Issues](https://github.com/Amirhoshein/hshgamepad/issues) for these)
- Problems that require full physical access to the computer or phone
- Issues in third-party software (browser, Windows, the ViGEm driver, etc.)

---

## 🧰 Recommendations for Users

- 🌐 Use the app only on a **trusted network** (like your home Wi-Fi), not on public networks.
- 🔽 Download the app only from the [official Releases page](https://github.com/Amirhoshein/hshgamepad/releases) or the [@HSHGamepad](https://t.me/HSHGamepad) channel.
- ✅ The **SHA-256 checksum** of each file is shown next to it in the Assets section; compare it against the file after downloading.
- 📱 Get the Android **APK** only from those same Releases. If Play Protect says the app is unknown, that's because it isn't from the store; but if you got the file from anywhere else, don't install it.
- 🧩 Check any layout, theme, and language JSON files you got from untrusted sources before importing them.
- 🔄 Always use the latest version.

---

🎮 Telegram channel: [@HSHGamepad](https://t.me/HSHGamepad)
