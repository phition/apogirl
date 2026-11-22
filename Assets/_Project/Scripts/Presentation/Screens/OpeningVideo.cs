using System;
using System.Collections;
using System.IO;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 새 게임 오프닝 영상 (힉스필드 6차 등굣길): 프롤로그 대화 첫 줄 바로 앞에 한 번 튼다.
    /// 파일은 StreamingAssets (설정 Resources/Art/opening_video.json). 아무 키·클릭·터치로 건너뛰고,
    /// 2회차부터는 처음부터 건너뛰기 단추가 보인다. 끝나면 검은 화면으로 잦아들고, 그 아래에서 프롤로그가 시작되며
    /// <see cref="Reveal"/>로 검은 막을 걷는다. 파일이 없거나 재생에 실패하면 조용히 넘어간다.
    /// </summary>
    internal sealed class OpeningVideo
    {
        [Serializable]
        private sealed class Config
        {
            public string File = "";
            public float FadeIn = 0.6f;
            public float FadeOut = 1.2f;
            public float HoldBlack = 0.4f;
            public float Reveal = 1f;
            public float HintDelay = 2.5f;
            public float PrepareTimeout = 6f;

            /// <summary>프롤로그 대화 뒤 배경 (Story/ 아래 경로). 영상 마지막 교문 컷에서 이어진다.</summary>
            public string PrologueBackdrop = "";
        }

        private readonly VisualElement _root;
        private readonly bool _skipButton;
        private readonly Config _config;
        private VisualElement _curtain;
        private bool _skipped;

        public OpeningVideo(VisualElement root, bool skipButton)
        {
            _root = root;
            _skipButton = skipButton;
            var text = Resources.Load<TextAsset>("Art/opening_video");
            _config = text != null ? JsonUtility.FromJson<Config>(text.text) : null;
        }

        /// <summary>프롤로그 배경 그림 (없으면 null).</summary>
        public Texture2D PrologueBackdrop => string.IsNullOrEmpty(_config?.PrologueBackdrop) ? null : Art.StoryArt.Story(_config.PrologueBackdrop);

        /// <summary>영상을 끝까지(또는 건너뛸 때까지) 틀고, 검은 막을 덮은 채로 돌아온다. 영상이 없으면 막 없이 바로 돌아온다.</summary>
        public IEnumerator Play()
        {
            if (_config == null || string.IsNullOrEmpty(_config.File))
            {
                yield break;
            }

            var path = Path.Combine(Application.streamingAssetsPath, _config.File);
            if (Application.platform != RuntimePlatform.Android && !File.Exists(path))
            {
                Debug.LogWarning("오프닝 영상 없음: " + path);
                yield break;
            }

            // 검은 막 + 영상 그림
            _curtain = new VisualElement { name = "opening-video", pickingMode = PickingMode.Position };
            _curtain.style.position = Position.Absolute;
            _curtain.style.left = 0;
            _curtain.style.top = 0;
            _curtain.style.right = 0;
            _curtain.style.bottom = 0;
            _curtain.style.backgroundColor = Color.black;
            var image = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.flexGrow = 1;
            image.style.opacity = 0f;
            _curtain.Add(image);
            var hint = new Label(Loc.T(_skipButton ? "opening.skip" : "opening.skip_hint"));
            hint.AddToClassList("opening-skip");
            hint.style.position = Position.Absolute;
            hint.style.right = 32;
            hint.style.bottom = 24;
            hint.style.color = new Color(1f, 1f, 1f, 0.75f);
            hint.style.fontSize = 18;
            hint.style.opacity = _skipButton ? 1f : 0f;
            if (_skipButton)
            {
                hint.style.paddingLeft = 14;
                hint.style.paddingRight = 14;
                hint.style.paddingTop = 6;
                hint.style.paddingBottom = 6;
                hint.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
                hint.style.borderTopLeftRadius = 6;
                hint.style.borderTopRightRadius = 6;
                hint.style.borderBottomLeftRadius = 6;
                hint.style.borderBottomRightRadius = 6;
            }

            _curtain.Add(hint);
            _curtain.RegisterCallback<PointerDownEvent>(_ => _skipped = true);
            _root.Add(_curtain);

            var host = new GameObject("OpeningVideo");
            var player = host.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.skipOnDrop = true;
            player.source = VideoSource.Url;
            player.url = path;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            var failed = false;
            var ended = false;
            player.errorReceived += (_, message) =>
            {
                Debug.LogWarning("오프닝 영상 재생 실패: " + message);
                failed = true;
            };
            player.loopPointReached += _ => ended = true;

            player.Prepare();
            var waited = 0f;
            while (!player.isPrepared && !failed && waited < _config.PrepareTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!player.isPrepared || failed)
            {
                UnityEngine.Object.Destroy(host);
                _curtain.RemoveFromHierarchy();
                _curtain = null;
                yield break;
            }

            var texture = new RenderTexture((int)Math.Max(16, player.width), (int)Math.Max(16, player.height), 0);
            player.targetTexture = texture;
            image.image = texture;
            var volume = Audio.Sound.MasterVolume * Audio.Sound.CategoryVolume(Audio.SoundCategory.Music);
            if (player.audioTrackCount > 0)
            {
                player.EnableAudioTrack(0, true);
                player.SetDirectAudioVolume(0, volume);
            }

            player.Play();
            var time = 0f;
            while (!ended && !failed && !_skipped)
            {
                time += Time.unscaledDeltaTime;
                image.style.opacity = Mathf.Clamp01(time / Mathf.Max(0.01f, _config.FadeIn));
                if (!_skipButton)
                {
                    hint.style.opacity = Mathf.Clamp01((time - _config.HintDelay) / 0.5f) * 0.8f;
                }

                _skipped |= AnyInput();
                if (time > 1f && !player.isPlaying && player.frame > 0)
                {
                    ended = true;   // 끝 알림을 놓친 경우
                }

                yield return null;
            }

            // 화면·소리를 함께 검게 잦아들게
            hint.style.opacity = 0f;
            var start = image.resolvedStyle.opacity;
            for (var t = 0f; t < _config.FadeOut; t += Time.unscaledDeltaTime)
            {
                var k = 1f - t / _config.FadeOut;
                image.style.opacity = start * k;
                if (player.audioTrackCount > 0)
                {
                    player.SetDirectAudioVolume(0, volume * k);
                }

                yield return null;
            }

            image.style.opacity = 0f;
            player.Stop();
            UnityEngine.Object.Destroy(host);
            texture.Release();
            UnityEngine.Object.Destroy(texture);
            for (var t = 0f; t < _config.HoldBlack; t += Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        /// <summary>검은 막을 걷는다 (그 아래에서 프롤로그가 이미 시작된 뒤).</summary>
        public IEnumerator Reveal()
        {
            var curtain = _curtain;
            if (curtain == null)
            {
                yield break;
            }

            _curtain = null;
            curtain.pickingMode = PickingMode.Ignore;
            for (var t = 0f; t < _config.Reveal; t += Time.unscaledDeltaTime)
            {
                curtain.style.opacity = 1f - t / _config.Reveal;
                yield return null;
            }

            curtain.RemoveFromHierarchy();
        }

        private static bool AnyInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }
    }
}
