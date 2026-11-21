using System.Linq;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Screens;
using NUnit.Framework;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>입상 영상 묶음 고르기 (standing_map.json Motion.Sets, 힉스필드 3·4차).</summary>
    public sealed class StandingMotionTests
    {
        [TestCase("base", "neutral", "base_idle")]
        [TestCase("face_tired", "tired", "face_tired_idle")]
        [TestCase("mark_mask", "sad", "mark_mask_idle")]
        [TestCase("outfit_summer", "tired", "outfit_summer_tired")]
        [TestCase("outfit_winter", "worried", "outfit_winter_worried")]
        [TestCase("outfit_summer", "neutral", "summer_idle")]
        [TestCase("outfit_winter", "surprised", "winter_idle")]
        [TestCase("hair_long", "neutral", "hair_long_idle")]
        public void PickMotion_ByPictureAndFace(string picture, string face, string idle)
        {
            var set = StandingConfig.Load().PickMotion(picture, face);
            Assert.IsNotNull(set, picture + "/" + face);
            Assert.AreEqual(idle, set.Idle);
        }

        [Test]
        public void PickMotion_NoSetForArmorOrReact()
        {
            Assert.IsNull(StandingConfig.Load().PickMotion("outfit_riot", "tired"), "방어구 차림은 눈 깜빡임·시선만");
            Assert.IsNull(StandingConfig.Load().PickMotion("face_surprised", "surprised"));
        }

        [Test]
        public void EverySetClip_HasFrames()
        {
            var config = StandingConfig.Load();
            foreach (var set in config.Motion.Sets)
            {
                foreach (var clip in new[] { set.Idle }.Concat(set.Moves).ToArray())
                {
                    Assert.IsNotNull(StoryArt.Load(config.Motion.Folder + "/" + clip + "/m_00"), "프레임 없음: " + clip);
                }
            }
        }
    }
}
