using System;
using System.Collections.Generic;
using ApoGirl.Core.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Art
{
    /// <summary>
    /// 이야기 그림 (M6-5): 허브 배경·사건 그림·대사 초상화·엔딩 원화·지역 그림·UI 아이콘.
    /// 그림은 Resources/Art/ 아래(원본은 ArtInbox/, 변환 도구는 code_temp의 art_import.py),
    /// 어디에 무엇을 쓰는지는 Resources/Art/art_map.json. 그림이 없으면 null을 돌려주고 화면은 지금처럼 그린다.
    /// </summary>
    public static class StoryArt
    {
        public const string PlayerSpeaker = "{PLAYER}";

        /// <summary>친구(지우) 화자 키: 대사 이름은 플레이어가 정한 호칭이라 표에는 이 키로 둔다.</summary>
        public const string FriendSpeaker = "{FRIEND}";

        [Serializable]
        private sealed class SpeakerEntry
        {
            public string Name = "";
            public string Portrait = "";
            public string[] IllFlags = Array.Empty<string>();
        }

        [Serializable]
        private sealed class EventEntry
        {
            public string Event = "";
            public string Image = "";
        }

        [Serializable]
        private sealed class FlagEntry
        {
            public string Flag = "";
            public string Image = "";

            /// <summary>밤 그림 (결산 화면 뒤 등, 없으면 Image).</summary>
            public string Night = "";
        }

        [Serializable]
        private sealed class LocationEntry
        {
            public string Location = "";
            public string Image = "";
        }

        [Serializable]
        private sealed class IconEntry
        {
            public string Key = "";
            public string Image = "";
        }

        [Serializable]
        private sealed class ArtMap
        {
            public IconEntry[] Icons = Array.Empty<IconEntry>();
            public SpeakerEntry[] Speakers = Array.Empty<SpeakerEntry>();
            public EventEntry[] Events = Array.Empty<EventEntry>();
            public FlagEntry[] Backdrops = Array.Empty<FlagEntry>();
            public LocationEntry[] Locations = Array.Empty<LocationEntry>();

            /// <summary>세력 대표 얼굴 (Location 칸에 세력 ID, Image는 Resources/Art 아래 경로).</summary>
            public LocationEntry[] Factions = Array.Empty<LocationEntry>();
        }

        private static ArtMap _map;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private static ArtMap Map
        {
            get
            {
                if (_map == null)
                {
                    var asset = Resources.Load<TextAsset>("Art/art_map");
                    _map = asset != null ? JsonUtility.FromJson<ArtMap>(asset.text) ?? new ArtMap() : new ArtMap();
                }

                return _map;
            }
        }

        /// <summary>Resources/Art/{path} 텍스처 (확장자 없이). 없으면 null.</summary>
        public static Texture2D Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!Cache.TryGetValue(path, out var texture))
            {
                texture = Resources.Load<Texture2D>("Art/" + path);
                Cache[path] = texture;
            }

            return texture;
        }

        public static Texture2D Story(string path) => Load("Story/" + path);

        public static Texture2D Ui(string key) => Load("UI/" + key);

        /// <summary>허브 배경: 플래그가 맞는 첫 그림 (빈 플래그 = 기본). night면 밤 그림이 있으면 그것.</summary>
        public static Texture2D Backdrop(GameState state, bool night = false)
        {
            foreach (var entry in Map.Backdrops)
            {
                if (string.IsNullOrEmpty(entry.Flag) || (state != null && state.Flags.Contains(entry.Flag)))
                {
                    return night && !string.IsNullOrEmpty(entry.Night) ? Story(entry.Night) ?? Story(entry.Image) : Story(entry.Image);
                }
            }

            return null;
        }

        /// <summary>이벤트 동안 대화 뒤에 까는 그림 (없으면 null).</summary>
        public static Texture2D EventImage(string eventId)
        {
            foreach (var entry in Map.Events)
            {
                if (entry.Event == eventId)
                {
                    return Story(entry.Image);
                }
            }

            return null;
        }

        public static Texture2D Location(string locationId)
        {
            foreach (var entry in Map.Locations)
            {
                if (entry.Location == locationId)
                {
                    return Story(entry.Image);
                }
            }

            return null;
        }

        /// <summary>
        /// UI 아이콘 (art_map.json의 Icons 표, 키 예: res.Food · cat.Education · stat.Stamina · npc.NPC_DOHYEON). 없으면 null.
        /// </summary>
        public static Texture2D Icon(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            foreach (var entry in Map.Icons)
            {
                if (entry.Key == key)
                {
                    return Ui(entry.Image);
                }
            }

            return null;
        }

        /// <summary>아이템 아이콘: 표에 item.{id}가 있으면 그것, 없으면 이름 규칙 (ITEM_CANNED_FOOD → UI/item_canned_food).</summary>
        public static Texture2D ItemIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            var mapped = Icon("item." + itemId);
            if (mapped != null)
            {
                return mapped;
            }

            var name = itemId.StartsWith("ITEM_") ? itemId.Substring(5) : itemId;
            return Ui("item_" + name.ToLowerInvariant());
        }

        /// <summary>글자 왼쪽에 작은 아이콘을 깐다 (클래스 with-icon이 왼쪽 여백을 준다). null이면 아이콘을 지운다.</summary>
        public static void SetIcon(VisualElement element, Texture2D icon, float size = 24f)
        {
            if (element == null)
            {
                return;
            }

            element.EnableInClassList("with-icon", icon != null);
            if (icon == null)
            {
                element.style.backgroundImage = StyleKeyword.Null;
                return;
            }

            element.style.backgroundImage = new StyleBackground(icon);
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(size, LengthUnit.Pixel), new Length(size, LengthUnit.Pixel)));
            element.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            element.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(4, LengthUnit.Pixel)));
            element.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
        }

        /// <summary>세력 대표 얼굴 (없으면 null).</summary>
        public static Texture2D Faction(string factionId)
        {
            foreach (var entry in Map.Factions)
            {
                if (entry.Location == factionId)
                {
                    return Load(entry.Image);
                }
            }

            return null;
        }

        public static Texture2D Ending(string endingId) => string.IsNullOrEmpty(endingId) ? null : Story("Endings/" + endingId);

        /// <summary>
        /// 말하는 사람의 초상화. playerName이 같으면 주인공. 표정: expression → (아픈 플래그면 ill) → neutral 순서로 찾는다.
        /// </summary>
        public static Texture2D Portrait(string speaker, string playerName, GameState state, string expression = "")
        {
            if (string.IsNullOrEmpty(speaker))
            {
                return null;
            }

            var key = speaker == playerName ? PlayerSpeaker : speaker == Dialogue.YarnFunctions.Friend() ? FriendSpeaker : speaker;
            foreach (var entry in Map.Speakers)
            {
                if (entry.Name != key)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(expression))
                {
                    var exact = Load("Portraits/" + entry.Portrait + "_" + expression);
                    if (exact != null) return exact;
                }

                if (state != null)
                {
                    foreach (var flag in entry.IllFlags)
                    {
                        if (state.Flags.Contains(flag))
                        {
                            var ill = Load("Portraits/" + entry.Portrait + "_ill");
                            if (ill != null) return ill;
                        }
                    }
                }

                return Load("Portraits/" + entry.Portrait + "_neutral");
            }

            return null;
        }

        /// <summary>요소 배경을 그림으로 채운다 (넘치는 쪽을 잘라 꽉 차게). null이면 배경 그림을 지운다.</summary>
        public static void SetCover(VisualElement element, Texture2D texture)
        {
            if (element == null)
            {
                return;
            }

            if (texture == null)
            {
                element.style.backgroundImage = StyleKeyword.Null;
                return;
            }

            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            element.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            element.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
        }

        /// <summary>요소 배경을 그림으로 (비율 유지, 안에 맞춤).</summary>
        public static void SetContain(VisualElement element, Texture2D texture)
        {
            SetCover(element, texture);
            if (texture != null)
            {
                element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
            }
        }
    }
}
