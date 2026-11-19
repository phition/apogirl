using System;
using System.IO;
using ApoGirl.Core.Simulation.Bots;
using Newtonsoft.Json;

namespace ApoGirl.Systems.Content
{
    /// <summary>M7-1 봇 시뮬레이터 설정 JSON 읽기 (콘텐츠 폴더 밖: 게임 데이터가 아니라 개발 도구 데이터).</summary>
    public static class BotConfigJson
    {
        public const string DefaultPath = "Assets/_Project/Balance/bot_strategies.json";

        public static BotConfigDef Parse(string json, string sourceName = "")
        {
            try
            {
                return JsonConvert.DeserializeObject<BotConfigDef>(json, ContentJson.Settings) ?? new BotConfigDef();
            }
            catch (JsonException exception)
            {
                throw new FormatException($"봇 설정 JSON 오류 ({sourceName}): {exception.Message}", exception);
            }
        }

        public static BotConfigDef Load(string path)
        {
            return Parse(File.ReadAllText(path), path);
        }
    }
}
