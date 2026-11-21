"""最小的 CDP 客户端：打开页面、等真实时间、抓控制台、截图。

不依赖第三方库，只用 http.client 做 WebSocket 握手，手写帧解析。
"""
import base64
import http.client
import json
import os
import socket
import struct
import subprocess
import sys
import time

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
PROFILE = os.path.join(os.environ["TEMP"], "dsh_cdp_profile")
URL = sys.argv[1]
OUT = sys.argv[2]
WAIT = float(sys.argv[3]) if len(sys.argv) > 3 else 40.0
PORT = 9333


class WS:
    def __init__(self, url):
        # ws://127.0.0.1:9333/devtools/page/XXX
        assert url.startswith("ws://")
        host_port, path = url[5:].split("/", 1)
        host, port = host_port.split(":")
        self.sock = socket.create_connection((host, int(port)), timeout=30)
        key = base64.b64encode(os.urandom(16)).decode()
        req = (
            f"GET /{path} HTTP/1.1\r\nHost: {host_port}\r\n"
            "Upgrade: websocket\r\nConnection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n"
        )
        self.sock.sendall(req.encode())
        resp = b""
        while b"\r\n\r\n" not in resp:
            resp += self.sock.recv(1)
        assert b"101" in resp.split(b"\r\n")[0], resp
        self.buf = b""
        self.next_id = 0

    def _read(self, n):
        while len(self.buf) < n:
            chunk = self.sock.recv(65536)
            if not chunk:
                raise EOFError
            self.buf += chunk
        out, self.buf = self.buf[:n], self.buf[n:]
        return out

    def send(self, payload):
        data = json.dumps(payload).encode()
        mask = os.urandom(4)
        header = bytearray([0x81])
        length = len(data)
        if length < 126:
            header.append(0x80 | length)
        elif length < 65536:
            header.append(0x80 | 126)
            header += struct.pack(">H", length)
        else:
            header.append(0x80 | 127)
            header += struct.pack(">Q", length)
        masked = bytes(b ^ mask[i % 4] for i, b in enumerate(data))
        self.sock.sendall(bytes(header) + mask + masked)

    def recv(self):
        b1, b2 = self._read(2)
        opcode = b1 & 0x0F
        length = b2 & 0x7F
        if length == 126:
            length = struct.unpack(">H", self._read(2))[0]
        elif length == 127:
            length = struct.unpack(">Q", self._read(8))[0]
        payload = self._read(length)
        if opcode == 0x8:
            raise EOFError
        if opcode == 0x9:  # ping -> pong
            self.sock.sendall(b"\x8a\x00")
            return self.recv()
        return payload.decode("utf-8", "replace")

    def call(self, method, params=None):
        self.next_id += 1
        mid = self.next_id
        self.send({"id": mid, "method": method, "params": params or {}})
        while True:
            msg = json.loads(self.recv())
            if msg.get("id") == mid:
                return msg


def get_targets():
    conn = http.client.HTTPConnection("127.0.0.1", PORT, timeout=5)
    conn.request("GET", "/json/list")
    body = conn.getresponse().read().decode()
    return json.loads(body)


def main():
    subprocess.run(["taskkill", "/F", "/IM", "chrome.exe"], capture_output=True)
    time.sleep(1)
    proc = subprocess.Popen([
        CHROME, "--headless=new", "--disable-gpu", "--use-angle=swiftshader",
        "--enable-unsafe-swiftshader", "--allow-file-access-from-files",
        f"--user-data-dir={PROFILE}", f"--remote-debugging-port={PORT}",
        "--window-size=1280,720", "about:blank",
    ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    deadline = time.time() + 30
    targets = []
    while time.time() < deadline:
        try:
            targets = [t for t in get_targets() if t.get("type") == "page"]
            if targets:
                break
        except Exception:
            pass
        time.sleep(0.5)
    if not targets:
        print("no targets")
        proc.kill()
        return 1

    ws = WS(targets[0]["webSocketDebuggerUrl"])
    for m in ("Page.enable", "Runtime.enable", "Log.enable", "Emulation.setFocusEmulationEnabled"):
        try:
            ws.call(m, {"enabled": True} if m.startswith("Emulation") else {})
        except Exception:
            pass
    try:
        ws.call("Page.bringToFront")
    except Exception:
        pass
    ws.call("Page.navigate", {"url": URL})

    start = time.time()
    console = []
    while time.time() - start < WAIT:
        try:
            msg = json.loads(ws.recv())
        except Exception as exc:
            print("recv ended:", exc)
            break
        method = msg.get("method")
        if method == "Runtime.consoleAPICalled":
            args = [a.get("value", a.get("description", "")) for a in msg["params"]["args"]]
            console.append("console: " + " ".join(str(a) for a in args))
        elif method == "Runtime.exceptionThrown":
            d = msg["params"]["exceptionDetails"]
            console.append("exception: " + d.get("text", "") + " " + str(d.get("exception", {}).get("description", ""))[:400])
        elif method == "Log.entryAdded":
            e = msg["params"]["entry"]
            console.append(f"log[{e.get('level')}]: {e.get('text','')[:300]}")
        if time.time() - start > WAIT - 2:
            break

    click = os.environ.get("CLICK", "")
    if click:
        nx, ny = [float(v) for v in click.split(",")]
        js = f"""(function() {{
          var c = document.querySelector('#unity-canvas');
          var r = c.getBoundingClientRect();
          var x = r.left + r.width * {nx};
          var y = r.top + r.height * {ny};
          var opts = {{ bubbles: true, cancelable: true, clientX: x, clientY: y, pointerId: 1, pointerType: 'mouse', isPrimary: true, button: 0, buttons: 1 }};
          c.dispatchEvent(new PointerEvent('pointerdown', opts));
          c.dispatchEvent(new MouseEvent('mousedown', opts));
          c.dispatchEvent(new PointerEvent('pointerup', Object.assign({{}}, opts, {{ buttons: 0 }})));
          c.dispatchEvent(new MouseEvent('mouseup', Object.assign({{}}, opts, {{ buttons: 0 }})));
          return 'clicked ' + x + ',' + y + ' rect ' + r.width + 'x' + r.height;
        }})()"""
        print(ws.call("Runtime.evaluate", {"expression": js, "returnByValue": True}).get("result", {}).get("result", {}).get("value"))
        time.sleep(4)

    info = ws.call("Runtime.evaluate", {
        "expression": "({title: document.title, canvas: !!document.querySelector('#unity-canvas'), "
                      "note: (document.querySelector('#offline-note')||{}).style ? document.querySelector('#offline-note').style.display : 'n/a', "
                      "warn: (document.querySelector('#unity-warning')||{}).innerText || ''})",
        "returnByValue": True,
    })

    shot = ws.call("Page.captureScreenshot", {"format": "png"})
    data = shot.get("result", {}).get("data")
    if data:
        with open(OUT, "wb") as f:
            f.write(base64.b64decode(data))
        print("saved", OUT, os.path.getsize(OUT))
    print("state:", json.dumps(info.get("result", {}).get("result", {}).get("value", {}), ensure_ascii=False))
    for line in console[-25:]:
        print(line)
    proc.kill()
    return 0


if __name__ == "__main__":
    sys.exit(main())
