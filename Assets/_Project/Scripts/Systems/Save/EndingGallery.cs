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

        /// <summary>엔딩을 봤다고 기록하고 저장한다. 처음 본 엔딩이면 true.</summary>
        public bool Record(string endingId, int week)
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
