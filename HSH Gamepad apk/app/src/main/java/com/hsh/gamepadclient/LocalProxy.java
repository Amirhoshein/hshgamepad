package com.hsh.gamepadclient;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.Collections;
import java.util.HashSet;
import java.util.Set;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.ThreadFactory;

/**
 * پروکسی TCP ساده روی 127.0.0.1 که همه‌چیز (HTTP و WebSocket) رو بی‌تغییر به سرور کامپیوتر می‌رسونه.
 *
 * چرا لازمه؟ کروم/WebView فقط صفحه‌هایی با آدرس «امن» (https یا localhost) اجازه‌ی میکروفون، ژیروسکوپ و
 * شتاب‌سنج و ... میده. صفحه از طریق http://127.0.0.1 باز میشه، پس همه‌ی این قابلیت‌ها فعاله،
 * ولی سرور تغییری نمی‌کنه.
 */
final class LocalProxy {
    private static final int PREFERRED_PORT = 38765;   // ثابت، تا localStorage صفحه (تنظیمات دسته) بین اجراها بمونه

    private final String host;
    private final int port;
    private ServerSocket server;
    private volatile boolean closed;
    private final Set<Socket> open = Collections.synchronizedSet(new HashSet<Socket>());
    private final ExecutorService pool = Executors.newCachedThreadPool(new ThreadFactory() {
        @Override
        public Thread newThread(Runnable r) {
            Thread t = new Thread(r, "proxy");
            t.setDaemon(true);
            return t;
        }
    });

    LocalProxy(String host, int port) {
        this.host = host;
        this.port = port;
    }

    /** پورت محلی رو برمی‌گردونه */
    int start() throws IOException {
        InetAddress lo = InetAddress.getByName("127.0.0.1");
        try {
            server = new ServerSocket();
            server.setReuseAddress(true);
            server.bind(new InetSocketAddress(lo, PREFERRED_PORT));
        } catch (IOException e) {
            try {
                if (server != null) server.close();
            } catch (IOException ignored) {
            }
            server = new ServerSocket(0, 50, lo);
        }
        pool.execute(new Runnable() {
            @Override
            public void run() {
                acceptLoop();
            }
        });
        return server.getLocalPort();
    }

    void stop() {
        closed = true;
        try {
            if (server != null) server.close();
        } catch (IOException ignored) {
        }
        synchronized (open) {
            for (Socket s : open) closeQuietly(s);
            open.clear();
        }
        pool.shutdownNow();
    }

    private void acceptLoop() {
        while (!closed) {
            try {
                final Socket c = server.accept();
                pool.execute(new Runnable() {
                    @Override
                    public void run() {
                        handle(c);
                    }
                });
            } catch (Exception e) {
                if (closed) return;
            }
        }
    }

    private void handle(final Socket client) {
        final Socket remote = new Socket();
        open.add(client);
        open.add(remote);
        try {
            remote.connect(new InetSocketAddress(host, port), 4000);
            client.setTcpNoDelay(true);
            remote.setTcpNoDelay(true);
            remote.setKeepAlive(true);
            pool.execute(new Runnable() {
                @Override
                public void run() {
                    pipe(remote, client);
                }
            });
            pipe(client, remote);
        } catch (Exception e) {
            closeBoth(client, remote);
        }
    }

    private void pipe(Socket from, Socket to) {
        byte[] buf = new byte[16384];
        try {
            InputStream in = from.getInputStream();
            OutputStream out = to.getOutputStream();
            int n;
            while ((n = in.read(buf)) >= 0) {
                out.write(buf, 0, n);
                out.flush();
            }
        } catch (Exception ignored) {
        } finally {
            closeBoth(from, to);
        }
    }

    private void closeBoth(Socket a, Socket b) {
        closeQuietly(a);
        closeQuietly(b);
        open.remove(a);
        open.remove(b);
    }

    private static void closeQuietly(Socket s) {
        try {
            s.close();
        } catch (Exception ignored) {
        }
    }
}
