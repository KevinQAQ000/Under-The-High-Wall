from PIL import Image
import numpy as np, os, json

page = Image.open(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_15.png').convert('RGB')
a = np.array(page)
h, w, _ = a.shape

# 道具卡区域（第 15 页下半部分两行，每行 14 张）
y0, y1 = 880, 1245
x0, x1 = 90, 2025
band = a[y0:y1, x0:x1]
r, g, b = band[:, :, 0].astype(int), band[:, :, 1].astype(int), band[:, :, 2].astype(int)
red = (r > 110) & (g < 70) & (b < 70)
col = red.sum(axis=0)
row = red.sum(axis=1)

def runs(mask, min_len=3):
    out, start = [], None
    for i, v in enumerate(mask):
        if v and start is None:
            start = i
        if not v and start is not None:
            if i - start >= min_len:
                out.append((start, i - 1))
            start = None
    if start is not None:
        out.append((start, len(mask) - 1))
    return out

sep_cols = runs(col > (y1 - y0) * 0.5, 4)
sep_rows = runs(row > (x1 - x0) * 0.5, 4)
print('separator columns', [(s + x0, e + x0) for s, e in sep_cols])
print('separator rows', [(s + y0, e + y0) for s, e in sep_rows])
print('col peaks', [(i + x0, int(col[i])) for i in np.argsort(col)[-6:]])
