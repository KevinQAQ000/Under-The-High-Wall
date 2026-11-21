import re, zlib, io
from PIL import Image

data = open(r'C:\Users\EDY\Desktop\Portfolio_LuoZhiyang.pdf', 'rb').read()
n = len(data)
sx = data.rfind(b'startxref')
start = int(data[sx:n].split()[1])
pos = start + 4
while data[pos:pos+1] in (b'\r', b'\n', b' '):
    pos += 1
offsets = {}
while True:
    hm = re.match(rb'(\d+)\s+(\d+)\s*\r?\n', data[pos:pos+40])
    if not hm:
        break
    first, count = int(hm.group(1)), int(hm.group(2))
    pos += hm.end()
    for i in range(count):
        om = re.match(rb'(\d{10})\s+(\d{5})\s+([nf])', data[pos:pos+20])
        off, gen, typ = int(om.group(1)), int(om.group(2)), om.group(3)
        if typ == b'n':
            offsets[first + i] = off
        pos += om.end()
        while data[pos:pos+1] in (b'\r', b'\n', b' '):
            pos += 1
    break

def obj_bytes(num):
    off = offsets[num]
    e = data.find(b'endobj', off)
    return data[off:e + 6]

def stream_bytes(num):
    b = obj_bytes(num)
    s = b.find(b'stream')
    # skip stream + EOL
    p = s + 6
    if b[p:p+2] == b'\r\n':
        p += 2
    elif b[p:p+1] in (b'\r', b'\n'):
        p += 1
    e = b.rfind(b'endstream')
    return b[p:e]

def img_obj(num):
    b = obj_bytes(num)
    w = int(re.search(rb'/Width (\d+)', b).group(1))
    h = int(re.search(rb'/Height (\d+)', b).group(1))
    cs = re.search(rb'/ColorSpace /(\w+)', b).group(1).decode()
    raw = zlib.decompress(stream_bytes(num))
    print(num, w, h, cs, len(raw), 'expected', w * h * (3 if cs == 'DeviceRGB' else 1))
    mode = 'RGB' if cs == 'DeviceRGB' else 'L'
    return Image.frombytes(mode, (w, h), raw)

kids = [6, 11, 14, 17, 20, 23, 26, 29, 32, 35, 38, 41, 44, 47, 50, 53, 56, 59, 62, 65, 68, 71, 74, 77]
mask = img_obj(9)
print('mask extrema', mask.getextrema())

for page_no in [12, 13, 14, 15, 16]:
    page_obj = kids[page_no - 1]
    b = obj_bytes(page_obj)
    im_ref = int(re.search(rb'/XObject <</IM\d+ (\d+) 0 R>>', b).group(1))
    img = img_obj(im_ref)
    if img.size != mask.size:
        print('size mismatch', img.size, mask.size)
    rgb = img.convert('RGB')
    # composite over white using mask
    bg = Image.new('RGB', img.size, (255, 255, 255))
    bg.paste(rgb, (0, 0), mask)
    out = rf'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_{page_no:02d}.png'
    bg.save(out, optimize=True)
    print('WROTE', out, bg.size)
