using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Save;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>엔딩 갤러리 (타이틀). 본 엔딩은 제목·첫 컷·처음 본 주차, 못 본 엔딩은 구분만 보인다.</summary>
    public sealed class GalleryView
    {
        private readonly VisualElement _root;

        /// <summary>갤러리에서 막을 보여 주는 차례: 그 뒤의 나 → 돌아보는 밤 → 구조의 날 → 배드.</summary>
        private static int ActOrder(EndingAct act)
        {
            switch (act)
            {
                case EndingAct.Future: return 0;
                case EndingAct.Reflection: return 1;
                case EndingAct.Rescue: return 2;
                default: return 3;
            }
        }

        public GalleryView(VisualElement parent, IDefinitionDatabase db, EndingGallery gallery, Action onClosed)
        {
            _root = new VisualElement { name = "gallery-view" };
            _root.AddToClassList("gallery-view");
            parent.Add(_root);

            var endings = db.Extras.Endings;
            var unlocked = 0;
            foreach (var ending in endings)
            {
                if (gallery != null && gallery.IsUnlocked(ending.Id)) unlocked++;
            }

            var header = new Label(Loc.T("gallery.title"));
            header.AddToClassList("ending-record-title");
            _root.Add(header);
            var progress = new Label(Loc.F("gallery.progress", unlocked, endings.Count)
                                     + (gallery != null && gallery.ComboCount > 0 ? "   ·   " + Loc.F("gallery.combos", gallery.ComboCount) : ""));
            progress.AddToClassList("gallery-progress");
            _root.Add(progress);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("gallery-scroll");
            _root.Add(scroll);

            // 막마다 한 묶음: 그 뒤의 나(미래) → 돌아보는 밤(회고) → 구조의 날(장면) → 배드
            var shownAct = (EndingAct)(-1);
            foreach (var ending in endings.OrderBy(e => ActOrder(e.Act)))
            {
                if (ending.Act != shownAct)
                {
                    shownAct = ending.Act;
                    var section = new Label(Loc.T("gallery.act." + ending.Act));
                    section.AddToClassList("ending-record-section");
                    section.AddToClassList("gallery-section");
                    scroll.Add(section);
                }

                var entry = default(GalleryEntry);
                var seen = gallery != null && gallery.Entries.TryGetValue(ending.Id, out entry);
                var card = new VisualElement();
                card.AddToClassList("gallery-card");
                if (seen && ColorUtility.TryParseHtmlString(ending.Color, out var color))
                {
                    card.style.backgroundColor = color;
                    var art = Art.StoryArt.Ending(ending.Id);
                    Art.StoryArt.SetCover(card, art);
                    card.EnableInClassList("with-art", art != null);
                }
                else
                {
                    card.AddToClassList("locked");
                }

                var category = new Label(Loc.T("ending.cat." + ending.Category));
                category.AddToClassList("gallery-category");
                card.Add(category);

                var title = new Label(seen ? StoryText.T(ending.TitleKey, null) : Loc.T("gallery.locked"));
                title.AddToClassList("gallery-title");
                card.Add(title);

                if (seen)
                {
                    if (ending.CutKeys.Count > 0)
                    {
                        var cut = new Label(StoryText.T(ending.CutKeys[0], null));
                        cut.AddToClassList("gallery-cut");
                        card.Add(cut);
                    }

                    var info = new Label(Loc.F("gallery.seen", entry.FirstWeek, entry.Count));
                    info.AddToClassList("gallery-info");
                    card.Add(info);
                }

                scroll.Add(card);
            }

            var close = new Button(() =>
            {
                _root.RemoveFromHierarchy();
                onClosed?.Invoke();
            }) { text = Loc.T("common.close") };
            close.AddToClassList("ending-button");
            _root.Add(close);
            KoreanFont.ApplyDeep(_root);
        }
    }
}
