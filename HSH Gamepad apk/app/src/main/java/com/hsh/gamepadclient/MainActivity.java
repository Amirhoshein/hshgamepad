package com.hsh.gamepadclient;

import android.animation.ObjectAnimator;
import android.net.Uri;
import android.content.Context;
import android.content.ClipboardManager;
import android.content.ClipData;
import android.content.ActivityNotFoundException;
import android.view.inputmethod.EditorInfo;
import android.view.WindowManager;
import android.view.Window;
import android.view.KeyEvent;
import android.graphics.drawable.ColorDrawable;
import android.app.Dialog;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.animation.DecelerateInterpolator;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import java.net.URI;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Set;

/** صفحه‌ی اصلی: انتخاب زبان (بار اول)، اسکن شبکه، لیست سرورها، اسکن QR. */
public class MainActivity extends Activity {
    private static final String PREFS = "gp";
    private static final int REQ_QR = 7;
    private static final String PC_URL = "https://github.com/Amirhoshein/hshgamepad/releases/latest";

    private static final int ACCENT = Color.parseColor("#4C8DFF");
    private static final int CARD = Color.parseColor("#1B2133");
    private static final int STROKE = Color.parseColor("#2A3350");
    private static final int MUTED = Color.parseColor("#8D97B3");
    private static final int GREEN = Color.parseColor("#3DDC97");
    private static final int AMBER = Color.parseColor("#FFB547");

    private final Handler ui = new Handler(Looper.getMainLooper());
    private final Map<String, Integer> found = new LinkedHashMap<String, Integer>();   // "ip:port" -> port
    private final Map<String, String> hosts = new LinkedHashMap<String, String>();
    private final Set<String> seenThisScan = new HashSet<String>();
    private final Set<String> shown = new HashSet<String>();   // کارت‌هایی که قبلاً با انیمیشن ظاهر شدن

    private LinearLayout listBox;
    private TextView status;
    private View dot;
    private ObjectAnimator pulse;
    private int state;          // 0 در حال اسکن، 1 بدون شبکه، 2 پیدا نشد، 3 پیدا شد
    private LanScanner scanner;
    private boolean resumed;
    private boolean pickerOpen;

    private final Runnable rescan = new Runnable() {
        @Override
        public void run() {
            startScan();
        }
    };

    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        Lang.init(this);
        if (Lang.chosen()) buildUi();
        else showPicker();
    }

    @Override
    protected void onResume() {
        super.onResume();
        resumed = true;
        if (pickerOpen) return;
        refreshList();
        startScan();
    }

    @Override
    protected void onPause() {
        super.onPause();
        resumed = false;      // برنامه «می‌خوابه»: اسکن متوقف میشه
        ui.removeCallbacks(rescan);
        if (pulse != null) pulse.cancel();
        if (scanner != null) {
            scanner.cancel();
            scanner = null;
        }
    }

    @Override
    @SuppressWarnings("deprecation")
    public void onBackPressed() {
        if (pickerOpen && Lang.chosen()) {
            pickerOpen = false;
            buildUi();
            startScan();
            return;
        }
        super.onBackPressed();
    }

    // ---------- اسکن ----------

    private void startScan() {
        if (!resumed || pickerOpen || !Lang.chosen()) return;
        if (scanner != null) scanner.cancel();
        seenThisScan.clear();
        final LanScanner s = new LanScanner();
        scanner = s;
        state = 0;
        renderStatus();
        s.start(new LanScanner.Listener() {
            @Override
            public void onFound(final String host, final int port) {
                ui.post(new Runnable() {
                    @Override
                    public void run() {
                        if (s != scanner) return;
                        String key = host + ":" + port;
                        seenThisScan.add(key);
                        if (!found.containsKey(key)) {
                            found.put(key, port);
                            hosts.put(key, host);
                            refreshList();
                        }
                        state = 3;
                        renderStatus();
                    }
                });
            }

            @Override
            public void onDone(final boolean noNetwork) {
                ui.post(new Runnable() {
                    @Override
                    public void run() {
                        if (s != scanner) return;
                        found.keySet().retainAll(seenThisScan);
                        hosts.keySet().retainAll(seenThisScan);
                        refreshList();
                        state = noNetwork ? 1 : (found.isEmpty() ? 2 : 3);
                        renderStatus();
                        ui.postDelayed(rescan, 5000);
                    }
                });
            }
        });
    }

    private void renderStatus() {
        if (status == null || dot == null) return;
        String[] keys = {"scanning", "no_net", "none_found", "found"};
        status.setText(Lang.t(keys[state]));
        int color = state == 0 ? ACCENT : state == 3 ? GREEN : AMBER;
        GradientDrawable g = new GradientDrawable();
        g.setShape(GradientDrawable.OVAL);
        g.setColor(color);
        dot.setBackground(g);
        if (state == 0) {
            if (pulse != null && !pulse.isRunning()) pulse.start();
        } else {
            if (pulse != null) pulse.cancel();
            dot.setAlpha(1f);
        }
    }

    // ---------- رابط کاربری (بدون XML) ----------

    private void buildUi() {
        pickerOpen = false;
        int pad = dp(20);
        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setBackground(backdrop());
        page.setPadding(pad, dp(12), pad, pad);
        page.setLayoutDirection(Lang.rtl() ? View.LAYOUT_DIRECTION_RTL : View.LAYOUT_DIRECTION_LTR);

        // نوار بالا: دکمه‌ی زبان
        LinearLayout top = new LinearLayout(this);
        top.setGravity(Gravity.CENTER_VERTICAL);
        LinearLayout helpChip = new LinearLayout(this);
        helpChip.setGravity(Gravity.CENTER_VERTICAL);
        helpChip.setPadding(dp(14), dp(8), dp(14), dp(8));
        helpChip.setBackground(ripple(rounded(CARD, 22, STROKE)));
        helpChip.addView(text("?", 14, ACCENT, true));
        TextView helpTv = text(Lang.t("help"), 13, MUTED, true);
        LinearLayout.LayoutParams hl = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        hl.setMarginStart(dp(8));
        helpChip.addView(helpTv, hl);
        helpChip.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showHelp();
            }
        });
        top.addView(helpChip);
        top.addView(new View(this), new LinearLayout.LayoutParams(0, 1, 1f));
        LinearLayout chip = new LinearLayout(this);
        chip.setGravity(Gravity.CENTER_VERTICAL);
        chip.setPadding(dp(12), dp(8), dp(14), dp(8));
        chip.setBackground(ripple(rounded(CARD, 22, STROKE)));
        chip.addView(icon(R.drawable.ic_globe, 18, MUTED));
        TextView code = text(Lang.CODES[Lang.index()].toUpperCase(), 13, MUTED, true);
        LinearLayout.LayoutParams cl = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        cl.setMarginStart(dp(6));
        chip.addView(code, cl);
        chip.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (scanner != null) {
                    scanner.cancel();
                    scanner = null;
                }
                ui.removeCallbacks(rescan);
                showPicker();
            }
        });
        top.addView(chip);
        page.addView(top, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));

        // سربرگ: لوگو + اسم
        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.icon);
        LinearLayout.LayoutParams ll = new LinearLayout.LayoutParams(dp(84), dp(84));
        ll.gravity = Gravity.CENTER_HORIZONTAL;
        ll.topMargin = dp(4);
        page.addView(logo, ll);

        TextView title = text("HSH Gamepad", 26, Color.WHITE, true);
        title.setGravity(Gravity.CENTER);
        title.setPadding(0, dp(10), 0, 0);
        page.addView(title);

        TextView sub = text(Lang.t("subtitle"), 14, MUTED, false);
        sub.setGravity(Gravity.CENTER);
        page.addView(sub);

        // وضعیت: نقطه‌ی چشمک‌زن + متن
        LinearLayout st = new LinearLayout(this);
        st.setGravity(Gravity.CENTER);
        st.setPadding(0, dp(18), 0, dp(10));
        dot = new View(this);
        LinearLayout.LayoutParams dl = new LinearLayout.LayoutParams(dp(10), dp(10));
        dl.setMarginEnd(dp(8));
        st.addView(dot, dl);
        status = text("", 14, MUTED, false);
        status.setGravity(Gravity.CENTER);
        st.addView(status, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f));
        page.addView(st, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));

        pulse = ObjectAnimator.ofFloat(dot, "alpha", 1f, 0.2f);
        pulse.setDuration(700);
        pulse.setRepeatCount(ObjectAnimator.INFINITE);
        pulse.setRepeatMode(ObjectAnimator.REVERSE);

        // لیست سرورها
        ScrollView sv = new ScrollView(this);
        sv.setVerticalScrollBarEnabled(false);
        listBox = new LinearLayout(this);
        listBox.setOrientation(LinearLayout.VERTICAL);
        sv.addView(listBox);
        page.addView(sv, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f));

        // دکمه‌های پایین
        View qr = actionButton(R.drawable.ic_qr, Lang.t("scan_qr"), true);
        qr.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                startActivityForResult(new Intent(MainActivity.this, QrActivity.class), REQ_QR);
            }
        });
        page.addView(qr, rowParams(54, 12));

        View manual = actionButton(R.drawable.ic_keyboard, Lang.t("manual"), false);
        manual.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                askAddress();
            }
        });
        page.addView(manual, rowParams(54, 10));

        setContentView(page);
        renderStatus();
        refreshList();
    }

    private void refreshList() {
        if (listBox == null) return;
        listBox.removeAllViews();

        SharedPreferences p = getSharedPreferences(PREFS, MODE_PRIVATE);
        final String lastHost = p.getString("host", null);
        if (lastHost != null) {
            final int lastPort = p.getInt("port", 80);
            final String lastPath = p.getString("path", "");
            View c = serverCard(lastHost, lastPort, true);
            c.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    startGame(lastHost, lastPort, lastPath);
                }
            });
            listBox.addView(c, rowParams(-2, 10));
            appear(c, "last");
        }

        for (final Map.Entry<String, Integer> e : found.entrySet()) {
            final String host = hosts.get(e.getKey());
            final int port = e.getValue();
            View c = serverCard(host, port, false);
            c.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    startGame(host, port, "");
                }
            });
            listBox.addView(c, rowParams(-2, 10));
            appear(c, e.getKey());
        }
        shown.retainAll(found.keySet());
        shown.add("last");
    }

    /** کارت سرور: آیکون مانیتور + آدرس + دکمه‌ی اتصال */
    private View serverCard(String host, int port, boolean last) {
        LinearLayout row = new LinearLayout(this);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setPadding(dp(14), dp(14), dp(14), dp(14));
        row.setBackground(ripple(rounded(CARD, 18, last ? STROKE : Color.parseColor("#35508A"))));

        FrameLayout circle = new FrameLayout(this);
        GradientDrawable cg = new GradientDrawable();
        cg.setShape(GradientDrawable.OVAL);
        cg.setColor(last ? Color.parseColor("#262E45") : Color.parseColor("#22345E"));
        circle.setBackground(cg);
        ImageView mon = icon(last ? R.drawable.ic_history : R.drawable.ic_monitor, 24, last ? MUTED : ACCENT);
        circle.addView(mon, new FrameLayout.LayoutParams(dp(24), dp(24), Gravity.CENTER));
        row.addView(circle, new LinearLayout.LayoutParams(dp(48), dp(48)));

        LinearLayout col = new LinearLayout(this);
        col.setOrientation(LinearLayout.VERTICAL);
        TextView t1 = text(host, 17, Color.WHITE, true);
        t1.setSingleLine(true);
        col.addView(t1);
        String portText = port == 80 ? "" : Lang.f("port", port);
        String subText = last ? Lang.t("last_server") : "HSH Gamepad";
        if (portText.length() > 0) subText += "  •  " + portText;
        col.addView(text(subText, 12, MUTED, false));
        LinearLayout.LayoutParams cp = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f);
        cp.setMarginStart(dp(12));
        cp.setMarginEnd(dp(8));
        row.addView(col, cp);

        TextView pill = text(Lang.t("connect"), 14, Color.WHITE, true);
        pill.setGravity(Gravity.CENTER);
        pill.setPadding(dp(16), dp(9), dp(16), dp(9));
        pill.setBackground(rounded(last ? Color.parseColor("#3A4466") : ACCENT, 20, 0));
        row.addView(pill);
        return row;
    }

    private View actionButton(int iconRes, String label, boolean primary) {
        LinearLayout b = new LinearLayout(this);
        b.setGravity(Gravity.CENTER);
        b.setBackground(ripple(primary ? rounded(ACCENT, 16, 0) : rounded(CARD, 16, STROKE)));
        b.addView(icon(iconRes, 22, Color.WHITE));
        TextView t = text(label, 16, Color.WHITE, true);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        lp.setMarginStart(dp(10));
        b.addView(t, lp);
        return b;
    }

    /** کارت تازه از بالا میاد پایین و محو‌به‌روشن میشه؛ کارت قبلی بدون انیمیشن می‌مونه */
    private void appear(View v, String key) {
        if (shown.contains(key)) return;
        shown.add(key);
        animateIn(v, 0);
    }

    private void animateIn(View v, long delay) {
        v.setAlpha(0f);
        v.setTranslationY(-dp(28));
        v.animate().alpha(1f).translationY(0f).setStartDelay(delay)
                .setDuration(380).setInterpolator(new DecelerateInterpolator()).start();
    }

    // ---------- انتخاب زبان ----------

    private void showPicker() {
        pickerOpen = true;
        if (pulse != null) pulse.cancel();
        LinearLayout p = new LinearLayout(this);
        p.setOrientation(LinearLayout.VERTICAL);
        p.setGravity(Gravity.CENTER);
        p.setBackground(backdrop());
        p.setPadding(dp(24), dp(24), dp(24), dp(24));
        p.setLayoutDirection(View.LAYOUT_DIRECTION_LTR);

        ImageView globe = icon(R.drawable.ic_globe, 44, ACCENT);
        LinearLayout.LayoutParams gl = new LinearLayout.LayoutParams(dp(44), dp(44));
        gl.gravity = Gravity.CENTER_HORIZONTAL;
        p.addView(globe, gl);

        TextView t = text("زبان / Language", 22, Color.WHITE, true);
        t.setGravity(Gravity.CENTER);
        t.setPadding(0, dp(12), 0, dp(20));
        p.addView(t);

        for (int i = 0; i < Lang.NAMES.length; i++) {
            final int idx = i;
            boolean sel = Lang.chosen() && Lang.index() == i;
            TextView c = text(Lang.NAMES[i], 18, Color.WHITE, true);
            c.setGravity(Gravity.CENTER);
            c.setBackground(ripple(rounded(sel ? Color.parseColor("#22345E") : CARD, 16, sel ? ACCENT : STROKE)));
            c.setOnClickListener(new View.OnClickListener() {
                @Override
                public void onClick(View v) {
                    Lang.set(MainActivity.this, idx);
                    buildUi();
                    startScan();
                }
            });
            p.addView(c, rowParams(56, 10));
            animateIn(c, 70L * i);
        }
        setContentView(p);
    }

    // ---------- QR و آدرس دستی ----------

    @Override
    protected void onActivityResult(int req, int res, Intent data) {
        super.onActivityResult(req, res, data);
        if (req == REQ_QR && res == RESULT_OK && data != null) {
            if (!connectToText(data.getStringExtra("text")))
                Toast.makeText(this, Lang.t("bad_qr"), Toast.LENGTH_LONG).show();
        }
    }

    /** پنجره‌ی ورود دستی آدرس: ظاهر خودِ برنامه (کارت تیره‌ی گوشه‌گرد، ورودی و دکمه‌های هم‌سبک با صفحه‌ی اصلی) */
    private void askAddress() {
        final Dialog dlg = new Dialog(this);
        dlg.requestWindowFeature(Window.FEATURE_NO_TITLE);

        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setBackground(rounded(CARD, 22, STROKE));
        card.setPadding(dp(20), dp(20), dp(20), dp(18));
        card.setLayoutDirection(Lang.rtl() ? View.LAYOUT_DIRECTION_RTL : View.LAYOUT_DIRECTION_LTR);

        LinearLayout head = new LinearLayout(this);
        head.setOrientation(LinearLayout.HORIZONTAL);
        head.setGravity(Gravity.CENTER_VERTICAL);
        head.addView(icon(R.drawable.ic_keyboard, 24, ACCENT));
        TextView title = text(Lang.t("server_addr"), 18, Color.WHITE, true);
        LinearLayout.LayoutParams tl = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        tl.setMarginStart(dp(10));
        head.addView(title, tl);
        card.addView(head);

        final EditText et = new EditText(this);
        et.setHint("192.168.1.5   |   http://192.168.1.5:1111/key");
        et.setHintTextColor(Color.parseColor("#5A6482"));
        et.setTextColor(Color.WHITE);
        et.setTextSize(16);
        et.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_URI | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
        et.setSingleLine(true);
        et.setImeOptions(EditorInfo.IME_ACTION_GO);
        et.setLayoutDirection(View.LAYOUT_DIRECTION_LTR);       // آدرس همیشه چپ‌به‌راست
        et.setTextDirection(View.TEXT_DIRECTION_LTR);
        et.setGravity(Gravity.START | Gravity.CENTER_VERTICAL);
        et.setPadding(dp(14), dp(12), dp(14), dp(12));
        final GradientDrawable idle = rounded(Color.parseColor("#12172A"), 14, STROKE);
        final GradientDrawable focus = rounded(Color.parseColor("#12172A"), 14, ACCENT);
        et.setBackground(idle);
        et.setOnFocusChangeListener(new View.OnFocusChangeListener() {
            @Override
            public void onFocusChange(View v, boolean has) {
                v.setBackground(has ? focus : idle);
            }
        });
        LinearLayout.LayoutParams el = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(50));
        el.topMargin = dp(16);
        card.addView(et, el);

        final Runnable go = new Runnable() {
            @Override
            public void run() {
                if (connectToText(et.getText().toString())) dlg.dismiss();
                else Toast.makeText(MainActivity.this, Lang.t("bad_addr"), Toast.LENGTH_LONG).show();
            }
        };
        et.setOnEditorActionListener(new TextView.OnEditorActionListener() {
            @Override
            public boolean onEditorAction(TextView v, int id, KeyEvent ev) {
                go.run();
                return true;
            }
        });

        LinearLayout btns = new LinearLayout(this);
        btns.setOrientation(LinearLayout.HORIZONTAL);
        TextView cancel = text(Lang.t("cancel"), 16, Color.WHITE, true);
        cancel.setGravity(Gravity.CENTER);
        cancel.setBackground(ripple(rounded(Color.parseColor("#12172A"), 14, STROKE)));
        cancel.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                dlg.dismiss();
            }
        });
        TextView ok = text(Lang.t("connect"), 16, Color.WHITE, true);
        ok.setGravity(Gravity.CENTER);
        ok.setBackground(ripple(rounded(ACCENT, 14, 0)));
        ok.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                go.run();
            }
        });
        LinearLayout.LayoutParams cl = new LinearLayout.LayoutParams(0, dp(48), 1f);
        LinearLayout.LayoutParams ol = new LinearLayout.LayoutParams(0, dp(48), 1f);
        ol.setMarginStart(dp(10));
        btns.addView(cancel, cl);
        btns.addView(ok, ol);
        LinearLayout.LayoutParams bl = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        bl.topMargin = dp(16);
        card.addView(btns, bl);

        dlg.setContentView(card);
        Window w = dlg.getWindow();
        if (w != null) {
            w.setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            w.setLayout(Math.min(getResources().getDisplayMetrics().widthPixels - dp(40), dp(420)), ViewGroup.LayoutParams.WRAP_CONTENT);
            w.setDimAmount(0.6f);
            w.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE | WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);
        }
        dlg.show();
        et.requestFocus();
    }

    private LinearLayout stepRow(int n, String txt) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        TextView badge = text(String.valueOf(n), 13, Color.WHITE, true);
        badge.setGravity(Gravity.CENTER);
        GradientDrawable oval = new GradientDrawable();
        oval.setShape(GradientDrawable.OVAL);
        oval.setColor(ACCENT);
        badge.setBackground(oval);
        row.addView(badge, new LinearLayout.LayoutParams(dp(26), dp(26)));
        TextView t = text(txt, 14, Color.parseColor("#D5DCF0"), false);
        t.setLineSpacing(0, 1.25f);
        LinearLayout.LayoutParams tl = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f);
        tl.setMarginStart(dp(12));
        row.addView(t, tl);
        return row;
    }

    private void copyPcLink() {
        try {
            ClipboardManager cm = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
            if (cm != null) cm.setPrimaryClip(ClipData.newPlainText("HSH Gamepad", PC_URL));
            Toast.makeText(this, Lang.t("link_copied"), Toast.LENGTH_SHORT).show();
        } catch (Exception ignored) {
        }
    }

    /** راهنمای اتصال گوشی به کامپیوتر + لینک دانلود نسخه‌ی ویندوز */
    private void showHelp() {
        final Dialog dlg = new Dialog(this);
        dlg.requestWindowFeature(Window.FEATURE_NO_TITLE);

        LinearLayout card = new LinearLayout(this);
        card.setOrientation(LinearLayout.VERTICAL);
        card.setBackground(rounded(CARD, 22, STROKE));
        card.setPadding(dp(20), dp(20), dp(20), dp(18));
        card.setLayoutDirection(Lang.rtl() ? View.LAYOUT_DIRECTION_RTL : View.LAYOUT_DIRECTION_LTR);

        TextView title = text(Lang.t("help_title"), 18, Color.WHITE, true);
        card.addView(title);

        String[] keys = {"help_s1", "help_s2", "help_s3", "help_s4"};
        for (int i = 0; i < keys.length; i++) {
            card.addView(stepRow(i + 1, Lang.t(keys[i])), rowParams(-2, i == 0 ? 16 : 14));
        }

        TextView note = text(Lang.t("help_note"), 12, MUTED, false);
        LinearLayout.LayoutParams nl = rowParams(-2, 14);
        card.addView(note, nl);

        View dl = actionButton(R.drawable.ic_monitor, Lang.t("help_dl"), true);
        dl.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                try {
                    startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(PC_URL)));
                } catch (ActivityNotFoundException e) {
                    copyPcLink();
                }
            }
        });
        card.addView(dl, rowParams(52, 18));

        TextView link = text(PC_URL, 11, MUTED, false);
        link.setGravity(Gravity.CENTER);
        link.setLayoutDirection(View.LAYOUT_DIRECTION_LTR);
        link.setTextDirection(View.TEXT_DIRECTION_LTR);
        link.setPadding(0, dp(8), 0, dp(8));
        link.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                copyPcLink();
            }
        });
        card.addView(link, rowParams(-2, 4));

        TextView close = text(Lang.t("help_close"), 16, Color.WHITE, true);
        close.setGravity(Gravity.CENTER);
        close.setBackground(ripple(rounded(Color.parseColor("#12172A"), 14, STROKE)));
        close.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                dlg.dismiss();
            }
        });
        card.addView(close, rowParams(48, 6));

        ScrollView sv = new ScrollView(this);
        sv.setVerticalScrollBarEnabled(false);
        sv.addView(card);
        dlg.setContentView(sv);
        Window w = dlg.getWindow();
        if (w != null) {
            w.setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            w.setLayout(Math.min(getResources().getDisplayMetrics().widthPixels - dp(32), dp(440)), ViewGroup.LayoutParams.WRAP_CONTENT);
            w.setDimAmount(0.6f);
        }
        dlg.show();
    }

    private boolean connectToText(String raw) {
        if (raw == null) return false;
        String t = raw.trim();
        if (t.length() == 0) return false;
        if (!t.contains("://")) t = "http://" + t;
        try {
            URI u = new URI(t);
            String host = u.getHost();
            if (host == null) return false;
            int port = u.getPort();
            if (port <= 0) port = "https".equalsIgnoreCase(u.getScheme()) ? 443 : 80;
            String path = u.getRawPath() == null ? "" : u.getRawPath();
            if (u.getRawQuery() != null) path += "?" + u.getRawQuery();
            if (path.equals("/")) path = "";
            startGame(host, port, path);
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    private void startGame(String host, int port, String path) {
        getSharedPreferences(PREFS, MODE_PRIVATE).edit()
                .putString("host", host).putInt("port", port).putString("path", path == null ? "" : path)
                .apply();
        Intent i = new Intent(this, GameActivity.class);
        i.putExtra("host", host);
        i.putExtra("port", port);
        i.putExtra("path", path == null ? "" : path);
        startActivity(i);
    }

    // ---------- ابزارهای ظاهر ----------

    private Drawable backdrop() {
        GradientDrawable g = new GradientDrawable(GradientDrawable.Orientation.TOP_BOTTOM,
                new int[]{Color.parseColor("#0D111C"), Color.parseColor("#182041")});
        return g;
    }

    private GradientDrawable rounded(int color, int radiusDp, int strokeColor) {
        GradientDrawable g = new GradientDrawable();
        g.setColor(color);
        g.setCornerRadius(dp(radiusDp));
        if (strokeColor != 0) g.setStroke(dp(1), strokeColor);
        return g;
    }

    private Drawable ripple(Drawable bg) {
        return new RippleDrawable(ColorStateList.valueOf(0x33FFFFFF), bg, null);
    }

    private ImageView icon(int res, int sizeDp, int tint) {
        ImageView v = new ImageView(this);
        v.setImageResource(res);
        v.setColorFilter(tint);
        v.setLayoutParams(new LinearLayout.LayoutParams(dp(sizeDp), dp(sizeDp)));
        return v;
    }

    private TextView text(String s, int sp, int color, boolean bold) {
        TextView t = new TextView(this);
        t.setText(s);
        t.setTextSize(sp);
        t.setTextColor(color);
        if (bold) t.setTypeface(Typeface.DEFAULT_BOLD);
        return t;
    }

    /** h = ارتفاع به dp (-2 یعنی WRAP_CONTENT) */
    private LinearLayout.LayoutParams rowParams(int h, int topMargin) {
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT, h == -2 ? ViewGroup.LayoutParams.WRAP_CONTENT : dp(h));
        lp.topMargin = dp(topMargin);
        return lp;
    }

    private int dp(int v) {
        return Math.round(v * getResources().getDisplayMetrics().density);
    }
}
