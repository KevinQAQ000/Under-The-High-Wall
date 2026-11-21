from PIL import Image
import os

base = r'D:\Github\Under-The-High-Wall\Under The High Wall\_analysis\pages'
out = os.path.join(base, 'crops')
os.makedirs(out, exist_ok=True)

def crop(page, box, name, scale=2):
    im = Image.open(os.path.join(base, f'page_{page:02d}.png'))
    c = im.crop(box)
    c = c.resize((int(c.width * scale), int(c.height * scale)), Image.LANCZOS)
    p = os.path.join(out, name)
    c.save(p)
    print('WROTE', p, c.size)

# page 16 flowchart left side (2400x1350)
crop(16, (10, 180, 700, 1230), 'p16_flow.png', 2.4)
# page 14 character cards right side
crop(14, (1860, 150, 2400, 1200), 'p14_chars.png', 3.0)
# page 14 venue cards
crop(14, (990, 150, 1900, 560), 'p14_venue.png', 2.2)
# page 15 monsters
crop(15, (20, 150, 500, 900), 'p15_monsters.png', 3.0)
# page 15 drop cards
crop(15, (520, 150, 1010, 900), 'p15_drop.png', 3.0)
# page 15 props
crop(15, (100, 890, 2010, 1230), 'p15_props.png', 2.2)
# page 13 mindmap
crop(13, (650, 150, 1190, 1220), 'p13_mindmap.png', 2.6)
