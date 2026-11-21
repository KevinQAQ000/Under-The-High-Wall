import re, sys, zlib, json
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

for i in sorted(offsets):
    b = obj_bytes(i)
    head = b[:250].replace(b'\r', b' ').replace(b'\n', b' ')
    stream = b'stream' in b
    print(f'--- {i} len={len(b)} stream={stream}')
    print('   ', head[:240])
