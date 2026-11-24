package com.hsh.gamepadclient;

import android.content.Context;

import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

/** متن‌ها در ۵ زبان: فارسی، انگلیسی، ترکی، روسی، چینی (ترتیب ستون‌ها همین است). */
final class Lang {
    static final String[] CODES = {"fa", "en", "tr", "ru", "zh"};
    static final String[] NAMES = {"فارسی", "English", "Türkçe", "Русский", "中文"};

    private static int idx = 1;
    private static boolean chosen;
    private static final Map<String, String[]> S = new HashMap<String, String[]>();

    static void init(Context c) {
        String code = c.getSharedPreferences("gp", Context.MODE_PRIVATE).getString("lang", null);
        chosen = false;
        if (code != null) {
            for (int i = 0; i < CODES.length; i++) {
                if (CODES[i].equals(code)) {
                    idx = i;
                    chosen = true;
                }
            }
        }
    }

    static boolean chosen() {
        return chosen;
    }

    static void set(Context c, int i) {
        idx = i;
        chosen = true;
        c.getSharedPreferences("gp", Context.MODE_PRIVATE).edit().putString("lang", CODES[i]).apply();
    }

    static int index() {
        return idx;
    }

    static boolean rtl() {
        return idx == 0;
    }

    static String t(String key) {
        String[] v = S.get(key);
        return v == null ? key : v[idx];
    }

    static String f(String key, Object... args) {
        return String.format(Locale.US, t(key), args);
    }

    private static void p(String key, String fa, String en, String tr, String ru, String zh) {
        S.put(key, new String[]{fa, en, tr, ru, zh});
    }

    static {
        p("subtitle", "کنترلر بازی روی گوشی", "Phone gamepad client", "Telefon oyun kumandası",
                "Геймпад на телефоне", "手机游戏手柄");
        p("scanning", "در حال جستجوی شبکه...", "Searching your network...", "Ağ aranıyor...",
                "Поиск в сети...", "正在搜索局域网...");
        p("no_net", "به وای‌فای یا هات‌اسپات وصل نیستی.", "You're not connected to Wi‑Fi or a hotspot.",
                "Wi‑Fi veya hotspot'a bağlı değilsin.", "Нет подключения к Wi‑Fi или точке доступа.",
                "未连接到 Wi‑Fi 或热点。");
        p("none_found", "سروری پیدا نشد. برنامه‌ی کامپیوتر باز باشه و هر دو روی یک شبکه باشن، یا QR رو اسکن کن.",
                "No server found. Make sure the PC app is running on the same network, or scan the QR.",
                "Sunucu bulunamadı. Bilgisayar uygulaması açık ve aynı ağda olmalı, ya da QR'ı tara.",
                "Сервер не найден. Запустите программу на ПК в той же сети или отсканируйте QR.",
                "未找到服务器。请确认电脑端程序已运行且在同一网络，或扫描二维码。");
        p("found", "سرور پیدا شد", "Server found", "Sunucu bulundu", "Сервер найден", "已找到服务器");
        p("connect", "اتصال", "Connect", "Bağlan", "Подключить", "连接");
        p("last_server", "آخرین سرور", "Last server", "Son sunucu", "Последний сервер", "上次的服务器");
        p("scan_qr", "اسکن QR", "Scan QR", "QR tara", "Сканировать QR", "扫描二维码");
        p("manual", "وارد کردن دستی آدرس", "Enter address manually", "Adresi elle gir",
                "Ввести адрес вручную", "手动输入地址");
        p("server_addr", "آدرس سرور", "Server address", "Sunucu adresi", "Адрес сервера", "服务器地址");
        p("cancel", "لغو", "Cancel", "İptal", "Отмена", "取消");
        p("bad_qr", "این QR آدرس سرور نیست", "This QR isn't a server address", "Bu QR bir sunucu adresi değil",
                "Этот QR не содержит адрес сервера", "此二维码不是服务器地址");
        p("bad_addr", "آدرس نامعتبره", "Invalid address", "Geçersiz adres", "Неверный адрес", "地址无效");
        p("port", "پورت %d", "Port %d", "Port %d", "Порт %d", "端口 %d");
        p("qr_hint", "QR نمایش داده شده در برنامه‌ی کامپیوتر رو جلوی دوربین بگیر",
                "Point the camera at the QR shown in the PC app", "Kamerayı bilgisayar uygulamasındaki QR'a tut",
                "Наведите камеру на QR в программе на ПК", "将摄像头对准电脑程序中显示的二维码");
        p("cam_denied", "بدون اجازه‌ی دوربین نمی‌شه QR رو اسکن کرد", "Camera permission is needed to scan QR",
                "QR taramak için kamera izni gerekli", "Для сканирования QR нужен доступ к камере",
                "扫描二维码需要相机权限");
        p("cam_fail", "دوربین باز نشد", "Couldn't open the camera", "Kamera açılamadı",
                "Не удалось открыть камеру", "无法打开相机");
        p("proxy_fail", "خطا در راه‌اندازی اتصال محلی", "Couldn't start the local connection",
                "Yerel bağlantı başlatılamadı", "Не удалось запустить локальное соединение", "无法启动本地连接");
        p("conn_fail", "اتصال به سرور برقرار نشد", "Couldn't connect to the server", "Sunucuya bağlanılamadı",
                "Не удалось подключиться к серверу", "无法连接到服务器");
        p("http_fail", "سرور جواب نداد (کد %d) - آدرس یا کلید اشتباهه؟",
                "Server error (code %d) — wrong address or key?", "Sunucu hatası (kod %d) — adres veya anahtar yanlış mı?",
                "Ошибка сервера (код %d) — неверный адрес или ключ?", "服务器错误（代码 %d）— 地址或密钥有误？");
        p("crash", "مرورگر داخلی از کار افتاد", "The built‑in browser crashed", "Dahili tarayıcı çöktü",
                "Встроенный браузер завершился", "内置浏览器已崩溃");
        p("disconnect_q", "قطع اتصال و برگشت به لیست؟", "Disconnect and go back to the list?",
                "Bağlantıyı kes ve listeye dön?", "Отключиться и вернуться к списку?", "断开连接并返回列表？");
        p("yes", "بله", "Yes", "Evet", "Да", "是");
        p("no", "خیر", "No", "Hayır", "Нет", "否");
        p("press_back_exit", "برای خروج، دوباره دکمه‌ی برگشت را بزنید", "Press back again to exit", "Çıkmak için tekrar geri tuşuna basın",
                "Нажмите «Назад» ещё раз для выхода", "再按一次返回键退出");
        p("help", "راهنما", "Help", "Yardım", "Справка", "帮助");
        p("help_title", "چطور گوشی رو به کامپیوتر وصل کنم؟", "How do I connect my phone to the PC?", "Telefonumu bilgisayara nasıl bağlarım?",
                "Как подключить телефон к компьютеру?", "如何把手机连接到电脑？");
        p("help_s1", "نسخه‌ی کامپیوتر رو با دکمه‌ی پایین (یا لینک گیت‌هاب) دانلود کن، روی ویندوز نصبش کن و بازش کن.",
                "Download the PC version with the button below (or the GitHub link), install it on Windows and open it.",
                "Aşağıdaki düğmeyle (veya GitHub bağlantısıyla) bilgisayar sürümünü indir, Windows'a kur ve aç.",
                "Скачайте версию для ПК кнопкой ниже (или по ссылке GitHub), установите её в Windows и запустите.",
                "点击下方按钮（或 GitHub 链接）下载电脑版，在 Windows 上安装并打开。");
        p("help_s2", "گوشی و کامپیوتر باید روی یک شبکه باشن: هر دو به یک وای‌فای وصل باشن، یا کامپیوتر به هات‌اسپات گوشی وصل شده باشه.",
                "The phone and the PC must be on the same network: both on the same Wi‑Fi, or the PC connected to your phone's hotspot.",
                "Telefon ve bilgisayar aynı ağda olmalı: ikisi de aynı Wi‑Fi'a bağlı olsun ya da bilgisayar telefonun hotspot'una bağlansın.",
                "Телефон и компьютер должны быть в одной сети: оба в одном Wi‑Fi или компьютер подключён к точке доступа телефона.",
                "手机和电脑必须在同一网络：连接同一个 Wi‑Fi，或让电脑连接手机的热点。");
        p("help_s3", "همین برنامه رو باز کن؛ کامپیوتر خودکار تو لیست پیدا میشه. روش بزن تا وصل بشی.",
                "Open this app; your computer shows up in the list automatically. Tap it to connect.",
                "Bu uygulamayı aç; bilgisayarın listede otomatik görünür. Bağlanmak için ona dokun.",
                "Откройте это приложение — компьютер появится в списке автоматически. Нажмите на него для подключения.",
                "打开本应用，电脑会自动出现在列表中，点击即可连接。");
        p("help_s4", "اگه پیدا نشد: «اسکن QR» رو بزن و QR داخل پنجره‌ی برنامه‌ی کامپیوتر رو اسکن کن، یا آدرسی که اونجا نوشته (مثلاً 192.168.1.5) رو دستی وارد کن. اگه کلید مخفی فعاله، آدرس باید کلید رو هم داشته باشه، مثل 192.168.1.5/abcde",
                "If it doesn't show up: tap \"Scan QR\" and scan the QR in the PC app window, or type the address shown there (e.g. 192.168.1.5). If the secret key is on, the address must include it, like 192.168.1.5/abcde",
                "Görünmezse: \"QR tara\"ya dokunup bilgisayar uygulamasındaki QR'ı tara ya da orada yazan adresi (örn. 192.168.1.5) elle gir. Gizli anahtar açıksa adres anahtarı da içermeli, örn. 192.168.1.5/abcde",
                "Если не появился: нажмите «Сканировать QR» и отсканируйте QR в окне программы на ПК, либо введите адрес оттуда (например, 192.168.1.5). Если включён секретный ключ, адрес должен содержать его, например 192.168.1.5/abcde",
                "如果没有出现：点击“扫描二维码”，扫描电脑端窗口里的二维码，或手动输入那里显示的地址（如 192.168.1.5）。若开启了密钥，地址需包含密钥，如 192.168.1.5/abcde");
        p("help_note", "روشن یا خاموش بودن VPN مهم نیست.", "It doesn't matter whether a VPN is on or off.", "VPN'in açık veya kapalı olması fark etmez.",
                "Включён VPN или нет — неважно.", "是否开启 VPN 都没有影响。");
        p("help_dl", "دانلود نسخه‌ی کامپیوتر", "Download PC version", "Bilgisayar sürümünü indir", "Скачать версию для ПК", "下载电脑版");
        p("help_close", "بستن", "Close", "Kapat", "Закрыть", "关闭");
        p("link_copied", "لینک کپی شد", "Link copied", "Bağlantı kopyalandı", "Ссылка скопирована", "链接已复制");
        p("choose_lang", "زبان / Language", "Language", "Dil / Language", "Язык / Language", "语言 / Language");
        p("connecting", "در حال اتصال به کامپیوتر...", "Connecting to the computer...", "Bilgisayara bağlanılıyor...",
                "Подключение к компьютеру...", "正在连接电脑...");
    }
}
