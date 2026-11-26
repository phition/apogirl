"""이벤트 원화를 게임에 넣는다: ArtInbox/illustrations/events_정리/사용중/*.png → Assets/_Project/Resources/Art/Story/Events/*.jpg

원화를 고치거나 바꿔 넣은 뒤 저장소 루트(ArtInbox가 있는 본 체크아웃)에서 돌린다:
    python Tools/apply_event_art.py            # 달라진 그림만 다시 만든다
    python Tools/apply_event_art.py --check    # 무엇이 바뀔지만 보여 준다

- 게임 그림은 너비 1920 jpg(품질 88)다. 파일 이름(확장자 빼고)이 그림 표 `Resources/Art/art_map.json`의 `Events/이름`과 같아야 한다.
- 사용중 폴더에 있는데 그림 표에 없는 이름은 알려 주기만 한다(게임에 넣으려면 art_map.json에 이벤트 줄을 더한다).
- 원화 폴더 구성: 사용중(게임에 들어간 판) · 교체된_옛판(다시 받아 바뀐 것) · 미사용(연결할 이벤트가 없는 것).
  게임 폴더에서 빠진 미사용 그림은 `Assets/_Project/Art/StoryUnused/Events/`에 있다(빌드에 안 들어간다).
"""
import io
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'ArtInbox', 'illustrations', 'events_정리', '사용중')
ART = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Art')
DST = os.path.join(ART, 'Story', 'Events')


def thumb(im):
    return list(im.convert('L').resize((32, 18), Image.BILINEAR).getdata())


def main():
    check = '--check' in sys.argv
    if not os.path.isdir(SRC):
        print('원화 폴더가 없다:', SRC)
        return

    names = set()

    def walk(o):
        if isinstance(o, dict):
            for v in o.values():
                if isinstance(v, str) and v.startswith('Events/'):
                    names.add(v.split('/', 1)[1])
                walk(v)
        elif isinstance(o, list):
            for v in o:
                walk(v)

    walk(json.load(io.open(os.path.join(ART, 'art_map.json'), encoding='utf-8')))
    changed, unmapped = [], []
    for f in sorted(os.listdir(SRC)):
        stem, ext = os.path.splitext(f)
        if ext.lower() not in ('.png', '.jpg', '.jpeg'):
            continue
        if stem not in names:
            unmapped.append(stem)
            continue
        src = Image.open(os.path.join(SRC, f)).convert('RGB')
        out = os.path.join(DST, stem + '.jpg')
        if os.path.exists(out):
            a, b = thumb(src), thumb(Image.open(out))
            if sum(abs(x - y) for x, y in zip(a, b)) / len(a) < 1.5:
                continue   # 지금 게임 그림과 같다
        changed.append(stem)
        if not check:
            im = src.resize((1920, round(src.height * 1920 / src.width)), Image.LANCZOS) if src.width != 1920 else src
            im.save(out, 'JPEG', quality=88, optimize=True)

    print(('바뀔 그림' if check else '다시 만든 그림'), len(changed))
    for name in changed:
        print('  ', name)
    if unmapped:
        print('그림 표(art_map.json)에 없는 이름', len(unmapped))
        for name in unmapped:
            print('  ', name)
    missing = sorted(n for n in names if not os.path.exists(os.path.join(DST, n + '.jpg')))
    if missing:
        print('그림 표에는 있는데 게임 폴더에 없는 그림', len(missing))
        for name in missing:
            print('  ', name)


if __name__ == '__main__':
    main()
