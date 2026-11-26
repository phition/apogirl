"""게임 위키(Docs/Wiki/*.md) → 게임 안 PC통신 '이제누리'의 생존 백과(Assets/_Project/Resources/Bbs/wiki.json).

위키를 고친 뒤 저장소 루트에서 돌린다:  python Tools/sync_wiki.py
- 머리말(--- ... ---)의 id·title·category·spoiler를 읽는다. `ingame: false` 문서는 넣지 않는다.
- 본문은 파란 글자 화면에 그대로 찍히도록 마크다운을 걷어 낸다: 제목 → 【 】·■, 표 → "칸 │ 칸", 링크·굵게·코드 표시 제거.
"""
import glob
import io
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Docs', 'Wiki')
DST = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Bbs', 'wiki.json')


def front_matter(text):
    meta = {}
    if text.startswith('---'):
        end = text.find('\n---', 3)
        if end > 0:
            for line in text[3:end].strip().split('\n'):
                if ':' in line:
                    key, value = line.split(':', 1)
                    meta[key.strip()] = value.split('#')[0].strip()
            text = text[end + 4:]
    return meta, text


def inline(line):
    line = re.sub(r'!\[[^\]]*\]\([^)]*\)', '', line)             # 그림
    line = re.sub(r'\[([^\]]+)\]\([^)]*\)', r'\1', line)         # 링크 → 글만
    line = re.sub(r'`([^`]*)`', r'\1', line)                     # 코드
    line = re.sub(r'\*\*([^*]+)\*\*', r'\1', line)               # 굵게
    line = re.sub(r'(?<![*\w])\*([^*\n]+)\*(?![*\w])', r'\1', line)
    line = re.sub(r'</?(details|summary|br|u|b|i)[^>]*>', '', line)
    return line.replace('&nbsp;', ' ').rstrip()


def plain(body):
    out = []
    for raw in body.replace('\r\n', '\n').split('\n'):
        line = raw.rstrip()
        if line.startswith('```'):
            continue
        m = re.match(r'^(#{1,6})\s+(.*)$', line)
        if m:
            title = inline(m.group(2))
            level = len(m.group(1))
            if out and out[-1] != '':
                out.append('')
            out.append('【 %s 】' % title if level <= 2 else '■ ' + title)
            continue
        if re.match(r'^\s*\|?\s*:?-{2,}', line) and '|' in line:      # 표의 구분 줄
            continue
        if line.lstrip().startswith('|'):
            cells = [inline(c.strip()) for c in line.strip().strip('|').split('|')]
            out.append('  ' + ' │ '.join(cells))
            continue
        if line.lstrip().startswith('>'):
            out.append('  ' + inline(line.lstrip()[1:].strip()))
            continue
        m = re.match(r'^(\s*)[-*]\s+(.*)$', line)
        if m:
            out.append(m.group(1) + '· ' + inline(m.group(2)))
            continue
        out.append(inline(line))
    # 빈 줄이 겹치면 하나로
    text = re.sub(r'\n{3,}', '\n\n', '\n'.join(out)).strip()
    return text


def main():
    pages = []
    for path in sorted(glob.glob(os.path.join(SRC, '*.md'))):
        meta, body = front_matter(io.open(path, encoding='utf-8').read())
        if meta.get('ingame', '').lower() == 'false' or meta.get('id') == 'wiki.index':
            continue   # 개발자용 문서와 대문(링크 목차)은 통신 화면에 넣지 않는다 — 갈래 목록이 목차를 대신한다
        name = os.path.splitext(os.path.basename(path))[0]
        pages.append({
            'Id': meta.get('id', name),
            'File': name,
            'Title': meta.get('title', name),
            'Category': meta.get('category', ''),
            'Spoiler': meta.get('spoiler', 'none'),
            'Body': plain(body),
        })
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    io.open(DST, 'w', encoding='utf-8', newline='\n').write(json.dumps({'Pages': pages}, ensure_ascii=False, indent=1) + '\n')
    print('wiki pages', len(pages), '->', os.path.relpath(DST, ROOT))


if __name__ == '__main__':
    main()
