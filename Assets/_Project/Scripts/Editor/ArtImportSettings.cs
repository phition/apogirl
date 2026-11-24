using System.IO;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 받은 그림(M6-5)의 가져오기 설정 (Resources/Art 아래 폴더별).
    /// - ExplorationPixel: 픽셀 아트(PixelLab), PPU 36, 나머지는 ExplorationHD와 같다.
    /// - ExplorationHD: 스프라이트, PPU 72 (코드 그림 18 PPU의 4배 픽셀 = 같은 세계 크기), 점 필터, 무압축, 피벗은 같은 키의 코드 그림을 따른다.
    /// - Story·Portraits·UI: UI Toolkit 배경용 텍스처 (밉맵 없음, 최대 2048).
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Resources/Art/";
        private const string HdFolder = Root + "ExplorationHD/";
        private const string PixelFolder = Root + "ExplorationPixel/";

        /// <summary>픽셀 아트 (PixelLab, M6-5): 캐릭터 64px = 1.78유닛.</summary>
        public const float PixelPixelsPerUnit = 36f;
        private const string GeneratedFolder = Root + "Exploration/";
        public const float HdPixelsPerUnit = 72f;

        private void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root) || path.StartsWith(GeneratedFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;

            if (path.StartsWith(HdFolder) || path.StartsWith(PixelFolder))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = path.StartsWith(PixelFolder) ? PixelPixelsPerUnit : HdPixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                var key = Path.GetFileNameWithoutExtension(path);
                settings.spritePivot = PivotFor(key);

                // 픽셀 아트 표 (sprite_meta.json): 그림마다 기준점·테두리(조각 반복용)를 따로 정할 수 있다
                if (path.StartsWith(PixelFolder) && TryGetMeta(key, path.IndexOf('/', PixelFolder.Length) < 0, out var meta))
                {
                    if (meta.Pivot != null && meta.Pivot.Length == 2)
                    {
                        settings.spritePivot = new Vector2(meta.Pivot[0], meta.Pivot[1]);
                    }

                    if (meta.Border != null && meta.Border.Length == 4)
                    {
                        settings.spriteBorder = new Vector4(meta.Border[0], meta.Border[1], meta.Border[2], meta.Border[3]);
                    }

                    // 2배 해상도 그림(192×156 등)은 PPU도 2배로 두어 세계 크기를 맞춘다 (settings가 PPU도 덮어쓰므로 거기에 넣는다)
                    if (meta.Ppu > 0f)
                    {
                        settings.spritePixelsPerUnit = meta.Ppu;
                    }
                }

                importer.SetTextureSettings(settings);
                return;
            }

            // UI 아이콘은 128~256 px 그림을 16~28 px로 줄여 그리므로 밉맵으로 계단 현상을 줄인다
            importer.mipmapEnabled = path.StartsWith(Root + "UI/");
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = path.EndsWith(".png");
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            // 허브 단면도 뒷배경(PixelLab 픽셀 아트): 정수 배로 그리므로 점 필터, 밉맵·압축 없이
            if (path.StartsWith(Root + "UI/HubSky/"))
            {
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }

        /// <summary>
        /// 받은 소리 (Resources/Audio): 효과음은 짧아서 불러올 때 풀어 두고(바로 재생), 음성은 압축한 채 메모리에 둔다.
        /// </summary>
        private void OnPreprocessAudio()
        {
            var path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(AudioRoot))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.loadType = path.StartsWith(AudioRoot + "Voice/") ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = path.StartsWith(AudioRoot + "Voice/");
        }

        private const string AudioRoot = "Assets/_Project/Resources/Audio/";

        [System.Serializable]
        private sealed class SpriteMeta
        {
            public string Key = "";

            /// <summary>기준점 (0~1, x·y). 비면 코드 그림을 따르거나 아래 가운데.</summary>
            public float[] Pivot;

            /// <summary>테두리 픽셀 (왼쪽·아래·오른쪽·위). 있으면 늘리지 않고 가운데 조각을 반복해 그린다 (창문).</summary>
            public float[] Border;

            /// <summary>화소/유닛. 0이면 폴더 기본값(36). 2배 해상도 그림은 72.</summary>
            public float Ppu;
        }

        [System.Serializable]
        private sealed class SpriteMetaFile
        {
            public SpriteMeta[] Sprites = System.Array.Empty<SpriteMeta>();
        }

        private const string MetaPath = PixelFolder + "sprite_meta.json";
        private static System.Collections.Generic.Dictionary<string, SpriteMeta> _meta;
        private static System.DateTime _metaTime;

        private static bool TryGetMeta(string key, bool allowPrefix, out SpriteMeta meta)
        {
            meta = null;
            if (!File.Exists(MetaPath))
            {
                return false;
            }

            var time = File.GetLastWriteTimeUtc(MetaPath);
            if (_meta == null || time != _metaTime)
            {
                _meta = new System.Collections.Generic.Dictionary<string, SpriteMeta>();
                _metaTime = time;
                var file = JsonUtility.FromJson<SpriteMetaFile>(File.ReadAllText(MetaPath));
                if (file != null)
                {
                    foreach (var entry in file.Sprites)
                    {
                        _meta[entry.Key] = entry;
                    }
                }
            }

            if (_meta.TryGetValue(key, out meta) || !allowPrefix)
            {
                return meta != null;
            }

            // 키 끝이 '*'인 줄은 앞부분이 같은 모든 그림에 적용한다 (가장 긴 앞부분이 이긴다). 하위 폴더(Hub/ 등)에는 적용하지 않는다
            var best = -1;
            foreach (var pair in _meta)
            {
                var k = pair.Key;
                if (k.EndsWith("*") && k.Length - 1 > best && key.StartsWith(k.Substring(0, k.Length - 1)))
                {
                    best = k.Length - 1;
                    meta = pair.Value;
                }
            }

            return meta != null;
        }

        /// <summary>같은 키(또는 대표 프레임)의 코드 그림 피벗. 없으면 아래 가운데.</summary>
        private static Vector2 PivotFor(string key)
        {
            var generated = key;
            if (key.EndsWith("_dormant")) generated = key + "_0";
            else if (key.StartsWith("zombie_") && key.EndsWith("_idle")) generated = key.Replace("_idle", "_walk_0");

            if (AssetImporter.GetAtPath(GeneratedFolder + generated + ".png") is TextureImporter source)
            {
                var settings = new TextureImporterSettings();
                source.ReadTextureSettings(settings);
                return settings.spriteAlignment == (int)SpriteAlignment.Custom ? settings.spritePivot : AlignmentPivot((SpriteAlignment)settings.spriteAlignment);
            }

            // 코드 그림에 없는 새 키: 벽 타일·땅은 왼쪽 아래(타일 반복·벽 장식 배치 기준), 나머지는 아래 가운데
            return key.StartsWith("wall_") || key == "ground" ? Vector2.zero : new Vector2(0.5f, 0f);
        }

        private static Vector2 AlignmentPivot(SpriteAlignment alignment)
        {
            switch (alignment)
            {
                case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
                case SpriteAlignment.BottomLeft: return new Vector2(0f, 0f);
                case SpriteAlignment.TopCenter: return new Vector2(0.5f, 1f);
                case SpriteAlignment.Center: return new Vector2(0.5f, 0.5f);
                default: return new Vector2(0.5f, 0f);
            }
        }
    }
}
