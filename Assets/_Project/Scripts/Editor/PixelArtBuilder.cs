using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 탐험 화면 픽셀 아트 생성기 (메뉴 ApoGirl/Art/Build Exploration Sprites).
    /// 캐릭터·좀비·벽·소품은 코드로 그리고, 문·창문·책상 등은 CC0 타일셋(Art/ThirdParty/OfficeSpace)에서 잘라 어두운 톤으로 낮춘다.
    /// 결과: Resources/Art/Exploration/{키}.png (PPU 18, Point 필터). 런타임은 ExplorationArt.Get(키)로 읽는다.
    /// 정식 아트가 생기면 같은 키의 PNG로 바꾸기만 하면 된다.
    /// </summary>
    public static class PixelArtBuilder
    {
        public const int Ppu = 18;
        private const string OutFolder = "Assets/_Project/Resources/Art/Exploration";
        private const string TilesetPath = "Assets/_Project/Art/ThirdParty/OfficeSpace/office-space-tileset.png";

        // ── 팔레트 (문서 2장: 낮은 채도, 어두운 톤) ──
        private static readonly Color Clear = new Color(0, 0, 0, 0);
        private static readonly Color Outline = C(22, 20, 24);
        private static readonly Color Skin = C(222, 188, 160);
        private static readonly Color SkinShade = C(188, 150, 124);
        private static readonly Color Hair = C(34, 28, 32);
        private static readonly Color HairHi = C(58, 48, 54);
        private static readonly Color Blazer = C(44, 52, 78);
        private static readonly Color BlazerShade = C(32, 38, 60);
        private static readonly Color Shirt = C(214, 214, 206);
        private static readonly Color RibbonCol = C(148, 42, 50);
        private static readonly Color SkirtCol = C(56, 54, 68);
        private static readonly Color SkirtLine = C(78, 66, 80);
        private static readonly Color Sock = C(206, 204, 198);
        private static readonly Color Shoe = C(26, 24, 28);
        private static readonly Color Bag = C(96, 66, 50);
        private static readonly Color BagShade = C(72, 50, 38);

        private static readonly Color ZSkin = C(136, 152, 122);
        private static readonly Color ZSkinShade = C(108, 122, 98);
        private static readonly Color ZEye = C(28, 30, 26);

        [MenuItem("ApoGirl/Art/Build Exploration Sprites")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutFolder);
            var count = 0;
            count += BuildPlayer();
            count += BuildZombies();
            count += BuildSurvivor();
            count += BuildPeople();
            count += BuildProps();
            count += BuildSchoolRooms();
            count += BuildWalls();
            count += BuildThirdParty();
            AssetDatabase.Refresh();
            Debug.Log($"[Art] 탐험 스프라이트 {count}개 생성: {OutFolder}");
        }

        // ── 캐릭터 ──

        private sealed class Figure
        {
            public Color Top = Blazer, TopShade = BlazerShade, Bottom = SkirtCol, BottomLine = SkirtLine;
            public Color SkinC = Skin, SkinS = SkinShade, HairC = Hair, Leg = Sock, Feet = Shoe;
            public bool Skirt = true, Backpack = true, Collar = true, Zombie, LongHair, Hood;
            public Color Eye = Outline;
            public Color? Ribbon = RibbonCol;
        }

        /// <summary>
        /// 16×32 사람 한 프레임 (오른쪽을 본다). legPhase: -2~2 앞다리 오프셋, armSwing: 앞팔 오프셋, hunch: 좀비 앞굽음, reach: 팔 앞으로 뻗기.
        /// </summary>
        private static Texture2D DrawPerson(Figure f, int legPhase, int armSwing, int bob, int hunch, bool reach, bool headDown = false)
        {
            var t = New(16, 32);
            var hipY = 9 + bob;
            var top = 21 + bob;

            // 다리 (발 위치를 위상으로 벌린다)
            Leg(t, f, 7, hipY, 7 - legPhase, true);
            Leg(t, f, 8, hipY, 8 + legPhase, false);

            // 하의
            if (f.Skirt)
            {
                for (var y = hipY; y <= hipY + 4; y++)
                {
                    var w = y == hipY ? 1 : 0;
                    for (var x = 4 - w; x <= 11 + w; x++)
                    {
                        t.SetPixel(x, y, (x + y) % 3 == 0 ? f.BottomLine : f.Bottom);
                    }
                }
            }
            else
            {
                for (var y = hipY + 1; y <= hipY + 4; y++)
                {
                    for (var x = 5; x <= 10; x++) t.SetPixel(x, y, f.Bottom);
                }
            }

            // 몸통
            var shift = hunch;
            for (var y = hipY + 5; y <= top; y++)
            {
                var lean = (y - (hipY + 5)) * shift / 8;
                for (var x = 5 + lean; x <= 10 + lean; x++)
                {
                    var c = x <= 6 + lean ? f.TopShade : f.Top;
                    if (f.Zombie && (x * 7 + y * 3) % 11 == 0) c = f.TopShade; // 해진 옷
                    t.SetPixel(x, y, c);
                }
            }

            var headX = 5 + shift + (headDown ? 1 : 0);
            var headY = top + 1 - (headDown ? 2 : 0) - (f.Zombie ? 1 : 0);

            if (f.Collar)
            {
                t.SetPixel(8 + shift, top, Shirt);
                t.SetPixel(9 + shift, top, Shirt);
                t.SetPixel(9 + shift, top - 1, Shirt);
                if (f.Ribbon.HasValue)
                {
                    t.SetPixel(9 + shift, top - 2, f.Ribbon.Value);
                    t.SetPixel(10 + shift, top - 2, f.Ribbon.Value);
                }
            }

            // 가방 (등쪽 = 왼쪽)
            if (f.Backpack)
            {
                for (var y = hipY + 6; y <= top - 1; y++)
                {
                    for (var x = 2; x <= 4; x++) t.SetPixel(x, y, x == 2 ? BagShade : Bag);
                }
            }

            // 팔
            if (reach)
            {
                for (var x = 10 + shift; x <= 15; x++)
                {
                    t.SetPixel(x, top - 2, f.TopShade);
                    t.SetPixel(x, top - 3, f.Top);
                }

                t.SetPixel(15, top - 2, f.SkinC);
                t.SetPixel(15, top - 3, f.SkinS);
            }
            else
            {
                // 뒷팔
                for (var y = top - 7 - armSwing / 2; y <= top - 1; y++) t.SetPixel(5 + shift + (y < top - 4 ? -armSwing / 2 : 0), y, f.TopShade);
                // 앞팔 + 손
                for (var y = top - 7 + armSwing / 2; y <= top - 1; y++) t.SetPixel(10 + shift + (y < top - 4 ? armSwing / 2 : 0), y, f.Top);
                t.SetPixel(10 + shift + armSwing / 2, top - 8 + armSwing / 2, f.SkinC);
            }

            // 목·머리
            t.SetPixel(8 + shift, headY - 1, f.SkinS);
            for (var y = headY; y <= headY + 6; y++)
            {
                for (var x = headX + 1; x <= headX + 6; x++) t.SetPixel(x, y, f.SkinC);
            }

            // 머리카락
            for (var x = headX; x <= headX + 6; x++)
            {
                t.SetPixel(x, headY + 7, f.HairC);
                t.SetPixel(x, headY + 6, f.HairC);
            }

            for (var y = headY + (f.LongHair ? -3 : 1); y <= headY + 6; y++)
            {
                t.SetPixel(headX, y, f.HairC);
                t.SetPixel(headX + 1, y, f.HairC);
                if (y > headY + 2) t.SetPixel(headX + 2, y, f.HairC);
            }

            if (f.Hood)
            {
                for (var y = headY - 1; y <= headY + 7; y++) t.SetPixel(headX, y, f.TopShade);
                for (var x = headX; x <= headX + 5; x++) t.SetPixel(x, headY + 7, f.TopShade);
            }

            t.SetPixel(headX + 2, headY + 7, f.Zombie ? f.HairC : HairHi);
            // 눈 (앞쪽)
            t.SetPixel(headX + 5, headY + 3, f.Eye);
            if (f.Zombie) t.SetPixel(headX + 4, headY + 3, f.Eye);
            // 볼 그림자
            t.SetPixel(headX + 3, headY, f.SkinS);

            OutlinePass(t);
            t.Apply();
            return t;
        }

        private static void Leg(Texture2D t, Figure f, int hipX, int hipY, int footX, bool back)
        {
            var color = back ? Darken(f.Leg, 0.82f) : f.Leg;
            for (var y = 2; y < hipY; y++)
            {
                var k = (float)(y - 2) / Math.Max(1, hipY - 3);
                var x = Mathf.RoundToInt(Mathf.Lerp(footX, hipX, k));
                t.SetPixel(x, y, f.Skirt && y > 6 ? Darken(f.SkinC, back ? 0.85f : 1f) : color);
            }

            var foot = back ? Darken(f.Feet, 0.8f) : f.Feet;
            t.SetPixel(footX, 1, foot);
            t.SetPixel(footX + 1, 1, foot);
            t.SetPixel(footX, 0, foot);
            t.SetPixel(footX + 1, 0, foot);
        }

        private static int BuildPlayer()
        {
            var f = new Figure();
            var n = 0;
            Save("player_idle", DrawPerson(f, 0, 0, 0, 0, false), 0.5f, 0f); n++;
            var phases = new[] { 2, 0, -2, 0 };
            var bobs = new[] { 0, 1, 0, 1 };
            for (var i = 0; i < 4; i++)
            {
                Save("player_walk_" + i, DrawPerson(f, phases[i], -phases[i], bobs[i], 0, false), 0.5f, 0f); n++;
                Save("player_run_" + i, DrawPerson(f, phases[i] + Math.Sign(phases[i]), -phases[i] * 2, bobs[i], 1, false), 0.5f, 0f); n++;
            }

            Save("player_attack", DrawPerson(f, 1, 0, 0, 1, true), 0.5f, 0f); n++;
            Save("player_crouch", Crouch(f), 0.5f, 0f); n++;
            return n;
        }

        private static Texture2D Crouch(Figure f)
        {
            var full = DrawPerson(f, 1, 0, 0, 1, false);
            // 위 몸을 아래로 눌러 붙인 16×24 프레임 (다리를 접는다)
            var t = New(16, 24);
            for (var y = 0; y < 24; y++)
            {
                var src = y < 4 ? y : y + 8;
                for (var x = 0; x < 16; x++)
                {
                    t.SetPixel(x, y, src < 32 ? full.GetPixel(x, src) : Clear);
                }
            }

            // 접힌 무릎
            for (var x = 8; x <= 11; x++) t.SetPixel(x, 4, f.Bottom);
            OutlinePass(t);
            t.Apply();
            return t;
        }

        private static int BuildZombies()
        {
            var n = 0;
            var kinds = new Dictionary<string, Figure>
            {
                ["wanderer"] = new Figure
                {
                    Zombie = true, Skirt = false, Backpack = false, Collar = true, Ribbon = null,
                    Top = C(72, 78, 74), TopShade = C(54, 58, 56), Bottom = C(50, 52, 58), Leg = C(50, 52, 58), Feet = C(34, 32, 30),
                    SkinC = ZSkin, SkinS = ZSkinShade, HairC = C(40, 36, 30), Eye = ZEye,
                },
                ["listener"] = new Figure
                {
                    Zombie = true, Skirt = false, Backpack = false, Collar = false, Ribbon = null, Hood = true,
                    Top = C(84, 70, 58), TopShade = C(64, 52, 44), Bottom = C(44, 46, 54), Leg = C(44, 46, 54), Feet = C(30, 28, 26),
                    SkinC = C(146, 150, 128), SkinS = C(116, 120, 102), HairC = C(30, 28, 26), Eye = ZEye,
                },
                ["watcher"] = new Figure
                {
                    Zombie = true, Skirt = false, Backpack = false, Collar = false, Ribbon = null, LongHair = true,
                    Top = C(196, 196, 190), TopShade = C(150, 150, 146), Bottom = C(70, 72, 80), Leg = C(70, 72, 80), Feet = C(40, 38, 36),
                    SkinC = C(150, 156, 140), SkinS = C(120, 126, 112), HairC = C(28, 26, 26), Eye = C(214, 222, 170),
                },
            };

            foreach (var pair in kinds)
            {
                var f = pair.Value;
                Save($"zombie_{pair.Key}_dormant_0", DrawPerson(f, 0, 0, 0, 1, false, true), 0.5f, 0f);
                Save($"zombie_{pair.Key}_dormant_1", DrawPerson(f, 0, 1, 0, 2, false, true), 0.5f, 0f);
                Save($"zombie_{pair.Key}_walk_0", DrawPerson(f, 1, 1, 0, 2, false), 0.5f, 0f);
                Save($"zombie_{pair.Key}_walk_1", DrawPerson(f, -1, -1, 0, 2, false), 0.5f, 0f);
                Save($"zombie_{pair.Key}_chase_0", DrawPerson(f, 2, 0, 0, 3, true), 0.5f, 0f);
                Save($"zombie_{pair.Key}_chase_1", DrawPerson(f, -2, 0, 1, 3, true), 0.5f, 0f);
                Save($"zombie_{pair.Key}_stun", DrawPerson(f, 0, 0, 0, -1, false, true), 0.5f, 0f);
                n += 7;
            }

            return n;
        }

        /// <summary>적대적 생존자 (어른): 후드 달린 작업 점퍼, 작업 바지, 큰 배낭. 교복이 아니라 주인공과 한눈에 구분된다.</summary>
        private static int BuildSurvivor()
        {
            var f = new Figure
            {
                Skirt = false, Backpack = true, Collar = false, Ribbon = null, Hood = true,
                Top = C(78, 84, 60), TopShade = C(58, 62, 44), Bottom = C(58, 54, 48), Leg = C(58, 54, 48), Feet = C(44, 32, 24),
                SkinC = C(204, 168, 138), SkinS = C(170, 136, 110), HairC = C(36, 30, 26),
            };
            var n = 0;
            int[] phases = { 1, 0, -1, 0 };
            int[] bobs = { 0, 1, 0, 1 };
            Save("survivor_idle", DrawPerson(f, 0, 0, 0, 0, false), 0.5f, 0f); n++;
            for (var i = 0; i < 4; i++)
            {
                Save("survivor_walk_" + i, DrawPerson(f, phases[i], -phases[i], bobs[i], 0, false), 0.5f, 0f); n++;
                Save("survivor_run_" + i, DrawPerson(f, phases[i] + Math.Sign(phases[i]), -phases[i] * 2, bobs[i], 1, true), 0.5f, 0f); n++;
            }

            Save("survivor_stun", DrawPerson(f, 0, 0, 0, -1, false, true), 0.5f, 0f); n++;
            return n;
        }

        private static int BuildPeople()
        {
            // 조우: 웅크린 중학생 (도현), 쪽지·서류
            var boy = new Figure
            {
                Skirt = false, Backpack = false, Ribbon = null,
                Top = C(58, 88, 76), TopShade = C(44, 66, 58), Bottom = C(46, 48, 56), Leg = C(46, 48, 56), HairC = C(30, 26, 24),
            };
            Save("person_student_crouch", Crouch(boy), 0.5f, 0f);

            var paper = New(10, 8);
            Fill(paper, 1, 0, 8, 6, C(214, 208, 190));
            for (var y = 1; y <= 5; y += 2) HLine(paper, 2, 7, y, C(120, 116, 104));
            OutlinePass(paper);
            paper.Apply();
            Save("prop_paper", paper, 0.5f, 0f);

            // CD 케이스 (기획 A): 투명한 케이스 안의 은빛 원판과 손글씨 스티커
            var cd = New(10, 10);
            Fill(cd, 0, 0, 9, 9, C(70, 76, 92));
            Fill(cd, 1, 1, 8, 8, C(178, 186, 198));
            Fill(cd, 3, 3, 6, 6, C(214, 220, 232));
            Fill(cd, 4, 4, 5, 5, C(90, 96, 110));
            HLine(cd, 1, 4, 8, C(232, 190, 92));
            OutlinePass(cd);
            cd.Apply();
            Save("prop_cd", cd, 0.5f, 0f);

            // 창 너머 실루엣: 머리·어깨 (런타임에 어두운 색을 곱한다)
            var sil = New(14, 18);
            Fill(sil, 1, 0, 12, 7, Color.white);
            Fill(sil, 3, 8, 10, 9, Color.white);
            Fill(sil, 4, 10, 10, 16, Color.white);
            Fill(sil, 5, 17, 9, 17, Color.white);
            sil.Apply();
            Save("silhouette", sil, 0.5f, 0f);
            return 3;
        }

        // ── 소품 ──

        private static int BuildProps()
        {
            var n = 0;

            // 사물함 (철제, 2칸)
            var locker = New(16, 34);
            Fill(locker, 0, 0, 15, 33, C(88, 98, 104));
            for (var x = 0; x <= 15; x += 8)
            {
                Fill(locker, x, 1, x + 6, 32, C(104, 116, 122));
                for (var y = 26; y <= 30; y += 2) HLine(locker, x + 2, x + 4, y, C(64, 72, 78)); // 환기구
                locker.SetPixel(x + 5, 16, C(40, 40, 44)); // 손잡이
            }

            Save("prop_locker", Finish(locker), 0.5f, 0f); n++;

            // 학교 책상 + 의자
            var desk = New(24, 16);
            Fill(desk, 2, 9, 15, 10, C(150, 112, 70)); // 상판
            Fill(desk, 3, 6, 14, 8, C(72, 74, 80)); // 서랍
            VLine(desk, 3, 0, 8, C(60, 62, 66));
            VLine(desk, 14, 0, 8, C(60, 62, 66));
            Fill(desk, 17, 6, 22, 7, C(140, 104, 64)); // 의자 좌판
            VLine(desk, 22, 6, 13, C(60, 62, 66));
            Fill(desk, 21, 10, 23, 13, C(140, 104, 64)); // 등받이
            VLine(desk, 17, 0, 5, C(60, 62, 66));
            VLine(desk, 22, 0, 5, C(60, 62, 66));
            Save("prop_desk", Finish(desk), 0.5f, 0f); n++;

            // 청소함·캐비닛
            var cabinet = New(18, 26);
            Fill(cabinet, 0, 0, 17, 25, C(120, 112, 96));
            Fill(cabinet, 1, 1, 8, 24, C(136, 126, 108));
            Fill(cabinet, 9, 1, 16, 24, C(130, 120, 102));
            cabinet.SetPixel(7, 12, C(50, 46, 40));
            cabinet.SetPixel(10, 12, C(50, 46, 40));
            Save("prop_cabinet", Finish(cabinet), 0.5f, 0f); n++;

            // 냉장고
            var fridge = New(16, 32);
            Fill(fridge, 0, 0, 15, 31, C(178, 182, 184));
            HLine(fridge, 0, 15, 20, C(120, 124, 126));
            VLine(fridge, 13, 22, 28, C(90, 92, 94));
            VLine(fridge, 13, 10, 17, C(90, 92, 94));
            Save("prop_fridge", Finish(fridge), 0.5f, 0f); n++;

            // 종이 상자
            var box = New(16, 12);
            Fill(box, 0, 0, 15, 11, C(150, 116, 74));
            HLine(box, 0, 15, 8, C(122, 92, 58));
            VLine(box, 7, 8, 11, C(196, 176, 130)); // 테이프
            Save("prop_box", Finish(box), 0.5f, 0f); n++;

            // 편의점 진열대
            var shelf = New(20, 30);
            Fill(shelf, 0, 0, 19, 29, C(170, 174, 176));
            for (var y = 2; y <= 26; y += 8)
            {
                HLine(shelf, 1, 18, y, C(110, 114, 118));
                for (var x = 2; x <= 17; x += 3)
                {
                    var c = (x * 13 + y * 7) % 4 == 0 ? C(176, 70, 60) : (x + y) % 3 == 0 ? C(220, 190, 70) : C(80, 120, 160);
                    Fill(shelf, x, y + 1, x + 1, y + 4 + (x % 2), c);
                }
            }

            Save("prop_shelf", Finish(shelf), 0.5f, 0f); n++;

            // 약장 (유리문)
            var med = New(18, 30);
            Fill(med, 0, 0, 17, 29, C(196, 196, 190));
            Fill(med, 2, 12, 15, 27, C(150, 176, 186));
            for (var y = 14; y <= 26; y += 4)
            {
                for (var x = 3; x <= 14; x += 2) med.SetPixel(x, y, (x + y) % 3 == 0 ? C(220, 220, 214) : C(200, 120, 110));
            }

            Fill(med, 2, 2, 15, 10, C(176, 176, 170));
            Save("prop_medcabinet", Finish(med), 0.5f, 0f); n++;

            // 칠판 (교실 안)
            var board = New(110, 30);
            Fill(board, 0, 0, 109, 29, C(96, 78, 52));
            Fill(board, 2, 4, 107, 27, C(40, 70, 56));
            for (var i = 0; i < 18; i++)
            {
                var x = 10 + i * 5;
                board.SetPixel(x, 18 + i % 4, C(150, 170, 156));
                board.SetPixel(x + 1, 18 + i % 4, C(150, 170, 156));
            }

            HLine(board, 0, 109, 2, C(120, 100, 70)); // 분필 받침
            Save("prop_chalkboard", Finish(board), 0f, 0f); n++;

            // 계단 (올라가는 계단·내려가는 계단)
            Save("prop_stairs_up", Stairs(true), 0.5f, 0f); n++;
            Save("prop_stairs_down", Stairs(false), 0.5f, 0f); n++;

            // 출구 문 (유리 양문 + 초록 비상구 표지)
            var exit = New(28, 46);
            Fill(exit, 0, 0, 27, 41, C(70, 76, 80));
            Fill(exit, 2, 0, 12, 39, C(110, 136, 140));
            Fill(exit, 15, 0, 25, 39, C(110, 136, 140));
            VLine(exit, 13, 0, 39, C(50, 54, 58));
            VLine(exit, 14, 0, 39, C(50, 54, 58));
            Fill(exit, 8, 42, 19, 45, C(60, 150, 80));
            HLine(exit, 10, 17, 43, C(220, 240, 220));
            Save("prop_exit", Finish(exit), 0.5f, 0f); n++;

            var exitLocked = New(28, 46);
            Fill(exitLocked, 0, 0, 27, 41, C(58, 58, 62));
            Fill(exitLocked, 2, 0, 25, 39, C(78, 80, 84));
            for (var y = 4; y <= 36; y += 4) HLine(exitLocked, 2, 25, y, C(62, 64, 68)); // 셔터
            Save("prop_exit_locked", Finish(exitLocked), 0.5f, 0f); n++;

            // 교실 명패
            var plate = New(12, 5);
            Fill(plate, 0, 0, 11, 4, C(200, 196, 182));
            HLine(plate, 2, 9, 2, C(70, 66, 60));
            Save("prop_plate", Finish(plate), 0.5f, 0f); n++;

            // 창문 (안쪽 테두리; 유리는 반투명)
            var window = New(32, 28);
            Fill(window, 0, 0, 31, 27, C(190, 190, 184));
            FillA(window, 2, 2, 14, 25, new Color(0.55f, 0.66f, 0.72f, 0.35f));
            FillA(window, 17, 2, 29, 25, new Color(0.55f, 0.66f, 0.72f, 0.35f));
            VLine(window, 15, 0, 27, C(150, 150, 146));
            VLine(window, 16, 0, 27, C(150, 150, 146));
            window.Apply();
            Save("prop_window", window, 0f, 0f, tiled: false); n++;

            // 게시판
            var notice = New(30, 18);
            Fill(notice, 0, 0, 29, 17, C(110, 80, 52));
            Fill(notice, 2, 2, 27, 15, C(160, 120, 80));
            Fill(notice, 4, 5, 10, 13, C(214, 210, 196));
            Fill(notice, 13, 7, 19, 14, C(200, 190, 150));
            Fill(notice, 21, 4, 26, 11, C(190, 200, 214));
            Save("prop_notice", Finish(notice), 0.5f, 0f); n++;

            // 형광등
            var lamp = New(20, 3);
            Fill(lamp, 0, 0, 19, 2, C(120, 120, 116));
            HLine(lamp, 1, 18, 0, C(230, 232, 220));
            Save("prop_ceiling_lamp", Finish(lamp), 0.5f, 1f); n++;

            return n;
        }

        // ── 교실동 특수실 소품·벽 장식 (양호실·과학실·음악실·가사 실습실·컴퓨터실·방송실) ──

        private static int BuildSchoolRooms()
        {
            var n = 0;
            var metal = C(150, 154, 158);
            var dark = C(52, 54, 58);
            var wood = C(146, 108, 68);

            // 양호실 침대 (흰 시트, 철제 다리, 커튼 봉)
            var bed = New(30, 22);
            Fill(bed, 1, 6, 28, 9, C(220, 222, 220)); // 매트리스
            Fill(bed, 1, 10, 7, 12, C(236, 238, 236)); // 베개
            Fill(bed, 8, 9, 28, 11, C(170, 196, 214)); // 이불
            HLine(bed, 0, 29, 5, metal);
            VLine(bed, 1, 0, 5, metal);
            VLine(bed, 28, 0, 5, metal);
            VLine(bed, 0, 5, 14, metal); // 머리판
            Fill(bed, 0, 15, 1, 21, C(190, 210, 200)); // 커튼
            Save("prop_bed", Finish(bed), 0.5f, 0f); n++;

            // 과학실 실험대 (검은 상판, 개수대, 비커)
            var bench = New(28, 18);
            Fill(bench, 0, 9, 27, 11, C(40, 42, 46)); // 상판
            Fill(bench, 1, 0, 26, 8, C(196, 200, 196)); // 하부장
            VLine(bench, 13, 1, 7, C(150, 152, 150));
            bench.SetPixel(7, 4, dark);
            bench.SetPixel(19, 4, dark);
            Fill(bench, 4, 12, 6, 15, C(170, 210, 220)); // 비커
            Fill(bench, 4, 12, 6, 13, C(110, 190, 120)); // 약품
            Fill(bench, 20, 12, 21, 16, C(180, 214, 224)); // 시험관
            VLine(bench, 24, 12, 17, metal); // 수도꼭지
            HLine(bench, 22, 24, 17, metal);
            Save("prop_labbench", Finish(bench), 0.5f, 0f); n++;

            // 약품장 (유리문, 갈색 병)
            var chem = New(18, 32);
            Fill(chem, 0, 0, 17, 31, C(120, 98, 72));
            Fill(chem, 2, 12, 15, 29, C(150, 178, 186));
            for (var y = 14; y <= 26; y += 6)
            {
                HLine(chem, 2, 15, y - 1, C(100, 82, 60));
                for (var x = 3; x <= 13; x += 3) Fill(chem, x, y, x + 1, y + 3, (x + y) % 2 == 0 ? C(120, 70, 40) : C(90, 140, 90));
            }

            Fill(chem, 2, 2, 15, 10, C(136, 112, 84));
            chem.SetPixel(8, 6, dark);
            Save("prop_chemshelf", Finish(chem), 0.5f, 0f); n++;

            // 음악실 업라이트 피아노
            var piano = New(28, 22);
            Fill(piano, 0, 0, 27, 21, C(40, 30, 28));
            Fill(piano, 1, 10, 26, 12, C(236, 232, 220)); // 흰 건반
            for (var x = 2; x <= 25; x += 3) Fill(piano, x, 11, x, 12, C(20, 18, 18)); // 검은 건반
            Fill(piano, 2, 14, 25, 20, C(58, 44, 40));
            Fill(piano, 10, 16, 17, 19, C(220, 214, 196)); // 악보
            VLine(piano, 2, 0, 9, C(30, 22, 20));
            VLine(piano, 25, 0, 9, C(30, 22, 20));
            Save("prop_piano", Finish(piano), 0.5f, 0f); n++;

            // 악기장 (기타 넥, 북)
            var instr = New(20, 30);
            Fill(instr, 0, 0, 19, 29, wood);
            Fill(instr, 1, 1, 18, 28, C(120, 88, 56));
            Fill(instr, 3, 14, 8, 22, C(196, 150, 80)); // 기타 몸통
            VLine(instr, 6, 22, 27, C(80, 56, 36)); // 넥
            Fill(instr, 11, 3, 17, 9, C(170, 60, 50)); // 북
            HLine(instr, 11, 17, 9, C(230, 220, 200));
            HLine(instr, 1, 18, 12, C(90, 66, 42)); // 선반
            Save("prop_instrument", Finish(instr), 0.5f, 0f); n++;

            // 가사 실습실 조리대 (도마, 냄비)
            var kitchen = New(26, 20);
            Fill(kitchen, 0, 0, 25, 10, C(200, 196, 184));
            Fill(kitchen, 0, 11, 25, 12, C(170, 172, 176)); // 스테인리스 상판
            VLine(kitchen, 12, 1, 9, C(160, 156, 146));
            kitchen.SetPixel(6, 6, dark);
            kitchen.SetPixel(18, 6, dark);
            Fill(kitchen, 3, 13, 10, 14, C(196, 150, 96)); // 도마
            Fill(kitchen, 16, 13, 22, 18, metal); // 냄비
            HLine(kitchen, 15, 23, 18, dark);
            Save("prop_kitchen", Finish(kitchen), 0.5f, 0f); n++;

            // 가스레인지
            var stove = New(20, 18);
            Fill(stove, 0, 0, 19, 11, C(220, 220, 214));
            Fill(stove, 2, 2, 17, 8, C(60, 62, 66)); // 오븐 창
            Fill(stove, 0, 12, 19, 13, C(60, 60, 64));
            Fill(stove, 3, 14, 7, 15, C(40, 40, 44)); // 화구
            Fill(stove, 12, 14, 16, 15, C(40, 40, 44));
            Fill(stove, 13, 15, 15, 17, C(110, 112, 116)); // 주전자
            Save("prop_stove", Finish(stove), 0.5f, 0f); n++;

            // 컴퓨터실 책상 (모니터, 본체)
            var pc = New(24, 22);
            Fill(pc, 1, 9, 22, 10, C(186, 184, 176)); // 상판
            VLine(pc, 2, 0, 8, dark);
            VLine(pc, 21, 0, 8, dark);
            Fill(pc, 16, 0, 20, 8, C(210, 208, 200)); // 본체
            pc.SetPixel(18, 6, C(80, 200, 120));
            Fill(pc, 4, 12, 13, 19, C(40, 42, 46)); // 모니터
            Fill(pc, 5, 13, 12, 18, C(30, 46, 60));
            VLine(pc, 8, 11, 11, dark);
            HLine(pc, 4, 12, 11, C(90, 92, 96)); // 키보드
            Save("prop_computer", Finish(pc), 0.5f, 0f); n++;

            // 서버·장비 랙 (컴퓨터실·방송실)
            var rack = New(18, 32);
            Fill(rack, 0, 0, 17, 31, C(36, 38, 42));
            for (var y = 3; y <= 27; y += 4)
            {
                Fill(rack, 2, y, 15, y + 2, C(58, 62, 68));
                rack.SetPixel(13, y + 1, y % 8 == 3 ? C(90, 220, 120) : C(230, 170, 60));
            }

            Save("prop_rack", Finish(rack), 0.5f, 0f); n++;

            // 방송실 조정 콘솔 (페이더, 마이크)
            var console = New(28, 20);
            Fill(console, 0, 0, 27, 8, C(70, 72, 78));
            Fill(console, 0, 9, 27, 11, C(50, 52, 58)); // 경사 상판
            for (var x = 2; x <= 24; x += 3) VLine(console, x, 9, 11, x % 2 == 0 ? C(210, 210, 200) : C(200, 90, 70));
            VLine(console, 22, 12, 17, metal); // 마이크 스탠드
            Fill(console, 21, 17, 23, 19, C(30, 30, 34));
            Fill(console, 3, 12, 12, 15, C(30, 34, 40)); // 레벨 미터
            for (var x = 4; x <= 11; x++) console.SetPixel(x, 13, x < 9 ? C(90, 220, 120) : C(230, 80, 60));
            Save("prop_console", Finish(console), 0.5f, 0f); n++;

            // ── 뒷벽 장식 (피벗 왼쪽 아래, 칠판처럼) ──

            // 양호실: 적십자 게시판
            var cross = New(34, 22);
            Fill(cross, 0, 0, 33, 21, C(214, 210, 196));
            Fill(cross, 1, 1, 32, 20, C(236, 234, 224));
            Fill(cross, 6, 9, 14, 12, C(200, 50, 50));
            Fill(cross, 9, 6, 11, 15, C(200, 50, 50));
            for (var y = 6; y <= 16; y += 3) HLine(cross, 18, 29, y, C(150, 150, 150)); // 안내문
            Save("wall_redcross", Finish(cross), 0f, 0f); n++;

            // 과학실: 주기율표
            var periodic = New(46, 26);
            Fill(periodic, 0, 0, 45, 25, C(220, 220, 210));
            for (var row = 0; row < 6; row++)
            {
                for (var col = 0; col < 14; col++)
                {
                    var edge = col < 2 || col > 9;
                    if (!edge && row < 2) continue;
                    var c = col < 2 ? C(220, 140, 120) : col > 11 ? C(140, 180, 220) : edge ? C(230, 210, 120) : C(170, 210, 160);
                    Fill(periodic, 2 + col * 3, 3 + row * 3, 3 + col * 3, 4 + row * 3, c);
                }
            }

            Save("wall_periodic", Finish(periodic), 0f, 0f); n++;

            // 음악실: 오선지 게시판
            var staff = New(40, 22);
            Fill(staff, 0, 0, 39, 21, C(120, 90, 60));
            Fill(staff, 1, 1, 38, 20, C(236, 232, 214));
            for (var y = 5; y <= 17; y += 3) HLine(staff, 3, 36, y, C(70, 70, 70));
            foreach (var (x, y) in new[] { (7, 8), (12, 11), (17, 14), (22, 11), (27, 8), (32, 14) })
            {
                Fill(staff, x, y, x + 1, y + 1, C(30, 30, 30));
                VLine(staff, x + 1, y + 1, y + 5, C(30, 30, 30));
            }

            Save("wall_music", Finish(staff), 0f, 0f); n++;

            // 가사 실습실: 벽걸이 선반 (컵·조리 도구)
            var shelf = New(36, 18);
            Fill(shelf, 0, 0, 35, 2, wood);
            Fill(shelf, 0, 9, 35, 11, wood);
            for (var x = 3; x <= 31; x += 5) Fill(shelf, x, 3, x + 2, 6, (x / 5) % 2 == 0 ? C(220, 220, 214) : C(200, 120, 90)); // 컵
            for (var x = 4; x <= 30; x += 6)
            {
                VLine(shelf, x, 12, 17, metal); // 국자·뒤집개
                Fill(shelf, x - 1, 12, x + 1, 13, metal);
            }

            Save("wall_kitchen", Finish(shelf), 0f, 0f); n++;

            // 컴퓨터실: 화이트보드
            var white = New(44, 24);
            Fill(white, 0, 0, 43, 23, C(170, 172, 176));
            Fill(white, 1, 2, 42, 22, C(240, 242, 244));
            HLine(white, 0, 43, 1, C(130, 132, 136)); // 펜 받침
            for (var i = 0; i < 6; i++) HLine(white, 4, 4 + 8 + (i * 7) % 20, 18 - i * 3, C(60, 90, 170)); // 코드처럼 쓴 줄
            Save("wall_whiteboard", Finish(white), 0f, 0f); n++;

            // 방송실: ON AIR 표시등 + 흡음판
            var onair = New(40, 24);
            Fill(onair, 0, 0, 39, 15, C(90, 84, 80));
            for (var x = 1; x <= 37; x += 4)
            {
                for (var y = 1; y <= 13; y += 4) Fill(onair, x, y, x + 2, y + 2, C(110, 104, 98)); // 흡음판
            }

            Fill(onair, 10, 17, 29, 23, C(40, 20, 20));
            Fill(onair, 11, 18, 28, 22, C(200, 50, 40)); // 불 꺼진 ON AIR
            HLine(onair, 13, 26, 20, C(240, 200, 190));
            Save("wall_onair", Finish(onair), 0f, 0f); n++;

            return n;
        }

        private static Texture2D Stairs(bool up)
        {
            var t = New(24, 52);
            for (var i = 0; i < 8; i++)
            {
                var y = up ? i * 6 : (7 - i) * 6;
                Fill(t, i * 3, 0, Math.Min(23, i * 3 + 2), y + 4, C(92, 90, 86));
                HLine(t, i * 3, Math.Min(23, i * 3 + 2), y + 4, C(140, 136, 128));
            }

            // 난간
            for (var i = 0; i < 24; i++)
            {
                var y = up ? 10 + i * 2 : 10 + (23 - i) * 2;
                if (y < 52) t.SetPixel(i, y, C(160, 120, 70));
                if (i % 6 == 0) VLine(t, i, Math.Max(0, y - 9), y, C(70, 72, 76));
            }

            return Finish(t);
        }

        // ── 벽·바닥 (가로로 이어 붙이는 타일) ──

        private static int BuildWalls()
        {
            var n = 0;
            // 복도: 아래 징두리(연녹색) + 위 회백색 벽 + 굽도리 + 천장 몰딩
            Save("wall_corridor", WallTile(C(116, 132, 118), C(170, 170, 160), C(60, 64, 60), true), 0f, 0f, tiled: true); n++;
            // 복도에서 보이는 교실 벽 (조금 따뜻한 톤)
            Save("wall_room_outside", WallTile(C(128, 118, 100), C(178, 172, 158), C(66, 60, 52), false), 0f, 0f, tiled: true); n++;
            // 교실 안
            Save("wall_classroom", WallTile(C(140, 110, 78), C(186, 178, 160), C(84, 64, 44), false), 0f, 0f, tiled: true); n++;
            // 화장실 (타일)
            Save("wall_restroom", TileWall(C(180, 190, 194), C(150, 160, 166)), 0f, 0f, tiled: true); n++;
            // 교무실·진료실
            Save("wall_office", WallTile(C(120, 120, 126), C(176, 176, 172), C(70, 70, 76), false), 0f, 0f, tiled: true); n++;
            // 편의점 (밝은 타일)
            Save("wall_store", TileWall(C(196, 198, 196), C(176, 180, 178)), 0f, 0f, tiled: true); n++;
            // 약국 (연청색)
            Save("wall_pharmacy", WallTile(C(140, 168, 172), C(200, 210, 208), C(80, 96, 100), false), 0f, 0f, tiled: true); n++;
            // 건물 외벽 (벽돌)
            Save("wall_brick", Brick(), 0f, 0f, tiled: true); n++;
            // 바깥 땅
            var ground = New(32, 16);
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 32; x++) ground.SetPixel(x, y, (x * 7 + y * 13) % 17 == 0 ? C(52, 54, 50) : C(40, 42, 40));
            }

            HLine(ground, 0, 31, 15, C(70, 72, 66));
            Save("ground", Finish(ground, false), 0f, 0f, tiled: true); n++;
            return n;
        }

        private static Texture2D WallTile(Color lower, Color upper, Color baseboard, bool corridor)
        {
            // 72px = 층 높이 4유닛
            var t = New(32, 72);
            for (var y = 0; y < 72; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    Color c;
                    if (y < 3) c = baseboard;
                    else if (y < 26) c = Noise(lower, x, y, 0.04f);
                    else if (y == 26) c = Darken(lower, 0.7f);
                    else if (y < 68) c = Noise(upper, x, y, 0.03f);
                    else c = Darken(upper, 0.75f);
                    t.SetPixel(x, y, c);
                }
            }

            // 징두리 판넬 홈
            for (var y = 4; y < 25; y++) t.SetPixel(0, y, Darken(lower, 0.82f));
            if (corridor)
            {
                // 바닥 반사 (윤기 있는 복도 바닥 느낌)
                HLine(t, 0, 31, 3, Lighten(baseboard, 1.4f));
            }

            return Finish(t, false);
        }

        private static Texture2D TileWall(Color a, Color b)
        {
            var t = New(32, 72);
            for (var y = 0; y < 72; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var grout = x % 8 == 0 || y % 8 == 0;
                    t.SetPixel(x, y, y < 3 ? Darken(b, 0.6f) : grout ? Darken(b, 0.85f) : ((x / 8 + y / 8) % 2 == 0 ? a : b));
                }
            }

            return Finish(t, false);
        }

        private static Texture2D Brick()
        {
            var t = New(32, 16);
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var row = y / 4;
                    var offset = row % 2 == 0 ? 0 : 4;
                    var mortar = y % 4 == 3 || (x + offset) % 8 == 7;
                    t.SetPixel(x, y, mortar ? C(40, 36, 34) : Noise(C(82, 54, 46), x, y, 0.08f));
                }
            }

            return Finish(t, false);
        }

        // ── CC0 타일셋 잘라내기 ──

        private static int BuildThirdParty()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TilesetPath);
            if (importer == null)
            {
                Debug.LogWarning("[Art] 타일셋 없음: " + TilesetPath);
                return 0;
            }

            if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
            var n = 0;
            // 이미지 좌표 (위에서부터 y)
            Slice(src, "door_closed", 83, 5, 109, 47); n++;
            Slice(src, "door_open", 115, 5, 141, 47); n++;
            Slice(src, "door_dark", 51, 5, 77, 47); n++;
            Slice(src, "window_tileset", 176, 6, 222, 39); n++;
            Slice(src, "office_chair", 1, 69, 15, 95); n++;
            Slice(src, "office_desk", 18, 77, 62, 95); n++;
            Slice(src, "office_cooler", 99, 67, 109, 95); n++;
            Slice(src, "office_bin", 81, 83, 95, 95); n++;
            return n;
        }

        private static void Slice(Texture2D src, string key, int x0, int top, int x1, int bottom)
        {
            var w = x1 - x0;
            var h = bottom - top;
            var t = New(w, h);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var c = src.GetPixel(x0 + x, src.height - 1 - (top + (h - 1 - y)));
                    var gray = Mathf.Abs(c.r - 148 / 255f) < 0.01f && Mathf.Abs(c.g - 148 / 255f) < 0.01f && Mathf.Abs(c.b - 148 / 255f) < 0.01f;
                    if (c.a < 0.01f || gray)
                    {
                        t.SetPixel(x, y, Clear);
                        continue;
                    }

                    // 어두운 톤: 채도 30% 낮추고 25% 어둡게
                    var l = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    var muted = Color.Lerp(c, new Color(l, l, l, c.a), 0.3f) * 0.78f;
                    muted.a = c.a;
                    t.SetPixel(x, y, muted);
                }
            }

            t.Apply();
            Save(key, t, 0.5f, 0f);
        }

        // ── 도우미 ──

        private static Texture2D New(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[w * h];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Clear;
            t.SetPixels(pixels);
            return t;
        }

        private static Texture2D Finish(Texture2D t, bool outline = true)
        {
            if (outline) OutlinePass(t);
            t.Apply();
            return t;
        }

        /// <summary>투명 픽셀 중 불투명 픽셀과 맞닿은 곳에 외곽선.</summary>
        private static void OutlinePass(Texture2D t)
        {
            var w = t.width;
            var h = t.height;
            var src = t.GetPixels();
            var dst = (Color[])src.Clone();
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    if (src[y * w + x].a > 0.01f) continue;
                    var touch = (x > 0 && src[y * w + x - 1].a > 0.5f) || (x < w - 1 && src[y * w + x + 1].a > 0.5f)
                                || (y > 0 && src[(y - 1) * w + x].a > 0.5f) || (y < h - 1 && src[(y + 1) * w + x].a > 0.5f);
                    if (touch) dst[y * w + x] = Outline;
                }
            }

            t.SetPixels(dst);
        }

        private static void Fill(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            for (var y = Math.Max(0, y0); y <= Math.Min(t.height - 1, y1); y++)
            {
                for (var x = Math.Max(0, x0); x <= Math.Min(t.width - 1, x1); x++) t.SetPixel(x, y, c);
            }
        }

        private static void FillA(Texture2D t, int x0, int y0, int x1, int y1, Color c) => Fill(t, x0, y0, x1, y1, c);

        private static void HLine(Texture2D t, int x0, int x1, int y, Color c) => Fill(t, x0, y, x1, y, c);

        private static void VLine(Texture2D t, int x, int y0, int y1, Color c) => Fill(t, x, y0, x, y1, c);

        private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

        private static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        private static Color Lighten(Color c, float k) => new Color(Mathf.Min(1, c.r * k), Mathf.Min(1, c.g * k), Mathf.Min(1, c.b * k), c.a);

        /// <summary>결정적 잡음 (같은 좌표는 같은 값).</summary>
        private static Color Noise(Color c, int x, int y, float amount)
        {
            var hash = (x * 73856093) ^ (y * 19349663);
            var v = ((hash & 0xff) / 255f - 0.5f) * 2f * amount;
            return new Color(Mathf.Clamp01(c.r + v), Mathf.Clamp01(c.g + v), Mathf.Clamp01(c.b + v), c.a);
        }

        private static void Save(string key, Texture2D tex, float pivotX, float pivotY, bool tiled = false)
        {
            var path = $"{OutFolder}/{key}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(pivotX, pivotY);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
