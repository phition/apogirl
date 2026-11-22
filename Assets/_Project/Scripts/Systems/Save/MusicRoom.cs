using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ApoGirl.Systems.Save
{
    /// <summary>
    /// 타이틀 "음악실" (기획 A-2 ①): 어느 판에서든 한 번이라도 튼 CD를 기록한다. 세이브 슬롯과 별개인 파일 하나.
    /// 진열장(판 안의 CD)은 판마다 새로 모은다. 여기에는 들은 곡만 남는다.
    /// </summary>
    public sealed class MusicRoom
    {
        public const string FileName = "MusicRoom.json";

        private readonly string _path;
        private readonly Dictionary<string, string> _heard = new Dictionary<string, string>();

        public MusicRoom(string directory)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            _path = Path.Combine(directory, FileName);
            Load();
        }

        public int Count => _heard.Count;

        public bool IsHeard(string cdId) => _heard.ContainsKey(cdId ?? "");

        /// <summary>들었다고 기록한다. 처음 들은 곡이면 저장하고 true.</summary>
        public bool Record(string cdId)
        {
            if (string.IsNullOrEmpty(cdId) || _heard.ContainsKey(cdId))
            {
                return false;
            }

            _heard[cdId] = DateTime.UtcNow.ToString("o");
            Save();
            return true;
        }

        private void Load()
        {
            _heard.Clear();
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_path, Encoding.UTF8));
                foreach (var pair in map ?? new Dictionary<string, string>())
                {
                    if (!string.IsNullOrEmpty(pair.Key)) _heard[pair.Key] = pair.Value ?? "";
                }
            }
            catch (JsonException)
            {
                // 깨진 파일은 새로 시작한다 (진행에 영향 없음)
                _heard.Clear();
            }
        }

        private void Save()
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(_heard, Formatting.Indented), new UTF8Encoding(false));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temp, _path);
        }
    }
}
