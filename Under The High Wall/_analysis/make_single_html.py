import base64
import gzip
import io
import os

ROOT = r'D:\Github\Under-The-High-Wall\Under The High Wall'
BUILD = os.path.join(ROOT, 'Builds', 'WebGL')
OUT = os.path.join(ROOT, 'Builds', 'UnderTheHighWall.html')


def read(path):
    with open(path, 'rb') as f:
        return f.read()


def b64_gz(raw, label):
    buf = io.BytesIO()
    with gzip.GzipFile(fileobj=buf, mode='wb', compresslevel=9, mtime=0) as gz:
        gz.write(raw)
    packed = buf.getvalue()
    print(f'  {label}: {len(raw)/1048576:.1f} MB -> {len(packed)/1048576:.1f} MB gz')
    return base64.b64encode(packed).decode('ascii')


loader = read(os.path.join(BUILD, 'Build', 'WebGL.loader.js')).decode('utf-8').replace('</script', '<\\/script')

print('packing resources')
data_b64 = b64_gz(read(os.path.join(BUILD, 'Build', 'WebGL.data')), 'data')
wasm_b64 = b64_gz(read(os.path.join(BUILD, 'Build', 'WebGL.wasm')), 'wasm')
fw_b64 = b64_gz(read(os.path.join(BUILD, 'Build', 'WebGL.framework.js')), 'framework')

css = read(os.path.join(BUILD, 'TemplateData', 'style.css')).decode('utf-8')

html = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=no">
<title>高墙之下 Under The High Wall</title>
<style>
__CSS__
body { margin: 0; background: #0b0b0d; overflow: hidden; }
#unity-container { width: 100vw; height: 100vh; }
#unity-canvas { width: 100%; height: 100%; display: block; background: #0b0b0d; }
#unity-progress-bar-full { width: 0; }
#offline-note { position: fixed; left: 0; right: 0; bottom: 10px; text-align: center;
  color: #8a8a92; font: 13px/1.5 "Microsoft YaHei", sans-serif; }
#load-status { position: fixed; left: 0; right: 0; top: 46%; text-align: center;
  color: #d6d3cd; font: 15px/1.6 "Microsoft YaHei", sans-serif; letter-spacing: .5px; }
</style>
</head>
<body>
<div id="unity-container" class="unity-desktop">
  <canvas id="unity-canvas" width="1280" height="720" tabindex="-1"></canvas>
  <div id="unity-loading-bar">
    <div id="unity-logo"></div>
    <div id="unity-progress-bar-empty">
      <div id="unity-progress-bar-full"></div>
    </div>
  </div>
  <div id="unity-warning"></div>
</div>
<div id="load-status">正在解压游戏资源…</div>
<div id="offline-note">单文件离线版：首次打开需要解压约几十秒，请等加载条走完。</div>
<script>
__LOADER__
</script>
<script>
(function () {
  var dataB64 = "__DATA__";
  var wasmB64 = "__WASM__";
  var fwB64 = "__FW__";

  function b64ToBytes(b64) {
    var bin = atob(b64);
    var len = bin.length;
    var bytes = new Uint8Array(len);
    for (var i = 0; i < len; i++) bytes[i] = bin.charCodeAt(i);
    return bytes;
  }

  function gunzip(bytes) {
    if (typeof DecompressionStream === "undefined") return Promise.reject(new Error("浏览器不支持 DecompressionStream"));
    var stream = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
    return new Response(stream).arrayBuffer();
  }

  var statusEl = document.querySelector("#load-status");
  function status(text) { if (statusEl) statusEl.textContent = text; }

  function boot() {
    var t0 = performance.now();
    document.querySelector("#unity-loading-bar").style.display = "block";
    status("正在解压游戏资源…");
    Promise.all([gunzip(b64ToBytes(dataB64)), gunzip(b64ToBytes(wasmB64)), gunzip(b64ToBytes(fwB64))])
      .then(function (parts) {
        var dataUrl = URL.createObjectURL(new Blob([parts[0]], { type: "application/octet-stream" }));
        var codeUrl = URL.createObjectURL(new Blob([parts[1]], { type: "application/wasm" }));
        var fwUrl = URL.createObjectURL(new Blob([parts[2]], { type: "text/javascript" }));
        var canvas = document.querySelector("#unity-canvas");
        var config = {
          arguments: [],
          dataUrl: dataUrl,
          frameworkUrl: fwUrl,
          codeUrl: codeUrl,
          companyName: "DefaultCompany",
          productName: "Under The High Wall",
          productVersion: "0.1.0",
          showBanner: function (msg, type) {
            status("加载失败，请看页面上的提示。");
        var box = document.querySelector("#unity-warning");
            var div = document.createElement("div");
            div.innerHTML = msg;
            div.style = type === "error" ? "background:#a11;padding:10px;color:#fff" : "padding:10px;color:#ddd";
            box.appendChild(div);
          }
        };
        status("正在启动引擎…");
        return createUnityInstance(canvas, config, function (progress) {
          var bar = document.querySelector("#unity-progress-bar-full");
          if (bar) bar.style.width = (8 + 92 * progress) + "%";
        });
      })
      .then(function () {
        document.querySelector("#unity-loading-bar").style.display = "none";
        document.querySelector("#offline-note").style.display = "none";
        if (statusEl) statusEl.style.display = "none";
        var secs = ((performance.now() - t0) / 1000).toFixed(1);
        document.title = "高墙之下（加载 " + secs + " 秒）";
      })
      .catch(function (err) {
        status("加载失败，请看页面上的提示。");
        var box = document.querySelector("#unity-warning");
        box.innerHTML = '<div style="background:#a11;color:#fff;padding:12px;font:14px sans-serif">' +
          '加载失败：' + (err && err.message ? err.message : err) +
          '<br>请用 Chrome / Edge 打开本文件。</div>';
        console.error(err);
      });
  }

  boot();
})();
</script>
</body>
</html>
"""

html = (html
        .replace('__CSS__', css)
        .replace('__LOADER__', loader)
        .replace('__DATA__', data_b64)
        .replace('__WASM__', wasm_b64)
        .replace('__FW__', fw_b64))

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(html)

print('written', OUT, f'{os.path.getsize(OUT)/1048576:.1f} MB')
