using CheeseTama.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTama.Tests.EditMode
{
    public sealed class KoreanWebFontFeatureTests
    {
        [Test]
        public void BundledFontContainsRequiredKoreanGlyphs()
        {
            var font = Resources.Load<Font>(KoreanUiFontRuntime.ResourcePath);

            Assert.That(font, Is.Not.Null);
            foreach (var character in "한글치즈타마우유도감설정")
            {
                Assert.That(
                    font.HasCharacter(character),
                    Is.True,
                    $"번들 글꼴에 필요한 한국어 글리프가 없습니다: {character}");
            }
        }

        [Test]
        public void RuntimePassReplacesInactiveSceneTextFont()
        {
            var root = new GameObject("Korean Font Test Root");
            try
            {
                var label = root.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                root.SetActive(false);

                var changed = KoreanUiFontRuntime.ApplyToLoadedTextComponents();

                Assert.That(changed, Is.GreaterThanOrEqualTo(1));
                Assert.That(label.font, Is.SameAs(KoreanUiFontRuntime.GetDefaultFont()));
                Assert.That(label.font.HasCharacter('한'), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
