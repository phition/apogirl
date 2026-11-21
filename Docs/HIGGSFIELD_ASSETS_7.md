# 힉스필드 에셋 7차: 인물 뱃지 · 기호품 아이콘 (2026-11-22)

동료 탭(인물 서랍, [PROPOSAL_CD_DEPTH_EXPANSION.md](PROPOSAL_CD_DEPTH_EXPANSION.md) D절)에 필요한데 아직 없는 그림이다.

- 둥근 초상 뱃지 3장: 선배, 유랑자, 고양이
- 기호품 아이콘 16장

- 쓰는 법은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장과 같다. 회색 상자 하나가 생성 하나다.
- 넣는 곳: `ArtInbox/ui/`. 파일 이름은 이 문서 그대로.
- 모델은 `gpt_image_2_6`, 1:1, **투명 배경**.

---

## 1. 둥근 초상 뱃지 (P1, 3장)

이미 있는 `avatar_nurse.png` 등과 같은 틀로 만든다: 어두운 원 바탕, 얇은 황동색 테두리, 머리와 어깨, 정면에 가까운 3/4 각도.
**참고 이미지 두 장**을 함께 넣는다.
- 그 인물의 기준 시트
- 이미 있는 뱃지 하나(`Assets/_Project/Resources/Art/UI/avatar_nurse.png`) — 틀을 맞추는 용도

`ArtInbox/ui/avatar_senior.png` — 참고: `char_senior_sheet.png` + `avatar_nurse.png`
```
Small round portrait badge for a game UI, matching the frame of the second reference image exactly: dark charcoal circular background, thin brass-gold ring border, head and shoulders, slight three-quarter view, calm expression. The person is the 2nd-year high school girl from the first reference image (same face, hair and outfit). Clean line art, soft painterly 2D shading, muted low-saturation palette. Transparent background outside the circle, no text, no logo, no watermark.
```

`ArtInbox/ui/avatar_wanderer.png` — 참고: `char_wanderer_sheet.png` + `avatar_nurse.png`
```
Small round portrait badge for a game UI, matching the frame of the second reference image exactly: dark charcoal circular background, thin brass-gold ring border, head and shoulders, slight three-quarter view. The person is the adult wanderer from the first reference image: worn faded hiking jacket, wide-brim hat shadowing the face so the eyes are hidden, a strap of a large backpack on the shoulder. Clean line art, soft painterly 2D shading, muted low-saturation palette. Transparent background outside the circle, no text, no logo, no watermark.
```

`ArtInbox/ui/avatar_cat.png` — 참고: `avatar_nurse.png` (틀만)
```
Small round portrait badge for a game UI, matching the frame of the reference image exactly: dark charcoal circular background, thin brass-gold ring border. Inside: a scruffy Korean street cat, short-haired cheese-tabby (orange and white), one ear slightly notched, looking at the viewer with half-closed unimpressed eyes, head and chest. Clean line art, soft painterly 2D shading, muted low-saturation palette. Transparent background outside the circle, no text, no logo, no watermark.
```

- 고양이 생김새는 프롤로그 등굣길 영상([HIGGSFIELD_ASSETS_6.md](HIGGSFIELD_ASSETS_6.md))의 골목 고양이와 같아야 한다.
- 영상을 먼저 만들었다면 그 영상에서 고양이가 나온 프레임을 참고 이미지로 더 넣는다.
- 뱃지 테두리 굵기·색이 기존 6장과 다르면 다시 만든다. 나란히 놓고 본다.

---

## 2. 기호품 아이콘 (P2, 16장)

기존 아이템 아이콘(`item_*.png`, 1차 문서 4.4)과 같은 방식이다. 아래 `[F-ITEM]` 뒤에 물건 설명을 붙인다.

`[F-ITEM]`
```
flat game UI item icon, three-quarter view, muted earthy palette with dark charcoal #1d1f22, brass accent #b9a37a and paper white #e4e1da, simple bold silhouette, thin dark outline, slightly worn and used, readable at 32 pixels, centered, transparent background, no text, no logo, no brand name, no watermark
```

| 파일 | 누구의 것 | 물건 설명 (`[F-ITEM]` 뒤에 붙임) |
|---|---|---|
| `item_paperback.png` | 윤 선생님 | `a dog-eared paperback novel with a plain faded cover` |
| `item_pen.png` | 윤 선생님 | `a red ballpoint pen with a chewed cap` |
| `item_coffee_mix.png` | 박 선생님 | `three Korean instant coffee mix stick packets, plain yellow wrappers` |
| `item_hand_cream.png` | 박 선생님 | `a small squeezed tube of hand cream` |
| `item_comic.png` | 도현 | `a thick worn comic book volume with a blank cover` |
| `item_sports_drink.png` | 도현 | `a plastic bottle of blue isotonic sports drink with a blank label` |
| `item_keyring.png` | 송예지 | `a cute small plush animal keyring on a metal ring` |
| `item_stickers.png` | 송예지 | `a small sheet of cute stickers, a few already peeled off` |
| `item_hair_tie.png` | 최하린 | `a black hair tie and two simple hair pins` |
| `item_lip_balm.png` | 최하린 | `a small stick of lip balm with the cap off` |
| `item_guitar_score.png` | 선배 | `a few folded pages of handwritten guitar sheet music with chord boxes, no readable text` |
| `item_guitar_strings.png` | 선배 | `a coiled pack of acoustic guitar strings` |
| `item_old_map.png` | 유랑자 | `a creased, folded paper city map with pencil marks` |
| `item_candy.png` | 유랑자 | `a few hard candies in clear twisted wrappers` |
| `item_tuna_can.png` | 고양이 | `a small round tuna can with a pull tab, plain label` |
| `item_cat_treat.png` | 고양이 | `a few squeeze-tube cat treat sticks, plain wrappers` |

- 상표·글자가 생기면 다시 만든다(특히 커피 스틱·이온음료).
- 아이콘은 받는 대로 `Resources/Art/UI/`로 옮기고, 아이템 정의(`items_loot.json`)와 전리품 표에 연결한다.

---

## 3. 체크리스트

| 우선 | 파일 | 개수 |
|---|---|---|
| P1 | `avatar_senior`, `avatar_wanderer`, `avatar_cat` | 3 |
| P2 | 기호품 아이콘 `item_*` | 16 |
