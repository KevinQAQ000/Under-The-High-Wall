import sys, io
sys.stdout.reconfigure(encoding="utf-8")
from docx import Document
from docx.table import Table
from docx.text.paragraph import Paragraph
from docx.oxml.ns import qn

path = sys.argv[1]
doc = Document(path)

def iter_block_items(parent):
    body = parent.element.body
    for child in body.iterchildren():
        if child.tag == qn('w:p'):
            yield Paragraph(child, parent)
        elif child.tag == qn('w:tbl'):
            yield Table(child, parent)

i = 0
for block in iter_block_items(doc):
    if isinstance(block, Paragraph):
        txt = block.text.strip()
        if not txt:
            continue
        style = block.style.name if block.style is not None else ''
        # detect numbered / heading
        print(f"[P{i}][{style}] {txt}")
        i += 1
    else:
        print(f"[TABLE {i}] rows={len(block.rows)} cols={len(block.columns)}")
        for r in block.rows:
            cells = []
            for c in r.cells:
                cells.append(c.text.replace('\n', ' / ').strip())
            print("  | " + " || ".join(cells))
        i += 1
