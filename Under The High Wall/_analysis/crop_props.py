from PIL import Image
import os

page = Image.open(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\page_15.png').convert('RGB')
out = r'D:\Github\Under-The-High-Wall\Under The High Wall\Assets\Art\Props'
os.makedirs(out, exist_ok=True)

# 第 15 页道具卡网格：14 列 × 2 行，卡面约 117×164，列距 140
left, top = 123, 903
cw, ch = 117, 164
pitch_x, pitch_y = 140, 177

top_names = ['holy_water', 'special_dagger', 'candlestick', 'hatchet', 'eraser', 'demon_hunter',
             'broken_doll', 'crowbar', 'utility_knife', 'theater_costumes', 'classmate_utility_knife',
             'classmate_diary', 'holy_cross', 'bat']
bottom_names = ['torch', 'dagger', 'special_bat', 'crude_slingshot', 'potion_bandage', 'charm',
                'mithril_revolver', 'flashlight', 'campus_guide', 'infected_whiskey', 'dynamite',
                'textbook', 'classmate_pastries', 'broken_cross']

sheet = Image.new('RGB', (14 * 126, 2 * 176 + 20), (20, 20, 22))
for row, names in enumerate((top_names, bottom_names)):
    for i, name in enumerate(names):
        x = int(round(left + i * pitch_x))
        y = int(round(top + row * pitch_y))
        card = page.crop((x, y, x + cw, y + ch))
        card = card.resize((cw * 2, ch * 2), Image.LANCZOS)
        card.save(os.path.join(out, f'{row}_{i:02d}_{name}.png'))
        thumb = card.resize((118, 165), Image.LANCZOS)
        sheet.paste(thumb, (i * 126, row * 176 + 10))

sheet.save(r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages\crops\props_sheet.png')
print('done', len(top_names) + len(bottom_names))
