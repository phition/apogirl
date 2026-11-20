using System.IO;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 받은 그림(M6-5)의 가져오기 설정 (Resources/Art 아래 폴더별).
    /// - ExplorationHD: 스프라이트, PPU 72 (코드 그림 18 PPU의 4배 픽셀 = 같은 세계 크기), 점 필터, 무압축, 피벗은 같은 키의 코드 그림을 따른다.
    /// - Story·Portraits·UI: UI Toolkit 배경용 텍스처 (밉맵 없음, 최대 2048).
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Resources/Art/";
        private const string HdFolder = Root + "ExplorationHD/";
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

            if (path.StartsWith(HdFolder))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = HdPixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = PivotFor(Path.GetFileNameWithoutExtension(path));
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

            return new Vector2(0.5f, 0f);
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
