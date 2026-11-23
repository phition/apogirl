"""yarn 대사집 편집기 — 로컬 서버.

Resources/Dialogue 아래 .yarn 파일을 장면(노드) 단위로 보여 주고, 고친 장면을 원본 파일에 다시 쓴다.
안 고친 부분은 바이트 그대로 남는다(줄바꿈 CRLF/LF·BOM 유지).

실행:  python Tools/YarnBook/yarn_book.py [--root 대본폴더] [--port 8765] [--open]
그다음 브라우저에서 http://127.0.0.1:8765 를 연다. 저장한 뒤 Unity에서 ApoGirl/Content/Import Events.
"""
import argparse
import hashlib
import json
import re
import sys
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
DEFAULT_ROOT = REPO / "Assets" / "_Project" / "Resources" / "Dialogue"
GROUP_ORDER = ["Prologue", "Act1", "Act2", "Act3", "Act4", "Events"]
TITLE_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


# ---------- 파싱·직렬화 (왕복 무손실) ----------

def parse(raw: bytes):
    """파일 바이트 → {bom, nl, nodes, tail}. 줄 목록을 그대로 나눠 담아 다시 이으면 원본과 같다."""
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw[3:].decode("utf-8") if bom else raw.decode("utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.replace("\r\n", "\n").split("\n")
    nodes, i, n = [], 0, len(lines)
    while True:
        start = i
        while i < n and lines[i].strip() == "":
            i += 1
        # 남은 줄에 '---'와 '==='가 차례로 없으면 꼬리
        j = i
        while j < n and lines[j].strip() != "---":
            j += 1
        k = j + 1
        while k < n and lines[k].strip() != "===":
            k += 1
        if i >= n or j >= n or k >= n:
            tail = lines[start:]
            break
        nodes.append({
            "pre": lines[start:i],
            "head": lines[i:j],
            "sep": lines[j],
            "body": lines[j + 1:k],
            "end": lines[k],
        })
        i = k + 1
    return {"bom": bom, "nl": nl, "nodes": nodes, "tail": tail}


def serialize(doc) -> bytes:
    out = []
    for nd in doc["nodes"]:
        out += nd["pre"] + nd["head"] + [nd["sep"]] + nd["body"] + [nd["end"]]
    out += doc["tail"]
    text = doc["nl"].join(out)
    data = text.encode("utf-8")
    return (b"\xef\xbb\xbf" + data) if doc["bom"] else data


def head_get(head, key):
    for line in head:
        k, sep, v = line.partition(":")
        if sep and k.strip() == key:
            return v.strip()
    return ""


def head_set(head, key, value):
    out, done = [], False
    for line in head:
        k, sep, _ = line.partition(":")
        if sep and k.strip() == key and not done:
            out.append(f"{key}: {value}" if value else None)
            done = True
        else:
            out.append(line)
    out = [l for l in out if l is not None]
    if not done and value:
        out.append(f"{key}: {value}")
    return out


# ---------- 파일 묶음 ----------

def yarn_files(root: Path):
    def key(p):
        g = p.parent.name
        return (GROUP_ORDER.index(g) if g in GROUP_ORDER else 99, str(p.relative_to(root)).lower())
    return sorted(root.rglob("*.yarn"), key=key)


def load_all(root: Path):
    files = []
    for p in yarn_files(root):
        raw = p.read_bytes()
        doc = parse(raw)
        files.append({
            "path": p.relative_to(root).as_posix(),
            "group": p.parent.name,
            "name": p.stem,
            "sha": hashlib.sha1(raw).hexdigest(),
            "nodes": [{
                "title": head_get(nd["head"], "title"),
                "tags": head_get(nd["head"], "tags"),
                "body": "\n".join(nd["body"]),
            } for nd in doc["nodes"]],
        })
    return files


def save(root: Path, req):
    """req = {files:[{path, sha, edits:[{index, title, tags, body}]}]}. 파일별로 검사하고 통과한 것만 쓴다."""
    current = load_all(root)
    titles = {}  # 저장 뒤 기준 제목 → 위치
    by_path = {f["path"]: f for f in current}
    edits_by_path = {f["path"]: {e["index"]: e for e in f.get("edits", [])} for f in req.get("files", [])}
    for f in current:
        ed = edits_by_path.get(f["path"], {})
        for idx, nd in enumerate(f["nodes"]):
            t = ed[idx]["title"].strip() if idx in ed else nd["title"]
            titles.setdefault(t, []).append(f"{f['path']}#{idx + 1}")

    results, errors = [], []
    for fr in req.get("files", []):
        path = fr.get("path", "")
        cur = by_path.get(path)
        if cur is None:
            errors.append(f"{path}: 파일이 없습니다.")
            continue
        if cur["sha"] != fr.get("sha"):
            errors.append(f"{path}: 불러온 뒤 디스크에서 바뀌었습니다. 다시 불러온 뒤 고쳐 주세요.")
            continue
        file_path = (root / path).resolve()
        if root.resolve() not in file_path.parents:
            errors.append(f"{path}: 대본 폴더 밖 경로입니다.")
            continue
        doc = parse(file_path.read_bytes())
        bad = []
        for e in fr.get("edits", []):
            idx = e.get("index")
            if not isinstance(idx, int) or not (0 <= idx < len(doc["nodes"])):
                bad.append(f"장면 번호 {idx}가 맞지 않습니다.")
                continue
            title = e.get("title", "").strip()
            tags = " ".join(e.get("tags", "").split())
            body = e.get("body", "").replace("\r\n", "\n").replace("\r", "\n")
            body_lines = body.split("\n")
            if not TITLE_RE.match(title):
                bad.append(f"'{title}': 장면 ID는 영문·숫자·밑줄만 씁니다.")
            elif len(titles.get(title, [])) > 1:
                bad.append(f"'{title}': 같은 장면 ID가 또 있습니다 ({', '.join(titles[title])}).")
            if any(l.strip() in ("===", "---") for l in body_lines):
                bad.append(f"'{title}': 본문에 '===' 또는 '---'만 있는 줄은 쓸 수 없습니다.")
            nd = doc["nodes"][idx]
            nd["head"] = head_set(head_set(nd["head"], "title", title), "tags", tags)
            nd["body"] = body_lines
        if bad:
            errors += [f"{path}: {b}" for b in bad]
            continue
        data = serialize(doc)
        file_path.write_bytes(data)
        results.append(path)
    return {"saved": results, "errors": errors, "files": load_all(root)}


# ---------- HTTP ----------

def make_handler(root: Path):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, fmt, *args):
            pass

        def _send(self, code, body: bytes, ctype):
            self.send_response(code)
            self.send_header("Content-Type", ctype)
            self.send_header("Content-Length", str(len(body)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(body)

        def _json(self, obj, code=200):
            self._send(code, json.dumps(obj, ensure_ascii=False).encode("utf-8"), "application/json; charset=utf-8")

        def do_GET(self):
            if self.path in ("/", "/index.html"):
                self._send(200, (HERE / "index.html").read_bytes(), "text/html; charset=utf-8")
            elif self.path == "/api/files":
                self._json({"root": str(root), "files": load_all(root)})
            else:
                self._send(404, b"not found", "text/plain")

        def do_POST(self):
            # 다른 사이트에서 보낸 요청은 받지 않는다
            origin = self.headers.get("Origin")
            host = self.headers.get("Host", "")
            if origin and origin.split("://", 1)[-1] != host:
                self._json({"errors": ["다른 출처의 요청입니다."]}, 403)
                return
            if self.path != "/api/save":
                self._send(404, b"not found", "text/plain")
                return
            try:
                n = int(self.headers.get("Content-Length", "0"))
                req = json.loads(self.rfile.read(n).decode("utf-8"))
                res = save(root, req)
                self._json(res)
                for p in res["saved"]:
                    print(f"저장: {p}", flush=True)
            except Exception as ex:  # 잘못된 요청
                self._json({"errors": [f"저장 실패: {ex}"]}, 400)

    return Handler


def self_test(root: Path):
    """모든 파일이 파싱 → 직렬화로 바이트 그대로 돌아오는지 확인."""
    bad = 0
    for p in yarn_files(root):
        raw = p.read_bytes()
        if serialize(parse(raw)) != raw:
            print(f"왕복 불일치: {p}")
            bad += 1
    count = len(yarn_files(root))
    print(f"왕복 점검: {count}개 중 {count - bad}개 일치")
    return bad == 0


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(description="yarn 대사집 편집기")
    ap.add_argument("--root", type=Path, default=DEFAULT_ROOT)
    ap.add_argument("--port", type=int, default=8765)
    ap.add_argument("--open", action="store_true", help="브라우저를 연다")
    ap.add_argument("--check", action="store_true", help="왕복 점검만 하고 끝낸다")
    a = ap.parse_args()
    root = a.root.resolve()
    if not root.is_dir():
        sys.exit(f"대본 폴더가 없습니다: {root}")
    ok = self_test(root)
    if a.check:
        sys.exit(0 if ok else 1)
    if not ok:
        sys.exit("왕복이 맞지 않는 파일이 있어 저장을 막으려고 끝냅니다.")
    url = f"http://127.0.0.1:{a.port}"
    print(f"대본 폴더: {root}\n열기: {url}  (끝내려면 Ctrl+C)")
    if a.open:
        webbrowser.open(url)
    ThreadingHTTPServer(("127.0.0.1", a.port), make_handler(root)).serve_forever()


if __name__ == "__main__":
    main()
