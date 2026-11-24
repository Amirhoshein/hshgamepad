package com.hsh.gamepadclient;

import android.Manifest;
import android.app.Activity;
import android.content.pm.PackageManager;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.webkit.ValueCallback;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.webkit.GeolocationPermissions;
import android.webkit.PermissionRequest;
import android.webkit.RenderProcessGoneDetail;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.Toast;

import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

/** صفحه‌ی دسته رو تمام‌صفحه و با دسترسی‌های کامل (میکروفون، سنسورها، ...) نشون میده. */
public class GameActivity extends Activity {
    private static final int REQ_MEDIA = 11, REQ_GEO = 12;

    private FrameLayout root;
    private WebView web;
    private LocalProxy proxy;
    private PermissionRequest pendingMedia;
    private String pendingGeoOrigin;
    private GeolocationPermissions.Callback pendingGeoCb;
    private boolean failed;

    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        Lang.init(this);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        if (Build.VERSION.SDK_INT >= 28) {
            WindowManager.LayoutParams lp = getWindow().getAttributes();
            lp.layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES;
            getWindow().setAttributes(lp);
        }

        String host = getIntent().getStringExtra("host");
        int port = getIntent().getIntExtra("port", 80);
        String path = getIntent().getStringExtra("path");
        if (host == null) {
            finish();
            return;
        }
        if (path == null || path.length() == 0) path = "/";
        if (!path.startsWith("/")) path = "/" + path;

        proxy = new LocalProxy(host, port);
        int localPort;
        try {
            localPort = proxy.start();
        } catch (IOException e) {
            Toast.makeText(this, Lang.t("proxy_fail"), Toast.LENGTH_LONG).show();
            finish();
            return;
        }

        root = new FrameLayout(this);
        root.setBackgroundColor(Color.parseColor("#12151D"));
        web = new WebView(this);
        web.setBackgroundColor(Color.parseColor("#12151D"));
        root.addView(web, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        buildLoader();
        setContentView(root);

        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setMediaPlaybackRequiresUserGesture(false);
        s.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        s.setAllowFileAccess(false);
        s.setGeolocationEnabled(true);
        s.setSupportZoom(false);
        s.setBuiltInZoomControls(false);
        s.setUseWideViewPort(true);
        s.setCacheMode(WebSettings.LOAD_NO_CACHE);   // همیشه آخرین نسخه‌ی صفحه از سرور
        web.setOverScrollMode(View.OVER_SCROLL_NEVER);
        web.setLongClickable(false);
        web.setHapticFeedbackEnabled(false);
        web.setOnLongClickListener(new View.OnLongClickListener() {
            @Override
            public boolean onLongClick(View v) {
                return true;
            }
        });

        web.setWebViewClient(new WebViewClient() {
            @Override
            public void onPageFinished(WebView v, String url) {
                advance(20f);          // HTML دانلود شد => ۲۰٪؛ بقیه برای نمایش صفحه و تاخیر گوشی
                startPoll();
            }

            @Override
            public void onReceivedError(WebView v, WebResourceRequest r, WebResourceError e) {
                if (r.isForMainFrame()) fail(Lang.t("conn_fail"));
            }

            @Override
            public void onReceivedHttpError(WebView v, WebResourceRequest r, WebResourceResponse resp) {
                if (r.isForMainFrame() && resp.getStatusCode() >= 400)
                    fail(Lang.f("http_fail", resp.getStatusCode()));
            }

            @Override
            public boolean onRenderProcessGone(WebView v, RenderProcessGoneDetail d) {
                fail(Lang.t("crash"));
                return true;
            }
        });

        web.setWebChromeClient(new WebChromeClient() {
            @Override
            public void onPermissionRequest(final PermissionRequest request) {
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        handleMedia(request);
                    }
                });
            }

            @Override
            public void onGeolocationPermissionsShowPrompt(String origin, GeolocationPermissions.Callback cb) {
                if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED) {
                    cb.invoke(origin, true, false);
                } else {
                    pendingGeoOrigin = origin;
                    pendingGeoCb = cb;
                    requestPermissions(new String[]{Manifest.permission.ACCESS_FINE_LOCATION}, REQ_GEO);
                }
            }

            @Override
            public void onProgressChanged(WebView v, int progress) {
                advance(progress * 0.2f);
                hideFsHint(v);
            }
        });

        web.loadUrl("http://127.0.0.1:" + localPort + path);
    }

    // ---------- صفحه‌ی لودینگ (نوار پیشرفت) ----------
    // ۰ تا ۲۰٪: دانلود HTML از کامپیوتر (پیشرفت صفحه‌ی WebView)؛ ۲۰ تا ۱۰۰٪: نمایش صفحه، اتصال و اولین تصویر.
    // وقتی صفحه آماده شد (اتصال برقرار و اولین فریم یا حالت بدون تصویر) لودینگ محو میشه.
    private static final String READY_JS = "(function(){"
            + "var nb=document.getElementById('nameBox');if(nb&&nb.classList.contains('open'))return true;"
            + "var d=document.getElementById('dot');if(!d||!d.classList.contains('ok'))return false;"
            + "var sc=document.getElementById('scv');if(!sc||getComputedStyle(sc).display==='none')return true;"
            + "var w=document.getElementById('scw');return !!w&&w.classList.contains('hide');})()";
    private FrameLayout loader;
    private ProgressBar bar;
    private final Handler ui = new Handler(Looper.getMainLooper());
    private float pct;
    private int polls;
    private boolean ready;
    private final Runnable pollRun = new Runnable() {
        @Override
        public void run() {
            if (ready || web == null) return;
            web.evaluateJavascript(READY_JS, new ValueCallback<String>() {
                @Override
                public void onReceiveValue(String s) {
                    if (ready) return;
                    if ("true".equals(s) || ++polls > 150) finishLoading();
                    else {
                        advance(Math.min(pct + 0.5f, 92f));     // آروم پیش میره تا وقتی صفحه آماده میشه، ولی از ۹۲٪ رد نمیشه
                        ui.postDelayed(pollRun, 120);
                    }
                }
            });
        }
    };

    private void buildLoader() {
        loader = new FrameLayout(this);
        loader.setBackgroundColor(Color.parseColor("#12151D"));
        loader.setClickable(true);
        LinearLayout col = new LinearLayout(this);
        col.setOrientation(LinearLayout.VERTICAL);
        col.setGravity(Gravity.CENTER_HORIZONTAL);
        TextView tv = new TextView(this);
        tv.setText(Lang.t("connecting"));
        tv.setTextColor(Color.parseColor("#B4C8F2"));
        tv.setTextSize(15);
        col.addView(tv, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT));
        bar = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        bar.setMax(1000);
        bar.setProgressTintList(ColorStateList.valueOf(Color.parseColor("#5B92FF")));
        bar.setProgressBackgroundTintList(ColorStateList.valueOf(Color.parseColor("#262C3F")));
        LinearLayout.LayoutParams blp = new LinearLayout.LayoutParams(dp(220), dp(8));
        blp.topMargin = dp(16);
        col.addView(bar, blp);
        loader.addView(col, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, ViewGroup.LayoutParams.WRAP_CONTENT, Gravity.CENTER));
        root.addView(loader, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
    }

    private int dp(int v) {
        return Math.round(v * getResources().getDisplayMetrics().density);
    }

    private void advance(float p) {
        if (ready || loader == null) return;
        if (p > pct) {
            pct = Math.min(100f, p);
            bar.setProgress(Math.round(pct * 10));
        }
    }

    private void startPoll() {
        if (ready) return;
        ui.removeCallbacks(pollRun);
        polls = 0;
        ui.post(pollRun);
    }

    private void finishLoading() {
        if (ready) return;
        ready = true;
        ui.removeCallbacks(pollRun);
        advance(100f);
        loader.animate().alpha(0f).setDuration(220).withEndAction(new Runnable() {
            @Override
            public void run() {
                loader.setVisibility(View.GONE);
            }
        }).start();
    }

    // ---------- دسترسی‌ها (میکروفون / دوربین) ----------

    private static String androidPermFor(String resource) {
        if (PermissionRequest.RESOURCE_AUDIO_CAPTURE.equals(resource)) return Manifest.permission.RECORD_AUDIO;
        if (PermissionRequest.RESOURCE_VIDEO_CAPTURE.equals(resource)) return Manifest.permission.CAMERA;
        return null;
    }

    private void handleMedia(PermissionRequest request) {
        List<String> need = new ArrayList<String>();
        for (String res : request.getResources()) {
            String p = androidPermFor(res);
            if (p != null && checkSelfPermission(p) != PackageManager.PERMISSION_GRANTED && !need.contains(p)) need.add(p);
        }
        if (need.isEmpty()) {
            grantAllowed(request);
        } else {
            if (pendingMedia != null) pendingMedia.deny();
            pendingMedia = request;
            requestPermissions(need.toArray(new String[0]), REQ_MEDIA);
        }
    }

    private void grantAllowed(PermissionRequest request) {
        List<String> ok = new ArrayList<String>();
        for (String res : request.getResources()) {
            String p = androidPermFor(res);
            if (p == null || checkSelfPermission(p) == PackageManager.PERMISSION_GRANTED) ok.add(res);
        }
        if (ok.isEmpty()) request.deny();
        else request.grant(ok.toArray(new String[0]));
    }

    @Override
    public void onRequestPermissionsResult(int code, String[] perms, int[] results) {
        super.onRequestPermissionsResult(code, perms, results);
        if (code == REQ_MEDIA && pendingMedia != null) {
            PermissionRequest r = pendingMedia;
            pendingMedia = null;
            grantAllowed(r);
        } else if (code == REQ_GEO && pendingGeoCb != null) {
            boolean ok = results.length > 0 && results[0] == PackageManager.PERMISSION_GRANTED;
            pendingGeoCb.invoke(pendingGeoOrigin, ok, false);
            pendingGeoCb = null;
        }
    }

    // ---------- تمام‌صفحه ----------

    /** برنامه خودش تمام‌صفحه‌ست؛ پیام «برای فول‌اسکرین کلیک کن» صفحه لازم نیست */
    private static void hideFsHint(WebView v) {
        v.evaluateJavascript("(function(){if(document.getElementById('__nofs'))return;"
                + "var s=document.createElement('style');s.id='__nofs';"
                + "s.textContent='#fsHint{display:none!important}';"
                + "(document.head||document.documentElement).appendChild(s);})()", null);
    }

    @SuppressWarnings("deprecation")
    private void immersive() {
        getWindow().getDecorView().setSystemUiVisibility(
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                        | View.SYSTEM_UI_FLAG_FULLSCREEN
                        | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                        | View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                        | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                        | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION);
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) immersive();
    }

    // ---------- خطا / خروج ----------

    private void fail(String msg) {
        if (failed) return;
        failed = true;
        Toast.makeText(this, msg, Toast.LENGTH_LONG).show();
        finish();
    }

    private long lastBack;
    private Toast backToast;

    /** اول پنل‌های بازِ صفحه (تنظیمات، ظاهر، ...) بسته میشن؛ اگه چیزی باز نبود، با دوبار زدن برگشت خارج میشه. */
    @Override
    @SuppressWarnings("deprecation")
    public void onBackPressed() {
        if (web != null && ready) {
            web.evaluateJavascript("(function(){try{return !!(window.hshBack&&window.hshBack());}catch(e){return false;}})()",
                    new ValueCallback<String>() {
                        @Override
                        public void onReceiveValue(String r) {
                            if (!"true".equals(r)) exitOnDouble();
                        }
                    });
        } else {
            exitOnDouble();
        }
    }

    private void exitOnDouble() {
        long now = android.os.SystemClock.elapsedRealtime();
        if (now - lastBack < 2000) {
            if (backToast != null) backToast.cancel();
            finish();
            return;
        }
        lastBack = now;
        if (backToast != null) backToast.cancel();
        backToast = Toast.makeText(this, Lang.t("press_back_exit"), Toast.LENGTH_SHORT);
        backToast.show();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        ui.removeCallbacks(pollRun);
        if (web != null) {
            root.removeView(web);
            web.destroy();
            web = null;
        }
        if (proxy != null) proxy.stop();
    }
}
