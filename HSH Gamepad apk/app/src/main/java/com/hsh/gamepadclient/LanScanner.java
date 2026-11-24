package com.hsh.gamepadclient;

import java.io.InputStream;
import java.io.OutputStream;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.InterfaceAddress;
import java.net.NetworkInterface;
import java.net.Socket;
import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ThreadFactory;
import java.util.concurrent.TimeUnit;

/** شبکه‌ی محلی رو برای سرور HSH Gamepad (پورت 80 و 1111..9999) اسکن می‌کنه. */
final class LanScanner {

    interface Listener {
        void onFound(String host, int port);

        /** noNetwork = true یعنی هیچ شبکه‌ی محلی (وای‌فای/هات‌اسپات) پیدا نشد */
        void onDone(boolean noNetwork);
    }

    static final int[] PORTS = {80, 1111, 2222, 3333, 4444, 5555, 6666, 7777, 8888, 9999};
    private static final String MARKER = "HSH Gamepad";

    private volatile boolean cancelled;
    private ExecutorService pool;

    void cancel() {
        cancelled = true;
        ExecutorService p = pool;
        if (p != null) p.shutdownNow();
    }

    void start(final Listener l) {
        Thread t = new Thread(new Runnable() {
            @Override
            public void run() {
                scan(l);
            }
        }, "lan-scan");
        t.setDaemon(true);
        t.start();
    }

    private void scan(final Listener l) {
        final List<int[]> nets = localNets();   // {firstHost, lastHost, ownIp}
        if (nets.isEmpty()) {
            if (!cancelled) l.onDone(true);
            return;
        }

        pool = Executors.newFixedThreadPool(200, new ThreadFactory() {
            @Override
            public Thread newThread(Runnable r) {
                Thread t = new Thread(r);
                t.setDaemon(true);
                return t;
            }
        });

        for (int[] n : nets) {
            for (int ip = n[0]; ip <= n[1]; ip++) {
                if (ip == n[2]) continue;
                final String host = ipToString(ip);
                for (final int port : PORTS) {
                    try {
                        pool.execute(new Runnable() {
                            @Override
                            public void run() {
                                if (cancelled) return;
                                if (probe(host, port) && !cancelled) l.onFound(host, port);
                            }
                        });
                    } catch (Exception ignored) {
                        return;     // pool بسته شده (cancel)
                    }
                }
            }
        }

        pool.shutdown();
        try {
            pool.awaitTermination(90, TimeUnit.SECONDS);
        } catch (InterruptedException ignored) {
        }
        if (!cancelled) l.onDone(false);
    }

    private static boolean probe(String host, int port) {
        Socket s = new Socket();
        try {
            s.connect(new InetSocketAddress(host, port), 400);
            s.setSoTimeout(1500);
            OutputStream o = s.getOutputStream();
            o.write(("GET / HTTP/1.1\r\nHost: " + host + "\r\nConnection: close\r\n\r\n").getBytes("US-ASCII"));
            o.flush();
            InputStream in = s.getInputStream();
            byte[] buf = new byte[16384];
            int total = 0;
            while (total < buf.length) {
                int n = in.read(buf, total, buf.length - total);
                if (n < 0) break;
                total += n;
                if (new String(buf, 0, total, "UTF-8").contains(MARKER)) return true;
            }
        } catch (Exception ignored) {
        } finally {
            try {
                s.close();
            } catch (Exception ignored) {
            }
        }
        return false;
    }

    private static List<int[]> localNets() {
        List<int[]> res = new ArrayList<int[]>();
        Set<String> seen = new LinkedHashSet<String>();
        try {
            for (NetworkInterface ni : Collections.list(NetworkInterface.getNetworkInterfaces())) {
                if (!ni.isUp() || ni.isLoopback()) continue;
                String name = ni.getName().toLowerCase();
                if (name.startsWith("tun") || name.startsWith("ppp") || name.startsWith("rmnet")
                        || name.startsWith("dummy") || name.startsWith("ccmni") || name.contains("tap")) continue;
                for (InterfaceAddress ia : ni.getInterfaceAddresses()) {
                    InetAddress a = ia.getAddress();
                    if (!(a instanceof Inet4Address) || !a.isSiteLocalAddress()) continue;
                    byte[] b = a.getAddress();
                    int ip = ((b[0] & 255) << 24) | ((b[1] & 255) << 16) | ((b[2] & 255) << 8) | (b[3] & 255);
                    int prefix = ia.getNetworkPrefixLength();
                    if (prefix < 24) prefix = 24;      // فقط /24 اطراف خودمون
                    if (prefix > 30) prefix = 30;
                    int mask = -1 << (32 - prefix);
                    int net = ip & mask;
                    int first = net + 1;
                    int last = (net | ~mask) - 1;
                    if (seen.add(first + ":" + last)) res.add(new int[]{first, last, ip});
                }
            }
        } catch (Exception ignored) {
        }
        return res;
    }

    private static String ipToString(int ip) {
        return ((ip >> 24) & 255) + "." + ((ip >> 16) & 255) + "." + ((ip >> 8) & 255) + "." + (ip & 255);
    }
}
