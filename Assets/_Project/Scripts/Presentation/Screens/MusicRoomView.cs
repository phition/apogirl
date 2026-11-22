using System;
using ApoGirl.Presentation.Audio;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 타이틀 "음악실" (기획 A-2 ①): 어느 판에서든 한 번이라도 튼 CD는 여기서 언제든 다시 듣는다.
    /// 못 들은 곡은 칸만 보인다. 엔딩 갤러리와 같은 모양 (gallery-* 스타일을 같이 쓴다).
    /// </summary>
    public sealed class MusicRoomView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _list;
        private string _playing = "";

        public MusicRoomView(VisualElement parent, Action onClosed)
        {
            _root = new VisualElement { name = "music-room-view" };
            _root.AddToClassList("gallery-view");
            parent.Add(_root);

            var room = CdPlayer.Room;
            var header = new Label(Loc.T("music.title"));
            header.AddToClassList("ending-record-title");
            _root.Add(header);
            var progress = new Label(Loc.F("music.progress", room?.Count ?? 0, CdPlayer.All.Count));
            progress.AddToClassList("gallery-progress");
            _root.Add(progress);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("gallery-scroll");
            _root.Add(scroll);
            _list = scroll;

            if (room == null || room.Count == 0)
            {
                var empty = new Label(Loc.T("music.empty"));
                empty.AddToClassList("gallery-info");
                _root.Insert(2, empty);
            }

            Fill();

            var close = new Button(() =>
            {
                Sound.StopLoop(CdPlayer.Channel);
                _root.RemoveFromHierarchy();
                onClosed?.Invoke();
            }) { text = Loc.T("common.close") };
            close.AddToClassList("ending-button");
            _root.Add(close);
            KoreanFont.ApplyDeep(_root);
        }

        private void Fill()
        {
            _list.Clear();
            foreach (var entry in CdPlayer.All)
            {
                var heard = CdPlayer.Room != null && CdPlayer.Room.IsHeard(entry.Id);
                var card = new VisualElement();
                card.AddToClassList("gallery-card");
                if (heard && ColorUtility.TryParseHtmlString(entry.Color, out var color))
                {
                    card.style.borderLeftWidth = 8;
                    card.style.borderLeftColor = color;
                }
                else
                {
                    card.AddToClassList("locked");
                }

                var title = new Label(heard ? Loc.T("cd." + entry.Id + ".title") : Loc.T("music.locked"));
                title.AddToClassList("gallery-title");
                card.Add(title);

                if (heard)
                {
                    var owner = new Label(Loc.T("cd." + entry.Id + ".owner") + "  ·  " + Loc.T("location." + entry.Location));
                    owner.AddToClassList("gallery-cut");
                    card.Add(owner);

                    var id = entry.Id;
                    if (CdPlayer.HasClip(id))
                    {
                        var playing = _playing == id;
                        var button = new Button(() => Toggle(id)) { text = Loc.T(playing ? "music.stop" : "music.play") };
                        button.AddToClassList("ending-button");
                        card.Add(button);
                    }
                    else
                    {
                        var note = new Label(Loc.T("music.no_clip"));
                        note.AddToClassList("gallery-info");
                        card.Add(note);
                    }
                }

                _list.Add(card);
            }

            KoreanFont.ApplyDeep(_list);
        }

        private void Toggle(string id)
        {
            if (_playing == id)
            {
                Sound.StopLoop(CdPlayer.Channel);
                _playing = "";
            }
            else
            {
                CdPlayer.Preview(id);
                _playing = id;
            }

            Fill();
        }
    }
}
