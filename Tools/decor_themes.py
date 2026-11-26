"""탐험 방 장식 다시 꾸미기 (2026-11-26): exploration_art.json의 Decor(방별 분위기 묶음 Themes)와 Interior(벽지 색조 Tints)를 고친다.
Assets/_Project에서 실행. 여러 번 돌려도 같은 결과 (방 규칙을 통째로 다시 쓴다).
"""
import io
import json
import os

P = 'Resources/Art/exploration_art.json'
PIX = 'Resources/Art/ExplorationPixel/'


def d(*names):
    keys = ['decor_' + n for n in names]
    for k in keys:
        assert os.path.exists(PIX + k + '.png'), k
    return keys


def theme(name, floor, wall, fd=0, wd=0):
    t = {'Name': name, 'Floor': d(*floor.split()), 'Wall': d(*wall.split())}
    if fd:
        t['FloorDensity'] = fd
    if wd:
        t['WallDensity'] = wd
    return t


def rule(loc, template, floor, wall, fd, wd, themes=None):
    r = {'Location': loc, 'Template': template, 'Floor': d(*floor.split()), 'Wall': d(*wall.split()), 'FloorDensity': fd, 'WallDensity': wd}
    if themes:
        r['Themes'] = themes
    return r


# 방 규칙 (지역의 "*" 줄보다 앞에 온다: 맞는 첫 줄을 쓴다)
ROOMS = [
    # ── 학교 ──
    rule('LOC_SCHOOL', 'CLASSROOM', 'books bag chair_fallen papers bucket', 'motto timetable clock worldmap speaker calendar', 0.2, 0.16, [
        theme('수업하던 교실', 'books bag pencilcase notebook chalk papers chair_fallen bookshelf', 'timetable motto classboard clock'),
        theme('체육 시간 뒤', 'ball basketball shuttle bag slippers whistle bucket', 'clock speaker calendar poster_event'),
        theme('며칠 버틴 교실', 'sleepingbag blanket candle canned bottle lantern suitcase', 'calendar clock poster_safety cobweb'),
        theme('청소하다 만 교실', 'broom bucket pail trashbin wetfloor chair_fallen', 'timetable worldmap speaker classboard'),
        theme('오래 비운 교실', 'debris papers plant_dead glass bookshelf books chair_fallen', 'cobweb worldmap motto classboard', 0.24),
    ]),
    rule('LOC_SCHOOL', 'ROOM_OFFICE', 'papers plant trashbin stool watercooler newspapers fan', 'staffboard keycabinet calendar clock frame', 0.2, 0.16, [
        theme('일하던 교무실', 'papers plant stool watercooler laptop fan newspapers', 'staffboard keycabinet calendar clock'),
        theme('급히 떠난 교무실', 'papers chair_fallen handbag umbrella trashbin debris keys', 'staffboard frame clock cobweb'),
    ]),
    rule('LOC_SCHOOL', 'ROOM_NURSE', 'stool firstaid_floor bandage pillbottle mask trashbin tissue', 'eyechart healthboard medposter clock', 0.2, 0.18),
    rule('LOC_SCHOOL', 'ROOM_SCIENCE', 'stool bucket extinguisher papers glass glove', 'safetyboard schematic clock warning', 0.18, 0.16),
    rule('LOC_SCHOOL', 'ROOM_MUSIC', 'guitar stool papers books', 'composers clock speaker', 0.18, 0.16),
    rule('LOC_SCHOOL', 'ROOM_HOMEEC', 'pot kettle ricecooker bowl trays stool bucket', 'utensilrack wallshelf clock calendar', 0.2, 0.16),
    rule('LOC_SCHOOL', 'ROOM_COMPUTER', 'laptop stool papers trashbin fan', 'netcabinet socket aircon clock', 0.18, 0.16),
    rule('LOC_SCHOOL', 'ROOM_BROADCAST', 'radio stool papers battery tape', 'speaker clock socket poster_event', 0.18, 0.16),
    rule('LOC_SCHOOL', 'RESTROOM', 'bucket wetfloor trashbin tissue pail broom', 'mirror vent switch', 0.18, 0.18),
    rule('LOC_SCHOOL', '*', 'books stool papers trashbin plant_dead chair_fallen lantern', 'clock calendar poster_a speaker frame switch poster_safety', 0.16, 0.14),
    # ── 튜토리얼 (체육관 창고) ──
    rule('LOC_TUTORIAL', '*', 'vaultbox ball cone bucket basketball shuttle', 'clock poster_event hooks_pe', 0.16, 0.12),
    # ── 편의점 ──
    rule('LOC_STORE', '*', 'crates bottlecrate bucket trashbin water', 'calendar elecbox switch', 0.18, 0.14, [
        theme('점장 사무실', 'stool papers fan register trashbin newspapers', 'calendar staffboard keycabinet clock'),
        theme('뒤 창고', 'crates bottlecrate ramenbox canned water broom pail', 'elecbox switch calendar cobweb'),
    ]),
    # ── 약국 ──
    rule('LOC_PHARMACY', '*', 'exambed stool trashbin papers plant', 'firstaid clock calendar frame', 0.18, 0.15, [
        theme('진료실', 'exambed stool plant tissue mask trashbin', 'eyechart medposter firstaid clock'),
        theme('조제실', 'pillbottle bandage firstaid_floor trashbin papers stool', 'healthboard calendar frame firstaid'),
        theme('뒤 창고', 'crates pillbottle papers bucket broom', 'safetyboard calendar switch cobweb'),
    ]),
    # ── 주택가 ──
    rule('LOC_RESIDENTIAL', '*', 'sofa tvstand diningtable teddy vase smalltable plant lantern', 'frame calendar clock mirror aircon', 0.22, 0.16, [
        theme('살림집 거실', 'sofa tvstand smalltable plant vase slippers fan', 'frame clock wallshelf aircon'),
        theme('식구 많은 집', 'diningtable teddy ricecooker kettle pot bowl basket slippers', 'calendar coathanger frame clock'),
        theme('떠날 채비를 하던 집', 'suitcase handbag bag blanket newspapers shoes wallet keys umbrella', 'coathanger calendar mirror'),
        theme('숨어 버티던 집', 'sleepingbag candle canned bottle ramenbox lantern radio water bucket', 'calendar cobweb clock'),
        theme('오래 비운 집', 'plant_dead debris glass papers chair_fallen smalltable photo', 'cobweb frame mirror socket'),
    ]),
    # ── 지하철역 ──
    rule('LOC_SUBWAY', '*', 'stool bucket newspapers trashbin', 'clock elecbox calendar phone', 0.18, 0.15, [
        theme('역무실', 'stool newspapers trashbin fan radio helmet papers', 'clock staffboard keycabinet phone schematic'),
        theme('피난민이 머문 방', 'sleepingbag blanket suitcase bottle canned candle bag', 'cobweb phone calendar'),
        theme('청소 도구실', 'bucket pail broom wetfloor trashbin tissue', 'elecbox switch vent'),
    ]),
    # ── 대형마트 ──
    rule('LOC_MART', '*', 'crates toolbox gascan rope bottlecrate stool', 'elecbox warning calendar switch', 0.18, 0.14, [
        theme('물류 창고', 'crates toolbox gascan rope bottlecrate ramenbox canned pail helmet', 'elecbox warning safetyboard switch'),
        theme('관리 사무실', 'stool papers fan register trashbin plant', 'calendar staffboard keycabinet clock'),
        theme('직원 휴게 공간', 'smalltable kettle ramenbox slippers newspapers bag', 'calendar clock coathanger'),
    ]),
    # ── 파출소 ──
    rule('LOC_POLICE', '*', 'stool papers toolbox trashbin', 'calendar frame police_board switch', 0.18, 0.15, [
        theme('근무하던 사무실', 'stool papers newspapers fan watercooler trashbin', 'police_board staffboard keycabinet calendar'),
        theme('대기실', 'blanket bottle slippers helmet flashlight', 'clock cctv warning'),
        theme('뒤진 흔적', 'papers debris chair_fallen glass toolbox', 'frame cobweb switch police_board'),
    ]),
    # ── 정수장 ──
    rule('LOC_WATERWORKS', '*', 'stool toolbox gascan bucket', 'warning clock elecbox calendar', 0.18, 0.15, [
        theme('수질 실험실', 'stool bucket pail glove bottle papers', 'warning schematic safetyboard clock'),
        theme('정비실', 'toolbox gascan rope helmet tape spraycan valve', 'elecbox schematic netcabinet calendar'),
        theme('숙직하던 방', 'sleepingbag kettle canned radio slippers', 'calendar clock coathanger'),
    ]),
]

# 복도에 더하는 장식 (지역 복도 줄의 목록 끝에 붙인다, 이미 있으면 건너뜀)
CORRIDOR_ADD = {
    'LOC_SCHOOL': ('broom slippers', 'safetyboard'),
    'LOC_RESIDENTIAL': ('basket slippers', 'cobweb'),
    'LOC_SUBWAY': ('suitcase', 'cobweb'),
    'LOC_MART': ('ramenbox', 'safetyboard'),
    'LOC_POLICE': ('helmet', 'staffboard'),
    'LOC_WATERWORKS': ('helmet', 'schematic'),
    'LOC_PHARMACY': ('tissue', 'medposter'),
    'LOC_STORE': ('ramenbox', ''),
}

# 벽지 색조: 방마다 하나 (그림에 곱하는 색, 흰색 = 그대로)
TINTS = {
    'ROOM_HOUSE': ['#FFFFFF', '#CFE4CC', '#F2D2C6', '#C8D6EC', '#E8DCA8'],
    'ROOM_MART_OFFICE': ['#FFFFFF', '#DCE4C8'],
    'ROOM_MART_WAREHOUSE': ['#FFFFFF', '#DCD6C8'],
    'ROOM_POLICE': ['#FFFFFF', '#D0DCE6', '#E6DCC4'],
    'ROOM_WATER': ['#FFFFFF', '#CCE2DE', '#E6E0C8'],
    'ROOM_SUBWAY': ['#FFFFFF', '#DEE2C8', '#CCD8E4'],
}
# 표에 줄이 없어 코드 기본 벽지를 쓰는 방: 줄을 새로 만든다 (그림은 같은 것)
NEW_INTERIOR = [
    {'Location': 'LOC_SCHOOL', 'Template': 'CLASSROOM', 'Key': 'wall_classroom', 'Tints': ['#FFFFFF', '#DCE6CC', '#F0D8C8', '#D0DCE8', '#ECE2B4']},
    {'Location': 'LOC_PHARMACY', 'Template': 'ROOM_PH_', 'Key': 'wall_pharmacy', 'Tints': ['#FFFFFF', '#CEE4DA', '#EADFC8']},
]


def main():
    raw = io.open(P, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in raw else '\n'
    data = json.loads(raw)

    def is_room(r):
        return r['Template'] not in ('', 'DEEP', 'OUT_ENTRY', 'OUT_ESCAPE')

    kept = [r for r in data['Decor'] if not is_room(r)]
    for r in kept:
        add = CORRIDOR_ADD.get(r['Location']) if r['Template'] == '' else None
        if add:
            for k in d(*add[0].split()):
                if k not in r['Floor']:
                    r['Floor'].append(k)
            for k in d(*add[1].split()):
                if k not in r['Wall']:
                    r['Wall'].append(k)
    # 차례: 깊은 곳·바깥 길 → 방 규칙 → 복도
    special = [r for r in kept if r['Template'] != '']
    corridors = [r for r in kept if r['Template'] == '']
    data['Decor'] = special + ROOMS + corridors

    interior = [r for r in data['Interior'] if not any(r['Location'] == n['Location'] and r['Template'] == n['Template'] for n in NEW_INTERIOR)]
    for r in interior:
        for part, tints in TINTS.items():
            if r['Template'] == part:
                r['Tints'] = tints
    for n in NEW_INTERIOR:
        assert os.path.exists(PIX + n['Key'] + '.png'), n['Key']
    data['Interior'] = interior + NEW_INTERIOR

    out = json.dumps(data, ensure_ascii=False, indent=1).replace('\n', nl)
    io.open(P, 'w', encoding='utf-8', newline='').write(out + (nl if raw.endswith('\n') else ''))
    print('rooms', len(ROOMS), 'themes', sum(len(r.get('Themes', [])) for r in ROOMS))


if __name__ == '__main__':
    main()
