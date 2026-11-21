from PIL import Image
import os

src = Image.open(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_14.png').convert('RGB')
out = r'D:\Github\Under-The-High-Wall\Under The High Wall\Assets\Art\Characters'
os.makedirs(out, exist_ok=True)

boxes = {
    'classmate': (2122, 150, 2310, 468),
    'teacher':   (1930, 460, 2118, 722),
    'security':  (2122, 460, 2310, 722),
    'mother':    (1930, 714, 2118, 976),
    'doctor':    (2122, 714, 2310, 976),
    'confidant': (1930, 968, 2118, 1230),
}

sheet = Image.new('RGB', (6 * 150 + 10, 190), (18, 18, 18))
for i, (name, box) in enumerate(boxes.items()):
    card = src.crop(box)
    w, h = card.size
    card.resize((w * 2, h * 2), Image.LANCZOS).save(os.path.join(out, name + '_card.png'))
    # 头像：卡片上方 58%，左右各留 6% 边
    inset_x = int(w * 0.06)
    ph = int(h * 0.58)
    head = card.crop((inset_x, 0, w - inset_x, ph))
    head.resize((head.width * 3, head.height * 3), Image.LANCZOS).save(os.path.join(out, name + '_portrait.png'))
    sheet.paste(head.resize((140, int(140 * head.height / head.width))), (i * 150, 10))
    print(name, card.size, '->', head.size)

sheet.save(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\crops\portraits_sheet.png')
print('saved')
