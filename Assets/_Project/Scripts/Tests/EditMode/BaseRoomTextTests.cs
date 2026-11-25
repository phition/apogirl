using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Systems.Content;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// 거점 상황판 시설 칸 (BASE_TAB_PROPOSAL P2): 단면도 방마다 쓰임·힌트 문구가 있고,
    /// 시설 효과 종류마다 문구가 있고, 역할 방 묶음이 단면도 방을 가리킨다.
    /// </summary>
    public sealed class BaseRoomTextTests
    {
        private static string Root => Path.GetDirectoryName(Application.dataPath) ?? "";

        private static HashSet<string> Keys()
        {
            var ko = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Assets/_Project/Localization/Source/ko.json")));
            return new HashSet<string>(ko.Descendants().OfType<JProperty>().Where(p => p.Value.Type == JTokenType.String).Select(p => p.Name));
        }

        private static JArray Rooms()
        {
            var section = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Assets/_Project/Resources/Art/hub_section.json")));
            return (JArray)section["Rooms"];
        }

        [Test]
        public void EveryRoom_HasUse_AndLockedRoomsHaveHint()
        {
            var keys = Keys();
            foreach (var room in Rooms())
            {
                var id = (string)room["Id"];
                Assert.IsTrue(keys.Contains("hubroom." + id + ".use"), id + " 쓰임 문구");
                if (room["Always"]?.Value<bool>() != true)
                {
                    Assert.IsTrue(keys.Contains("hubroom." + id + ".hint"), id + " 여는 방법 문구");
                }
            }
        }

        [Test]
        public void EveryFacilityEffect_HasText()
        {
            var keys = Keys();
            var content = ContentJson.LoadFolder(Path.Combine(Root, ContentJson.ContentFolder));
            Assert.Greater(content.Facilities.Count, 0);
            foreach (var modifier in content.Facilities.SelectMany(f => f.Modifiers))
            {
                Assert.IsTrue(keys.Contains("facility.mod." + modifier.Kind), modifier.Kind + " 효과 문구");
                if (!string.IsNullOrEmpty(modifier.Key))
                {
                    Assert.IsTrue(keys.Contains("prof." + modifier.Key), modifier.Key + " 숙련 이름");
                }
            }
        }

        [Test]
        public void RoleRooms_AreSectionRooms()
        {
            var ids = new HashSet<string>(Rooms().Select(r => (string)r["Id"]));
            var content = ContentJson.LoadFolder(Path.Combine(Root, ContentJson.ContentFolder));
            foreach (var role in content.Roles)
            {
                foreach (var room in role.Rooms)
                {
                    Assert.IsTrue(ids.Contains(room.Id), role.Id + "의 방 " + room.Id + "이 단면도에 있다");
                }
            }

            // 방 패널의 "○○ 자리" 줄은 방마다 역할 하나를 고른다: 한 방이 두 역할에 묶이지 않는다
            var all = content.Roles.SelectMany(r => r.Rooms.Select(room => room.Id)).ToList();
            Assert.AreEqual(all.Count, all.Distinct().Count());
        }
    }
}
