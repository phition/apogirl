using System;
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
            var progress = new Label(Loc.F("gallery.progress", unlocked, endings.Count));
            progress.AddToClassList("gallery-progress");
            _root.Add(progress);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("gallery-scroll");
            _root.Add(scroll);

            foreach (var ending in endings)
            {
                var entry = default(GalleryEntry);
                var seen = gallery != null && gallery.Entries.TryGetValue(ending.Id, out entry);
                var card = new VisualElement();
                card.AddToClassList("gallery-card");
                if (seen && ColorUtility.TryParseHtmlString(ending.Color, out var color))
                {
                    card.style.backgroundColor = color;
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
