package com.hsh.gamepadclient;

import android.Manifest;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.hardware.Camera;
import android.os.Bundle;
import android.view.Gravity;
import android.view.SurfaceHolder;
import android.view.SurfaceView;
import android.view.ViewGroup;
import android.widget.FrameLayout;
import android.widget.TextView;
import android.widget.Toast;

import com.google.zxing.BarcodeFormat;
import com.google.zxing.BinaryBitmap;
import com.google.zxing.DecodeHintType;
import com.google.zxing.MultiFormatReader;
import com.google.zxing.PlanarYUVLuminanceSource;
import com.google.zxing.Result;
import com.google.zxing.common.HybridBinarizer;

import java.util.Collections;
import java.util.EnumMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicBoolean;

/** اسکنر QR ساده (دوربین قدیمی + ZXing core)، بدون هیچ کتابخونه‌ی رابط کاربری. */
@SuppressWarnings("deprecation")
public class QrActivity extends Activity implements SurfaceHolder.Callback {
    private static final int REQ_CAM = 21;

    private FrameLayout root;
    private SurfaceView surface;
    private Camera camera;
    private boolean surfaceReady;
    private final AtomicBoolean busy = new AtomicBoolean(false);
    private final AtomicBoolean done = new AtomicBoolean(false);
    private ExecutorService decoder;
    private MultiFormatReader reader;

    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        Lang.init(this);
        root = new FrameLayout(this);
        root.setBackgroundColor(Color.BLACK);

        surface = new SurfaceView(this);
        surface.getHolder().addCallback(this);
        root.addView(surface, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT, Gravity.CENTER));

        TextView hint = new TextView(this);
        hint.setText(Lang.t("qr_hint"));
        hint.setTextColor(Color.WHITE);
        hint.setBackgroundColor(0x99000000);
        hint.setGravity(Gravity.CENTER);
        hint.setPadding(24, 24, 24, 24);
        root.addView(hint, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT, Gravity.BOTTOM));
        setContentView(root);

        Map<DecodeHintType, Object> hints = new EnumMap<DecodeHintType, Object>(DecodeHintType.class);
        hints.put(DecodeHintType.POSSIBLE_FORMATS, Collections.singletonList(BarcodeFormat.QR_CODE));
        hints.put(DecodeHintType.TRY_HARDER, Boolean.TRUE);
        reader = new MultiFormatReader();
        reader.setHints(hints);
        decoder = Executors.newSingleThreadExecutor();

        if (checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED)
            requestPermissions(new String[]{Manifest.permission.CAMERA}, REQ_CAM);
    }

    @Override
    public void onRequestPermissionsResult(int code, String[] perms, int[] results) {
        super.onRequestPermissionsResult(code, perms, results);
        if (code == REQ_CAM) {
            if (results.length > 0 && results[0] == PackageManager.PERMISSION_GRANTED) {
                startCamera();
            } else {
                Toast.makeText(this, Lang.t("cam_denied"), Toast.LENGTH_LONG).show();
                finish();
            }
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        startCamera();
    }

    @Override
    protected void onPause() {
        super.onPause();
        stopCamera();
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        if (decoder != null) decoder.shutdownNow();
    }

    @Override
    public void surfaceCreated(SurfaceHolder holder) {
        surfaceReady = true;
        startCamera();
    }

    @Override
    public void surfaceChanged(SurfaceHolder holder, int format, int w, int h) {
    }

    @Override
    public void surfaceDestroyed(SurfaceHolder holder) {
        surfaceReady = false;
        stopCamera();
    }

    private void startCamera() {
        if (camera != null || !surfaceReady) return;
        if (checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) return;
        try {
            camera = Camera.open();
            Camera.Parameters p = camera.getParameters();

            // اندازه‌ی پیش‌نمایش نزدیک به 1280x720
            Camera.Size best = null;
            List<Camera.Size> sizes = p.getSupportedPreviewSizes();
            for (Camera.Size s : sizes) {
                if (best == null || Math.abs(s.width * s.height - 1280 * 720) < Math.abs(best.width * best.height - 1280 * 720))
                    best = s;
            }
            if (best != null) p.setPreviewSize(best.width, best.height);
            if (p.getSupportedFocusModes().contains(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE))
                p.setFocusMode(Camera.Parameters.FOCUS_MODE_CONTINUOUS_PICTURE);
            camera.setParameters(p);

            // صفحه همیشه عمودی (portrait) هست
            Camera.CameraInfo info = new Camera.CameraInfo();
            Camera.getCameraInfo(0, info);
            camera.setDisplayOrientation(info.orientation % 360);

            // نسبت تصویر درست (بدون کشیدگی)
            if (best != null) fit(best.height, best.width);

            camera.setPreviewDisplay(surface.getHolder());
            final Camera.Size ps = camera.getParameters().getPreviewSize();
            camera.setPreviewCallback(new Camera.PreviewCallback() {
                @Override
                public void onPreviewFrame(final byte[] data, Camera c) {
                    if (done.get() || !busy.compareAndSet(false, true)) return;
                    decoder.execute(new Runnable() {
                        @Override
                        public void run() {
                            try {
                                PlanarYUVLuminanceSource src = new PlanarYUVLuminanceSource(
                                        data, ps.width, ps.height, 0, 0, ps.width, ps.height, false);
                                Result r = reader.decodeWithState(new BinaryBitmap(new HybridBinarizer(src)));
                                onResult(r.getText());
                            } catch (Exception ignored) {
                                // QR پیدا نشد؛ فریم بعدی
                            } finally {
                                reader.reset();
                                busy.set(false);
                            }
                        }
                    });
                }
            });
            camera.startPreview();
        } catch (Exception e) {
            stopCamera();
            Toast.makeText(this, Lang.t("cam_fail"), Toast.LENGTH_LONG).show();
            finish();
        }
    }

    /** نمایش با نسبت (w:h) در وسط صفحه، بدون کشیدگی */
    private void fit(int w, int h) {
        int sw = getResources().getDisplayMetrics().widthPixels;
        int sh = getResources().getDisplayMetrics().heightPixels;
        int tw = sw;
        int th = sw * h / w;
        if (th < sh) {          // پر کردن کل صفحه (کمی بریده میشه)
            th = sh;
            tw = sh * w / h;
        }
        surface.setLayoutParams(new FrameLayout.LayoutParams(tw, th, Gravity.CENTER));
    }

    private void stopCamera() {
        if (camera == null) return;
        try {
            camera.setPreviewCallback(null);
            camera.stopPreview();
        } catch (Exception ignored) {
        }
        try {
            camera.release();
        } catch (Exception ignored) {
        }
        camera = null;
    }

    private void onResult(final String text) {
        if (!done.compareAndSet(false, true)) return;
        runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Intent i = new Intent();
                i.putExtra("text", text);
                setResult(RESULT_OK, i);
                finish();
            }
        });
    }
}
