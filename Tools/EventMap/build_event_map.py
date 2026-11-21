"""이벤트 분기 지도 만들기: 1~52주의 이벤트·탐험 조우·행동·엔딩을 플래그로 이어 한 장의 대화형 HTML로.

사용 (저장소 루트에서):
    python Tools/EventMap/build_event_map.py [출력 경로]
기본 출력은 Builds/EventMap/event_map.html (git 제외). 브라우저로 열면 된다 (인터넷 없이 동작).

읽는 것
- Assets/_Project/Content/**/*.json : 이벤트(조건·시작 효과·선택지·연쇄), 행동 효과, 엔딩 조건
- Assets/_Project/Scripts/Editor/LocationBuilder.cs : 탐험 조우(조건 플래그·선택지가 켜는 플래그)
- Assets/_Project/Resources/Dialogue/**/*.yarn : 대사 첫 줄들과 선택지 문구 (#choice:ID)
- Assets/_Project/Localization/Source/ko.json : 엔딩 제목, 조우 문구
- Assets/_Project/Scripts/**/*.cs : 코드가 켜는 플래그(문자열)를 찾아 "코드" 출처로 표시

연결선
- flag : A가 켜는 플래그를 B의 조건(FlagsAll/FlagsAny)이 요구한다
- block : A가 켜는 플래그가 B의 조건(FlagsNone)을 막는다 (흐리게)
- then : 연쇄(ThenEventId / 선택지 NextEventId)
- npc : A가 동료 상태를 바꾸고 B가 그 상태를 요구한다
- clue : A가 단서를 주고 B(엔딩)가 단서 수를 본다
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PROJECT = os.path.join(ROOT, 'Assets', '_Project')
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'Builds', 'EventMap', 'event_map.html')
TEMPLATE = os.path.join(os.path.dirname(__file__), 'event_map_template.html')

TOTAL_WEEKS = 52


def load_json(path):
    with open(path, encoding='utf-8-sig') as f:
        return json.load(f)


# ── 문자열 표 ──
KO = load_json(os.path.join(PROJECT, 'Localization', 'Source', 'ko.json'))


def ko(key, fallback=''):
    return KO.get(key, fallback)


# ── Yarn: 노드별 대사 첫 줄들과 선택지 문구 ──
YARN = {}
for path in glob.glob(os.path.join(PROJECT, 'Resources', 'Dialogue', '**', '*.yarn'), recursive=True):
    text = open(path, encoding='utf-8-sig').read()
    for block in re.split(r'^===\s*$', text, flags=re.M):
        m = re.search(r'^title:\s*(\S+)', block, flags=re.M)
        if not m:
            continue
        body = block.split('---', 1)[1] if '---' in block else ''
        lines, options = [], {}
        for raw in body.splitlines():
            line = raw.strip()
            if not line or line.startswith('//') or line.startswith('<<'):
                continue
            o = re.match(r'->\s*(.*?)\s*#choice:(\w+)', line)
            if o:
                options.setdefault(o.group(2), o.group(1).strip())
                continue
            if line.startswith('->'):
                continue
            if len(lines) < 6 and raw.startswith(line[:1]) and not raw.startswith((' ', '\t')):
                lines.append(re.sub(r'\s*#\S+', '', line))
        YARN[m.group(1)] = {'lines': lines, 'options': options, 'file': os.path.relpath(path, PROJECT).replace('\\', '/')}

# ── 내용 파일 ──
events, actions, endings = [], [], []
for path in sorted(glob.glob(os.path.join(PROJECT, 'Content', '**', '*.json'), recursive=True)):
    data = load_json(path)
    if not isinstance(data, dict):
        continue
    rel = os.path.relpath(path, PROJECT).replace('\\', '/')
    for ev in data.get('Events', []):
        ev['_file'] = rel
        events.append(ev)
    for act in data.get('Actions', []):
        act['_file'] = rel
        actions.append(act)
    for end in data.get('Endings', []):
        end['_file'] = rel
        endings.append(end)


def flags_set(effect):
    return list((effect or {}).get('SetFlags', []))


def flags_cleared(effect):
    return list((effect or {}).get('ClearFlags', []))


def npc_changes(effect):
    out = []
    for n in (effect or {}).get('Npcs', []) or []:
        if n.get('SetStatus'):
            out.append(f"{n['NpcId']}={n['SetStatus']}")
    return out


def clues(effect):
    return list((effect or {}).get('AddClues', []))


def cond_summary(c):
    """조건을 사람이 읽는 짧은 줄들로."""
    if not c:
        return []
    out = []
    if c.get('WeekMin') or c.get('WeekMax'):
        out.append(f"주 {c.get('WeekMin') or 1}~{c.get('WeekMax') or TOTAL_WEEKS}")
    if c.get('Days'):
        out.append('요일 ' + ','.join(map(str, c['Days'])))
    if c.get('Phase'):
        out.append({'Day': '낮', 'Night': '밤'}.get(c['Phase'], c['Phase']))
    if c.get('Seasons'):
        out.append('계절 ' + ','.join(c['Seasons']))
    if c.get('Month'):
        out.append(f"{c['Month']}월 {c.get('DayOfMonth') or ''}일".replace(' 일', ''))
    if c.get('ActionCategories'):
        out.append('행동 분류 ' + ','.join(c['ActionCategories']))
    if c.get('ActionIds'):
        out.append('행동 ' + ','.join(c['ActionIds']))
    if c.get('FlagsAll'):
        out.append('필요: ' + ', '.join(c['FlagsAll']))
    if c.get('FlagsAny'):
        out.append('하나라도: ' + ', '.join(c['FlagsAny']))
    if c.get('FlagsNone'):
        out.append('없어야: ' + ', '.join(c['FlagsNone']))
    if c.get('CluesAll'):
        out.append('단서: ' + ', '.join(c['CluesAll']))
    for n in c.get('NpcStatuses', []) or []:
        out.append(f"동료 {n.get('NpcId')} = {n.get('Status')}")
    for ch in (c.get('Checks') or []) + (c.get('AnyChecks') or []):
        out.append(f"{ch.get('Kind')} {ch.get('Key', '')} {ch.get('Op', '')} {ch.get('Value', '')}".strip())
    if c.get('IsBirthday'):
        out.append('생일')
    return out


# ── 학사력: 날짜·계절 조건을 주로 (CalendarRules.DateOf, BalanceConfig.StartMonth/StartDayOfMonth) ──
_balance = open(os.path.join(PROJECT, 'Scripts', 'Core', 'Definitions', 'BalanceConfig.cs'), encoding='utf-8-sig').read()
START_MONTH = int(re.search(r'StartMonth\s*=\s*(\d+)', _balance).group(1))
START_DAY = int(re.search(r'StartDayOfMonth\s*=\s*(\d+)', _balance).group(1))
DAYS_IN_MONTH = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
SEASON_START = {'Spring': 1, 'Summer': 14, 'Autumn': 27, 'Winter': 40}


def date_of(week, day):
    month, date = START_MONTH, START_DAY + (week - 1) * 7 + (day - 1)
    while date > DAYS_IN_MONTH[month - 1]:
        date -= DAYS_IN_MONTH[month - 1]
        month = month % 12 + 1
    return month, date


def calendar_week(c):
    """조건이 처음 맞을 수 있는 주 (주 범위·날짜·계절 중 가장 늦은 것)."""
    w = c.get('WeekMin') or 1
    if c.get('Month'):
        for week in range(1, TOTAL_WEEKS + 1):
            hit = any(date_of(week, d)[0] == c['Month'] and (not c.get('DayOfMonth') or date_of(week, d)[1] == c['DayOfMonth']) for d in range(1, 8))
            if hit:
                w = max(w, week)
                break
    if c.get('Seasons'):
        w = max(w, min(SEASON_START.get(x, 1) for x in c['Seasons']))
    return w


# ── 줄(레인) ──
LANES = [
    ('A1', '1막 본편'), ('A2', '2막 본편'), ('A3', '3막 본편'), ('A4', '4막 본편'),
    ('DH', '도현'), ('NURSE', '박 선생님'), ('BY', '송예지'), ('HR', '최하린'),
    ('SAGEORI', '사거리 파'), ('MART', '더불어 마트'), ('UBC', 'UBC'), ('WANDER', '유랑자'),
    ('TALK', '밤 대화'), ('CAL', '학사 일정'), ('OUTING', '소풍·수학여행'), ('SYS', '시스템·거처'),
    ('RND', '무작위·날씨'), ('ACT', '행동'), ('ENDING', '엔딩'),
]
LANE_KEYS = [k for k, _ in LANES]


def lane_of_event(eid):
    s = eid.replace('EVT_', '')
    for key, lane in [('SAGEORI', 'SAGEORI'), ('KANGCHEOL', 'SAGEORI'), ('MART', 'MART'), ('UBC', 'UBC'), ('DJ', 'UBC'), ('MINGYU', 'UBC'),
                      ('WANDERER', 'WANDER'), ('RINGLEADER', 'HR'), ('BYSTANDER', 'BY'), ('NURSE', 'NURSE')]:
        if key in s and not s.startswith(('CAL_', 'TALK_', 'RND_', 'SYS_')):
            return lane
    for prefix, lane in [('DH_', 'DH'), ('NURSE_', 'NURSE'), ('BY_', 'BY'), ('HR_', 'HR'), ('CAL_', 'CAL'), ('TALK_', 'TALK'),
                         ('NIGHT_TEACHER', 'TALK'), ('OUTING_', 'OUTING'), ('TRIP_', 'OUTING'), ('SYS_', 'SYS'), ('RND_', 'RND'),
                         ('WX_', 'RND'), ('CRISIS_', 'RND'), ('NPC_SURVIVOR', 'RND'), ('NG_', 'A1'), ('A1_', 'A1'), ('A2_', 'A2'),
                         ('A3_', 'A3'), ('A4_', 'A4'), ('DAY_INTRUSION', 'RND')]:
        if s.startswith(prefix):
            return lane
    return 'RND'


nodes = {}


def add_node(nid, **kw):
    node = {'id': nid, 'kind': 'event', 'lane': 'RND', 'week': 0, 'weekMax': 0, 'produces': [], 'blocks': [], 'requires': [],
            'any': [], 'none': [], 'npcSet': [], 'npcNeed': [], 'clueSet': [], 'clueNeed': [], 'then': [], 'cond': [], 'choices': [],
            'lines': [], 'onShow': [], 'meta': {}}
    node.update(kw)
    nodes[nid] = node
    return node


# 이벤트
for ev in events:
    c = ev.get('Condition') or {}
    node = add_node(ev['Id'], kind='event', lane=lane_of_event(ev['Id']), week=calendar_week(c), weekMax=c.get('WeekMax') or 0,
                    requires=list(c.get('FlagsAll', [])), any=list(c.get('FlagsAny', [])), none=list(c.get('FlagsNone', [])),
                    npcNeed=[f"{n['NpcId']}={n['Status']}" for n in c.get('NpcStatuses', []) or []], clueNeed=list(c.get('CluesAll', [])),
                    cond=cond_summary(c))
    node['meta'] = {'파일': ev['_file'], '종류': ev.get('Kind', ''), '우선순위': ev.get('Priority', 0),
                    '한 번만': ev.get('Once', True), 'Yarn 노드': ev.get('DialogueNode') or ev['Id']}
    on_show = ev.get('OnShow') or {}
    node['onShow'] = flags_set(on_show) + ['동료 ' + x for x in npc_changes(on_show)] + ['단서 ' + x for x in clues(on_show)]
    node['produces'] += flags_set(on_show)
    node['npcSet'] += npc_changes(on_show)
    node['clueSet'] += clues(on_show)
    if ev.get('ThenEventId'):
        node['then'].append(ev['ThenEventId'])
    yarn = YARN.get(ev.get('DialogueNode') or ev['Id'], {})
    node['lines'] = yarn.get('lines', [])
    for ch in ev.get('Choices', []) or []:
        eff, fail = ch.get('Effect') or {}, ch.get('OnFailure') or {}
        sets = flags_set(eff) + [f for f in flags_set(fail) if f not in flags_set(eff)]
        node['produces'] += sets
        node['blocks'] += flags_cleared(eff)
        node['npcSet'] += npc_changes(eff)
        node['clueSet'] += clues(eff)
        nxt = ch.get('NextEventId') or eff.get('NextEventId')
        if nxt:
            node['then'].append(nxt)
        node['choices'].append({'id': ch['Id'], 'text': yarn.get('options', {}).get(ch['Id'], ''), 'flags': sets,
                                'npc': npc_changes(eff), 'cond': cond_summary(ch.get('Condition')), 'next': nxt or ''})
        # 선택지 조건의 플래그도 '쓰는' 플래그 (그 선택지를 열어 준다)
        node['any'] += [f for f in (ch.get('Condition') or {}).get('FlagsAll', []) if f not in node['requires']]

# 행동: 플래그를 켜는 것, 이벤트 조건(ActionIds)이 가리키는 것, 탐험(지역) 행동
referenced_actions = {a for ev in events for a in (ev.get('Condition') or {}).get('ActionIds', [])}
action_by_id = {a['Id']: a for a in actions}
for act in actions:
    eff = {}
    for key in ('Effect', 'OnSuccess', 'OnGreatSuccess'):
        for f in (act.get(key) or {}).get('SetFlags', []):
            if f not in eff.setdefault('SetFlags', []):
                eff['SetFlags'].append(f)
    sets = flags_set(eff)
    if not sets and act['Id'] not in referenced_actions and not act.get('LocationId'):
        continue
    u = act.get('Unlock') or {}
    add_node(act['Id'], kind='action', lane='ACT', week=u.get('WeekMin') or 0, requires=list(u.get('FlagsAll', [])),
             any=list(u.get('FlagsAny', [])), none=list(u.get('FlagsNone', [])), produces=sets, cond=cond_summary(u),
             lines=[ko(f"action.{act['Id']}.name", ko(f"action.{act['Id']}", act['Id']))],
             meta={'파일': act['_file'], '분류': act.get('Category', ''), '지역': act.get('LocationId', '')})

# 탐험 조우 (LocationBuilder.cs)
builder = open(os.path.join(PROJECT, 'Scripts', 'Editor', 'LocationBuilder.cs'), encoding='utf-8-sig').read()


def brace_block(text, start):
    depth, i = 0, text.index('{', start)
    for j in range(i, len(text)):
        if text[j] == '{':
            depth += 1
        elif text[j] == '}':
            depth -= 1
            if depth == 0:
                return text[i:j + 1]
    return text[i:]


def str_list(block, name):
    m = re.search(name + r'\s*=\s*\{([^}]*)\}', block)
    return re.findall(r'"([^"]+)"', m.group(1)) if m else []


LOC_OF_FUNC = dict((f, loc) for loc, f in re.findall(r'Save\("(LOC_[A-Z_]+)",\s*(\w+)\(\)\)', builder))
FUNC_STARTS = sorted((m.start(), m.group(1)) for m in re.finditer(r'(?:private|public)\s+static\s+LocationDef\s+(\w+)\s*\(', builder))


def location_at(pos):
    func = None
    for start, name in FUNC_STARTS:
        if start <= pos:
            func = name
    return LOC_OF_FUNC.get(func, '')


ENC_LANE = {'DOHYEON': 'DH', 'NURSE': 'NURSE', 'BYSTANDER': 'BY', 'RINGLEADER': 'HR', 'MART_GROUP': 'MART'}
for m in re.finditer(r'new EncounterDef\b', builder):
    block = brace_block(builder, m.end())
    eid = re.search(r'Id\s*=\s*"([^"]+)"', block).group(1)
    nid = 'ENC_' + eid
    if nid in nodes:
        continue
    head = block.split('Choices', 1)[0]
    choices, produces, enc_clues = [], [], []
    for cm in re.finditer(r'new EncounterChoiceDef\b', block):
        cb = brace_block(block, cm.end())
        cid = re.search(r'Id\s*=\s*"([^"]+)"', cb).group(1)
        text_key = re.search(r'TextKey\s*=\s*"([^"]+)"', cb)
        sets = str_list(cb, 'SetFlags') + [f'ENC_{eid}_{cid}']
        produces += sets
        enc_clues += str_list(cb, 'AddClues')
        choices.append({'id': cid, 'text': ko(text_key.group(1)) if text_key else '', 'flags': sets, 'npc': [], 'cond': [], 'next': ''})
    prompt = re.search(r'PromptKey\s*=\s*"([^"]+)"', head)
    scene = re.search(r'SceneEventId\s*=\s*"([^"]+)"', head)
    node = add_node(nid, kind='encounter', lane=ENC_LANE.get(eid, 'SYS'), requires=str_list(head, 'FlagsAll'), none=str_list(head, 'FlagsNone'),
                    produces=produces, clueSet=enc_clues, choices=choices, lines=[ko(prompt.group(1))] if prompt else [],
                    cond=cond_summary({'FlagsAll': str_list(head, 'FlagsAll'), 'FlagsNone': str_list(head, 'FlagsNone')}))
    node['location'] = location_at(m.start())
    node['meta'] = {'지역': node['location'], '정의': 'Scripts/Editor/LocationBuilder.cs', '그 자리 대화': re.search(r'DialogueNode\s*=\s*"([^"]+)"', head).group(1)
                    if 'DialogueNode' in head else '-', '후속 컷': scene.group(1) if scene else '-',
                    '현장에서 이어 치름': 'PlayEventInField = true' in head}
    if eid in ('RINGLEADER',) or 'PlayEventInField = true' in head:
        pass

# 프롤로그 (질문 답이 켜는 플래그: 혈액형 모름 등)
prologue_flags, prologue_choices = [], []


def walk_prologue(o, path=''):
    if isinstance(o, dict):
        sets = flags_set(o.get('Effect')) + flags_set(o)
        if sets and o.get('Id'):
            prologue_flags.extend(sets)
            prologue_choices.append({'id': o['Id'], 'text': (path + ' ' + str(o.get('ProfileValue', ''))).strip(), 'flags': sets,
                                     'npc': [], 'cond': [], 'next': ''})
        for k, v in o.items():
            walk_prologue(v, o.get('ProfileField', path) if k != 'Effect' else path)
    elif isinstance(o, list):
        for v in o:
            walk_prologue(v, path)


prologue_path = os.path.join(PROJECT, 'Content', 'Story', 'prologue.json')
if os.path.exists(prologue_path):
    walk_prologue(load_json(prologue_path))
if prologue_flags:
    add_node('PROLOGUE', kind='action', lane='A1', week=1, produces=sorted(set(prologue_flags)), choices=prologue_choices,
             lines=['프롤로그: 주인공 정보 질문의 답이 켜는 플래그'], meta={'파일': 'Content/Story/prologue.json'})

# 방 수색 (그 방 상자를 처음 뒤지면 켜지는 플래그)
search_flags = set(re.findall(r'SearchFlag\s*=\s*"([^"]+)"', builder))
search_flags.update(re.findall(r'\{\s*"ROOM_[A-Z_]+"\s*,\s*"(SEARCHED_[A-Z_0-9]+)"\s*\}', builder))
for flag in sorted(search_flags):
    add_node('SEARCH_' + flag, kind='search', lane='SYS', produces=[flag], lines=['탐험에서 그 방 상자를 처음 뒤지면 켜진다'],
             meta={'정의': 'Scripts/Editor/LocationBuilder.cs (문 SearchFlag)'})

# 엔딩 (52주)
for end in endings:
    c = end.get('Condition') or {}
    clue_need = [ch['Key'] + '*' for ch in c.get('Checks', []) if ch.get('Kind') == 'ClueCount']
    add_node(end['Id'], kind='ending', lane='ENDING', week=TOTAL_WEEKS, requires=list(c.get('FlagsAll', [])), any=list(c.get('FlagsAny', [])),
             clueNeed=clue_need, cond=cond_summary(c) + ([f"종료 원인 {', '.join(end['EndCauses'])}"] if end.get('EndCauses') else []),
             lines=[ko(f"ending.{end['Id']}.cut1")], meta={'제목': ko(f"ending.{end['Id']}.title", end['Id']), '분류': end.get('Category', ''),
                                                           '우선순위': end.get('Priority', 0)})

# ── 코드가 켜는 플래그 (문자열 리터럴) ──
code_strings = set()
for path in glob.glob(os.path.join(PROJECT, 'Scripts', '**', '*.cs'), recursive=True):
    if os.sep + 'Tests' + os.sep in path or os.sep + 'Editor' + os.sep in path:
        continue
    code_strings.update(re.findall(r'"([A-Z][A-Z0-9_]{2,})"', open(path, encoding='utf-8-sig').read()))

# ── 연결선 ──
producers, npc_producers, clue_producers = {}, {}, {}
for n in nodes.values():
    for f in set(n['produces']):
        producers.setdefault(f, []).append(n['id'])
    for s in set(n['npcSet']):
        npc_producers.setdefault(s, []).append(n['id'])
    for cl in set(n['clueSet']):
        clue_producers.setdefault(cl, []).append(n['id'])

edges = []
seen = set()


def edge(a, b, kind, label):
    if a == b or a not in nodes or b not in nodes:
        return
    key = (a, b, kind, label)
    if key not in seen:
        seen.add(key)
        edges.append({'from': a, 'to': b, 'kind': kind, 'label': label})


consumed = set()
for n in nodes.values():
    for f in set(n['requires'] + n['any']):
        consumed.add(f)
        for p in producers.get(f, []):
            edge(p, n['id'], 'flag', f)
    for f in set(n['none']):
        consumed.add(f)
        for p in producers.get(f, []):
            edge(p, n['id'], 'block', f)
    for s in set(n['npcNeed']):
        for p in npc_producers.get(s, []):
            edge(p, n['id'], 'npc', s)
    for need in n['clueNeed']:
        prefix = need.rstrip('*')
        for cl, ps in clue_producers.items():
            if (need.endswith('*') and cl.startswith(prefix)) or cl == need:
                for p in ps:
                    edge(p, n['id'], 'clue', cl)
    for t in n['then']:
        edge(n['id'], t, 'then', '연쇄')
    if n['kind'] == 'event':
        ev_cond = next((ev.get('Condition') or {} for ev in events if ev['Id'] == n['id']), {})
        for aid in ev_cond.get('ActionIds', []):
            edge(aid, n['id'], 'act', '그 행동을 할 때')
    if n['kind'] == 'encounter' and n.get('location'):
        for a in actions:
            if a.get('LocationId') == n['location']:
                edge(a['Id'], n['id'], 'act', '그 지역 탐험')

# 조우 → 같은 자리 후속 컷 이벤트(그 자리 대화)
for n in nodes.values():
    if n['kind'] == 'encounter' and n['meta'].get('후속 컷', '-') != '-':
        edge(n['id'], n['meta']['후속 컷'], 'scene', '그 자리 대화 컷')

# ── 주 배치: 조건의 최소 주와, 필요한 플래그를 처음 켤 수 있는 주 중 늦은 쪽 (연쇄·그 자리 컷은 앞 노드와 같은 주 이후) ──
LANE_FLOOR = {}
for lane in ('A2', 'A3', 'A4'):
    weeks = [n['week'] for n in nodes.values() if n['lane'] == lane and n['kind'] == 'event' and n['week'] > 1]
    if weeks:
        LANE_FLOOR[lane] = min(weeks)
chain_in = {}
for e in edges:
    if e['kind'] in ('then', 'scene'):
        chain_in.setdefault(e['to'], []).append(e['from'])
act_in = {}
for e in edges:
    if e['kind'] == 'act':
        act_in.setdefault(e['to'], []).append(e['from'])
x = {nid: max(1, n['week'] or 1) for nid, n in nodes.items()}
for _ in range(80):
    changed = False
    for nid, n in nodes.items():
        if n['kind'] == 'ending':
            continue
        w = max(1, n['week'] or 1)
        for f in n['requires'] + [s.split('=')[0] and s for s in n['npcNeed']]:
            ps = producers.get(f) or npc_producers.get(f) or []
            ps = [p for p in ps if p != nid]
            if ps:
                w = max(w, min(x[p] for p in ps))
        if n['any']:
            ps = [p for f in n['any'] for p in producers.get(f, []) if p != nid]
            if ps and not n['requires']:
                w = max(w, min(x[p] for p in ps))
        for src in chain_in.get(nid, []):
            w = max(w, x[src])
        if act_in.get(nid):
            w = max(w, min(x[a] for a in act_in[nid]))
        w = max(w, LANE_FLOOR.get(n['lane'], 1))
        w = min(TOTAL_WEEKS, w)
        if w != x[nid]:
            x[nid] = w
            changed = True
    if not changed:
        break
for nid, n in nodes.items():
    n['x'] = x[nid]

# ── 점검: 아무도 켜지 않는 필요 플래그, 아무도 쓰지 않는 플래그 ──
orphans = []
for n in nodes.values():
    # FORCED_ONLY·SYSTEM_ONLY: 코드가 직접 띄우는 이벤트·행동에 일부러 거는 표시
    missing = [f for f in n['requires'] if f not in producers and f not in code_strings and f not in ('FORCED_ONLY', 'SYSTEM_ONLY')]
    if missing:
        orphans.append({'id': n['id'], 'flags': missing})
yarn_flags = set()
for path in glob.glob(os.path.join(PROJECT, 'Resources', 'Dialogue', '**', '*.yarn'), recursive=True):
    yarn_flags.update(re.findall(r'flag\(\s*"([A-Z0-9_]+)"\s*\)', open(path, encoding='utf-8-sig').read()))
unused = sorted(f for f in producers if f not in consumed and not f.startswith('ENC_') and f not in code_strings and f not in yarn_flags)
dialogue_only = sorted(f for f in producers if f not in consumed and f in yarn_flags)
code_flags = sorted({f for n in nodes.values() for f in n['requires'] + n['any'] if f not in producers and f in code_strings})

data = {
    'lanes': [{'key': k, 'name': name} for k, name in LANES],
    'nodes': list(nodes.values()),
    'edges': edges,
    'checks': {'orphans': orphans, 'unused': unused, 'codeFlags': code_flags, 'dialogueOnly': dialogue_only},
    'stats': {'events': sum(1 for n in nodes.values() if n['kind'] == 'event'), 'encounters': sum(1 for n in nodes.values() if n['kind'] == 'encounter'),
              'actions': sum(1 for n in nodes.values() if n['kind'] == 'action'), 'endings': sum(1 for n in nodes.values() if n['kind'] == 'ending'),
              'edges': len(edges)},
}

template = open(TEMPLATE, encoding='utf-8').read()
html = template.replace('/*__DATA__*/null', json.dumps(data, ensure_ascii=False))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, 'w', encoding='utf-8') as f:
    f.write(html)
print(f"{OUT}\n노드 {len(nodes)} (이벤트 {data['stats']['events']}, 조우 {data['stats']['encounters']}, 행동 {data['stats']['actions']}, "
      f"엔딩 {data['stats']['endings']}), 연결 {len(edges)}, 출처 없는 조건 {len(orphans)}, 안 쓰는 플래그 {len(unused)}")
