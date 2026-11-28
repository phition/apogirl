using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ApoGirl.Systems.Save
{
    /// <summary>엔딩 하나의 해금 기록.</summary>
    public sealed class GalleryEntry
    {
        public string EndingId { get; set; } = "";

        /// <summary>처음 본 판의 종료 주차.</summary>
        public int FirstWeek { get; set; }

        public int Count { get; set; }

        /// <summary>처음 본 시각 (UTC, ISO 8601).</summary>
        public string FirstSeenUtc { get; set; } = "";

        /// <summary>본 엔딩 변형 ID (좌석 선택 등으로 그림·컷이 바뀐 판). 갤러리 칸은 엔딩 하나.</summary>
        public List<string> Variants { get; set; } = new List<string>();
    }

    /// <summary>
    /// 엔딩 갤러리 (문서 8.2 회차 요소). 세이브 슬롯과 별개인 파일 하나에 기록한다.
    /// 판이 끝날 때마다 Record, 타이틀의 갤러리 화면이 Entries를 읽는다.
    /// </summary>
    public sealed class EndingGallery
    {
        public const string FileName = "Gallery.json";

        private readonly string _path;
        private readonly Dictionary<string, GalleryEntry> _entries = new Dictionary<string, GalleryEntry>();

        public EndingGallery(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            _path = Path.Combine(directory, FileName);
            Load();
        }

        public IReadOnlyDictionary<string, GalleryEntry> Entries => _entries;

        public bool IsUnlocked(string endingId) => _entries.ContainsKey(endingId ?? "");

        /// <summary>조합 기록의 ID 머리 (세 막 엔딩: 구조 장면·회고 축·미래 묶음). 엔딩 ID와 섞이지 않게 따로 둔다.</summary>
        public const string ComboPrefix = "COMBO|";

        /// <summary>지금까지 본 서로 다른 조합 수 ("지금까지 보낸 1년: n가지").</summary>
        public int ComboCount
        {
            get
            {
                var count = 0;
                foreach (var key in _entries.Keys)
                {
                    if (key.StartsWith(ComboPrefix, StringComparison.Ordinal)) count++;
                }

                return count;
            }
        }

        /// <summary>엔딩 칸에 해당하는 기록인가 (조합 기록이 아닌 것).</summary>
        public static bool IsEnding(string entryId) => !string.IsNullOrEmpty(entryId) && !entryId.StartsWith(ComboPrefix, StringComparison.Ordinal);

        /// <summary>세 막의 조합을 본 것으로 남긴다. 처음 본 조합이면 true.</summary>
        public bool RecordCombo(string rescueId, string reflectionId, string futureId)
        {
            var id = ComboPrefix + (rescueId ?? "") + "|" + (reflectionId ?? "") + "|" + (futureId ?? "");
            var isNew = !_entries.TryGetValue(id, out var entry);
            if (isNew)
            {
                entry = new GalleryEntry { EndingId = id, FirstWeek = 0, FirstSeenUtc = DateTime.UtcNow.ToString("o") };
                _entries[id] = entry;
            }

            entry.Count++;
            Save();
            return isNew;
        }

        /// <summary>엔딩을 봤다고 기록하고 저장한다. 처음 본 엔딩이면 true.</summary>
        public bool Record(string endingId, int week, string variantId = null)
        {
            if (string.IsNullOrEmpty(endingId))
            {
                return false;
            }

            var isNew = !_entries.TryGetValue(endingId, out var entry);
            if (isNew)
            {
                entry = new GalleryEntry { EndingId = endingId, FirstWeek = week, FirstSeenUtc = DateTime.UtcNow.ToString("o") };
                _entries[endingId] = entry;
            }

            entry.Count++;
            if (entry.Variants == null) entry.Variants = new List<string>();
            if (!string.IsNullOrEmpty(variantId) && !entry.Variants.Contains(variantId)) entry.Variants.Add(variantId);
            Save();
            return isNew;
        }

        private void Load()
        {
            _entries.Clear();
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<GalleryEntry>>(File.ReadAllText(_path, Encoding.UTF8));
                foreach (var entry in list ?? new List<GalleryEntry>())
                {
                    if (!string.IsNullOrEmpty(entry.EndingId))
                    {
                        _entries[entry.EndingId] = entry;
                    }
                }
            }
            catch (JsonException)
            {
                // 깨진 갤러리 파일은 새로 시작한다 (세이브와 달리 진행에 영향 없음)
                _entries.Clear();
            }
        }

        private void Save()
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var list = new List<GalleryEntry>(_entries.Values);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(list, Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }
    }
}
