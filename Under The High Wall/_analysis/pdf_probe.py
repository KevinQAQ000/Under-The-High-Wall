import re, sys, zlib, json
data = open(r'C:\Users\EDY\Desktop\Portfolio_LuoZhiyang.pdf', 'rb').read()
n = len(data)

# locate xref
sx = data.rfind(b'startxref')
start = int(data[sx:n].split()[1])
print('startxref', start, 'there:', data[start:start+20])

# parse classic xref table
pos = start
m = re.match(rb'xref\s*', data[pos:pos+20])
print('match', bool(m))
pos += m.end()
offsets = {}
while True:
    hm = re.match(rb'(\d+)\s+(\d+)\s*\r?\n?', data[pos:pos+40])
    if not hm:
        break
    first, count = int(hm.group(1)), int(hm.group(2))
    pos += hm.end()
    if first == 0 and count == 1:
        # skip free entry line
        pass
    for i in range(count):
        line = data[pos:pos+20]
        om = re.match(rb'(\d{10})\s+(\d{5})\s+([nf])', line)
        if not om:
            print('bad entry at', pos, line[:20]); break
        off, gen, typ = int(om.group(1)), int(om.group(2)), om.group(3)
        if typ == b'n':
            offsets[first+i] = off
        pos += om.end()
        while data[pos:pos+1] in (b'\r', b'\n', b' '):
            pos += 1
    break

print('objects found', len(offsets), 'max', max(offsets))

def obj_bytes(num):
    off = offsets[num]
    # find 'endobj'
    e = data.find(b'endobj', off)
    return data[off:e+6]

def obj_dict(num):
    b = obj_bytes(num)
    return b

cat = obj_dict(1)
print('CATALOG', cat[:300])
