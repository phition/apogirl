using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace ApoGirl.Presentation.Text
{
    /// <summary>
    /// 로컬라이제이션 조회. 원본은 Assets/_Project/Localization/Source/ko.json →
    /// 메뉴 ApoGirl/Localization/Sync Tables 로 문자열 테이블에 반영한다. 없는 키는 키 자체를 돌려준다.
    /// </summary>
    public static class Loc
    {
        public const string UiTable = "UI";
        public const string ActionTable = "Actions";
        public const string ItemTable = "Items";
        private const string MissingPrefix = "No translation found";

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        public static string T(string key)
        {
            return Get(UiTable, key, key);
        }

        public static string F(string key, params object[] args)
        {
            try
            {
                return string.Format(T(key), args);
            }
            catch (FormatException)
            {
                return key;
            }
        }

        /// <summary>행동 문자열. suffix: name / short.</summary>
        public static string Action(string actionId, string suffix = "name")
        {
            return Get(ActionTable, actionId + "." + suffix, actionId);
        }

        /// <summary>아이템 이름.</summary>
        public static string Item(string itemId)
        {
            return Get(ItemTable, itemId + ".name", itemId);
        }

        /// <summary>테이블 조회. 없으면 null (Yarn 대사 등 대체 경로가 있는 경우).</summary>
        public static string Lookup(string table, string key)
        {
            var value = Get(table, key, null);
            return value;
        }

        /// <summary>단어 + 조사 (UI 문자열용, 예: Loc.Josa(name, "이/가")).</summary>
        public static string Josa(string word, string pair) => ApoGirl.Core.Text.KoreanJosa.Attach(word, pair);

        private static string Get(string table, string key, string fallback)
        {
            var cacheKey = table + "/" + key;
            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            string value = null;
            try
            {
                value = LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
            }
            catch (Exception)
            {
                // 로컬라이제이션 미설정: 키로 대체
            }

            if (string.IsNullOrEmpty(value) || value.StartsWith(MissingPrefix, StringComparison.Ordinal))
            {
                return fallback;
            }

            Cache[cacheKey] = value;
            return value;
        }
    }
}
