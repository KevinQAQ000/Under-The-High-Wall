from PIL import Image

src = Image.open(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_14.png').convert('RGB')

cols = [(1930, 2118), (2122, 2310)]
rows = [(150, 468), (460, 722), (714, 976), (968, 1230)]

sheet = Image.new('RGB', (188 * 2 + 30, 312 * 4 + 50), (20, 20, 20))
i = 0
for ri, (y0, y1) in enumerate(rows):
    for ci, (x0, x1) in enumerate(cols):
        box = (x0, y0, x1, y1)
        c = src.crop(box)
        print(i, box, c.size)
        sheet.paste(c.resize((188, 312)), (ci * 198, ri * 322))
        i += 1
sheet.save(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\crops\cards_sheet.png')
print('saved')
