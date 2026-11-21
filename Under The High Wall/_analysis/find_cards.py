from PIL import Image
import numpy as np

im = Image.open(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_14.png').convert('RGB')
a = np.asarray(im).astype(int)
h, w, _ = a.shape
print('size', w, h)

# 角色卡红色边框：R 明显大于 G、B
r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
red = (r > 90) & (r > g + 40) & (r > b + 30)
# 只看右半部分
red[:, :1800] = False
cols = red.sum(axis=0)
rows = red.sum(axis=1)
print('red pixels', int(red.sum()))

# 找列区间
def runs(v, thresh):
    out = []
    start = None
    for i, x in enumerate(v):
        if x > thresh and start is None:
            start = i
        elif x <= thresh and start is not None:
            out.append((start, i - 1))
            start = None
    if start is not None:
        out.append((start, len(v) - 1))
    return out

cr = [x for x in runs(cols, 40) if x[1] - x[0] > 30]
rr = [x for x in runs(rows, 40) if x[1] - x[0] > 30]
print('col runs', cr)
print('row runs', rr)
