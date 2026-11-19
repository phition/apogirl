using System;
using System.IO;
using System.Text;
using ApoGirl.Core.State;

namespace ApoGirl.Systems.Save
{
    /// <summary>
    /// 세이브 파일 관리: 자동 저장 1 + 수동 저장 3슬롯 (문서 9.5).
    /// 저장 폴더는 주입받는다 (게임에서는 Application.persistentDataPath/Saves, 테스트에서는 임시 폴더).
    /// </summary>
    public sealed class SaveService
    {
        public const string AutoSlot = "auto";
        public const int ManualSlotCount = 3;

        private readonly string _directory;
        private readonly GameStateSerializer _serializer;

        public SaveService(string directory, GameStateSerializer serializer)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public static string ManualSlot(int number)
        {
            if (number < 1 || number > ManualSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(number), number, $"수동 저장 슬롯은 1~{ManualSlotCount}");
            }

            return "slot" + number;
        }

        public bool Exists(string slot)
        {
            return File.Exists(PathFor(slot));
        }

        /// <summary>임시 파일에 쓴 뒤 교체해 저장 중 종료돼도 기존 세이브가 깨지지 않게 한다.</summary>
        public void Save(string slot, GameState state)
        {
            var path = PathFor(slot);
            Directory.CreateDirectory(_directory);
            var temp = path + ".tmp";
            File.WriteAllText(temp, _serializer.Serialize(state), new UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(temp, path);
        }

        public GameState Load(string slot)
        {
            var path = PathFor(slot);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"세이브 없음: {slot}", path);
            }

            return _serializer.Deserialize(File.ReadAllText(path, Encoding.UTF8));
        }

        public void Delete(string slot)
        {
            var path = PathFor(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string PathFor(string slot)
        {
            var valid = slot == AutoSlot;
            for (var i = 1; i <= ManualSlotCount && !valid; i++)
            {
                valid = slot == ManualSlot(i);
            }

            if (!valid)
            {
                throw new ArgumentException($"알 수 없는 세이브 슬롯: '{slot}'", nameof(slot));
            }

            return Path.Combine(_directory, slot + ".json");
        }
    }
}
