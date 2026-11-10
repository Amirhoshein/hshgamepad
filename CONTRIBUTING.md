# Contributing to HSH Gamepad

Thanks for wanting to help! / ممنون که می‌خوای کمک کنی!

## Reporting bugs / گزارش باگ
Open an [issue](https://github.com/Amirhoshein/hshgamepad/issues/new/choose) and include:
- Windows version (7 / 8.1 / 10 / 11, x86 or x64) and app version
- Phone model and browser
- Controller type (PS4 / Xbox 360), connection type (Wi-Fi / USB)
- Steps to reproduce, and screenshots or a short video if possible

## Suggesting features / پیشنهاد امکانات جدید
Open an issue using the *Feature request* template and describe the problem you want solved.

## Pull requests
1. Fork the repo and create a branch from `main`
2. Keep changes focused: one feature or fix per PR
3. Build and test with a real phone before opening the PR (see "Build from source" in the README)
4. If you change the UI text, update both the English and the Persian README sections
5. Describe what you changed and why in the PR

## Code notes
- C# on .NET Framework 4.8 (WinForms); the phone UI is a single `index.html` with no external libraries
- The phone UI must not make requests to the internet

## Security issues
Please don't open a public issue. See [SECURITY.md](SECURITY.md).
