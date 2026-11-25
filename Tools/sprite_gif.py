#!/usr/bin/env python3
"""스프라이트 프레임을 묶음별 재생 GIF로 만든다.

프레임 이름이 `{묶음}_{n}.png`이면 같은 묶음끼리 n 순서로 이어 GIF 하나를 만든다
(예: player_summer_bat_attack_0..23.png → player_summer_bat_attack.gif).
번호 없는 `{이름}.png`은 정지 그림 `{이름}_still.gif`로 따로 만든다.

빠르기는 게임 탐험 화면(ExpeditionDirector) 값을 따른다 — 묶음 이름 끝 동작으로 고름.
  주인공 도는 동작: 대기·지침·수색·잠듦 7fps, 걷기 18, 달리기 26, 웅크려 걷기 12
      (16장·12장 기준. 프레임이 더 많으면 한 바퀴 시간이 같게 fps를 늘림)
  주인공 한 번 동작: 공격·밀치기 0.5초, 다침 0.4초, 숨기 1.5초 + 끝에서 --hold ms 멈춤
  좀비(zombie_*): 걷기 11, 쫓기 20, 대기 6, 잠/휴면 3.6, 휘청 9.6
  그 밖: 10fps 반복
--fps 또는 --seconds로 덮어쓸 수 있다.

쓰기 (PIL 필요 — 시스템 python에 있음):
  python Tools/sprite_gif.py <폴더 또는 png ...> [--out 폴더] [--pattern "player_summer_*"]
         [--bg 3a3c42 | --bg transparent] [--min-height 240] [--scale N] [--fps N | --seconds S]
         [--hold 400] [--strip]
  - 폴더 이름이 frames면 기본 출력은 옆의 gifs/, 아니면 <폴더>/gifs/.
  - 프레임 크기가 달라도 가운데·발 바닥(아래)에 맞춰 한 캔버스에 놓는다.
  - --strip: 묶음마다 모든 프레임을 가로로 늘어놓은 PNG(_strip.png)도 만든다.
"""
import argparse, fnmatch, os, re, sys
from collections import defaultdict

try:
    from PIL import Image
except ImportError:  # 사용자 site-packages에만 있는 경우
    sys.path.append(os.path.expandvars(r"%APPDATA%\Python\Python313\site-packages"))
    from PIL import Image

LONG, SHORT = 16, 12  # ExpeditionDirector.LoopDesignedLong / Short
PLAYER_LOOP = {"idle": (7, LONG), "exhausted": (7, SHORT), "search": (7, SHORT), "doze": (7, SHORT),
               "walk": (18, LONG), "run": (26, LONG), "crouch": (12, LONG)}
PLAYER_ONCE = {"attack": 0.5, "shove": 0.5, "hurt": 0.4, "hide": 1.5, "open": 0.8}
ZOMBIE_LOOP = {"walk": 11, "chase": 20, "idle": 6, "sleep": 3.6, "dormant": 3.6, "stun": 9.6}
ZOMBIE_ONCE = {"hit": 0.6, "pushed": 0.6, "down": 1.0}
DEFAULT_FPS = 10

NUM = re.compile(r"^(.*?)(?:_(\d+))?\.png$", re.I)


def timing(bundle, count, args):
    """(프레임당 ms 목록, 반복 동작인가)"""
    action = bundle.split("_")[-1]
    zombie = bundle.startswith("zombie")
    if args.seconds:
        ms = args.seconds * 1000 / count
        return [ms] * count, False
    if args.fps:
        return [1000 / args.fps] * count, True
    if zombie:
        if action in ZOMBIE_ONCE:
            return [ZOMBIE_ONCE[action] * 1000 / count] * count, False
        return [1000 / ZOMBIE_LOOP.get(action, DEFAULT_FPS)] * count, True
    if action in PLAYER_LOOP:
        fps, designed = PLAYER_LOOP[action]
        return [1000 / (fps * count / designed)] * count, True
    if action in PLAYER_ONCE:
        return [PLAYER_ONCE[action] * 1000 / count] * count, False
    return [1000 / DEFAULT_FPS] * count, True


def collect(inputs, pattern):
    """묶음 이름 → [(번호, 경로)]"""
    files = []
    for p in inputs:
        if os.path.isdir(p):
            files += [os.path.join(p, n) for n in os.listdir(p) if n.lower().endswith(".png")]
        elif p.lower().endswith(".png"):
            files.append(p)
    groups = defaultdict(list)
    for f in files:
        m = NUM.match(os.path.basename(f))
        if not m:
            continue
        base, idx = m.group(1), m.group(2)
        key = base if idx is not None else base + "_still"
        if pattern and not fnmatch.fnmatch(base, pattern):
            continue
        groups[key].append((int(idx) if idx is not None else 0, f))
    return {k: [f for _, f in sorted(v)] for k, v in groups.items()}


def default_out(inputs):
    first = inputs[0] if os.path.isdir(inputs[0]) else os.path.dirname(os.path.abspath(inputs[0]))
    first = os.path.abspath(first)
    if os.path.basename(first).lower() == "frames":
        return os.path.join(os.path.dirname(first), "gifs")
    return os.path.join(first, "gifs")


def parse_bg(s):
    if s.lower() == "transparent":
        return None
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4)) + (255,)


def make(bundle, paths, out_dir, args, bg):
    imgs = [Image.open(p).convert("RGBA") for p in paths]
    w = max(i.width for i in imgs)
    h = max(i.height for i in imgs)
    s = args.scale or (max(1, -(-args.min_height // h)) if h < args.min_height else 1)
    canvases = []
    for im in imgs:
        c = Image.new("RGBA", (w, h), bg or (0, 0, 0, 0))
        c.alpha_composite(im, ((w - im.width) // 2, h - im.height))  # 가운데·발 바닥 맞춤
        if s > 1:
            c = c.resize((w * s, h * s), Image.NEAREST)
        canvases.append(c)

    frames = []
    for c in canvases:
        if bg is None:  # 투명: 알파 128 미만을 투명 칸(255번)으로
            alpha = c.getchannel("A")
            q = c.convert("RGB").quantize(colors=255, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
            mask = alpha.point(lambda a: 255 if a < 128 else 0)
            q.paste(255, mask=mask)
            q.info["transparency"] = 255
            frames.append(q)
        else:
            frames.append(c.convert("RGB").quantize(colors=255, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))

    durs, loop = timing(bundle, len(frames), args)
    durs = [max(20, int(round(d / 10.0)) * 10) for d in durs]  # GIF는 0.01초 단위, 20ms 미만은 브라우저가 느리게 틂
    if not loop and len(frames) > 1:
        durs[-1] += args.hold
    if len(frames) == 1:
        durs = [1000]
    extra = {"transparency": 255} if bg is None else {}
    frames[0].save(os.path.join(out_dir, bundle + ".gif"), save_all=True, append_images=frames[1:],
                   duration=durs, loop=0, optimize=False, disposal=2 if bg is None else 1, **extra)
    if args.strip:
        strip = Image.new("RGBA", (canvases[0].width * len(canvases), canvases[0].height), (0, 0, 0, 0))
        for i, c in enumerate(canvases):
            strip.alpha_composite(c, (i * c.width, 0))
        strip.save(os.path.join(out_dir, bundle + "_strip.png"))


def main():
    ap = argparse.ArgumentParser(description="스프라이트 프레임 → 묶음별 재생 GIF")
    ap.add_argument("inputs", nargs="+", help="프레임 폴더 또는 png 파일")
    ap.add_argument("--out", help="출력 폴더 (기본: frames 옆 gifs/ 또는 <폴더>/gifs/)")
    ap.add_argument("--pattern", help='묶음 이름 거르기 (fnmatch, 예: "player_summer_*_attack")')
    ap.add_argument("--bg", default="3a3c42", help="바탕색 hex 또는 transparent (기본 어두운 회색)")
    ap.add_argument("--min-height", type=int, default=240, help="이보다 낮으면 정수배로 키움 (기본 240)")
    ap.add_argument("--scale", type=int, help="배율을 직접 정함 (min-height 무시)")
    ap.add_argument("--fps", type=float, help="모든 묶음을 이 fps 반복으로")
    ap.add_argument("--seconds", type=float, help="모든 묶음을 이 길이(초)의 한 번 동작으로")
    ap.add_argument("--hold", type=int, default=400, help="한 번 동작 끝에서 멈추는 ms (기본 400)")
    ap.add_argument("--strip", action="store_true", help="프레임을 가로로 늘어놓은 PNG도 만든다")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

    groups = collect(args.inputs, args.pattern)
    if not groups:
        sys.exit("프레임을 찾지 못했다")
    out = args.out or default_out(args.inputs)
    os.makedirs(out, exist_ok=True)
    bg = parse_bg(args.bg)
    for i, (bundle, paths) in enumerate(sorted(groups.items()), 1):
        make(bundle, paths, out, args, bg)
        if i % 50 == 0:
            print(f"  {i}/{len(groups)}")
    print(f"{len(groups)}개 GIF → {out}")


if __name__ == "__main__":
    main()
