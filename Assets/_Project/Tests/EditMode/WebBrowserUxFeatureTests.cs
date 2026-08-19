using CheeseTama.Platform;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class WebBrowserUxFeatureTests
    {
        [Test]
        public void WebPlayerUsesCanvasKeyboardAndContextMenuGuards()
        {
            var capabilities = WebBrowserUxRuntime.Resolve(RuntimePlatform.WebGLPlayer);

            Assert.That(capabilities.CapturesAllKeyboardInput, Is.True);
            Assert.That(capabilities.BlocksCanvasContextMenu, Is.True);
            Assert.That(capabilities.EscapeMayBeConsumedByBrowser, Is.True);
        }

        [Test]
        public void DesktopPlayerDoesNotInstallBrowserCanvasGuards()
        {
            var capabilities = WebBrowserUxRuntime.Resolve(RuntimePlatform.WindowsPlayer);

            Assert.That(capabilities.CapturesAllKeyboardInput, Is.False);
            Assert.That(capabilities.BlocksCanvasContextMenu, Is.False);
            Assert.That(capabilities.EscapeMayBeConsumedByBrowser, Is.False);
        }
    }
}
