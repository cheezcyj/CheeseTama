using CheeseTama.Platform;
using NUnit.Framework;
using UnityEngine;

namespace CheeseTama.Tests.EditMode
{
    public sealed class WebPlatformPersistencePolicyTests
    {
        [Test]
        public void WebPlayerUsesBrowserLocalStorageAndHidesSteamCloud()
        {
            var capabilities = RuntimePlatformCapabilities.Resolve(RuntimePlatform.WebGLPlayer);

            Assert.That(capabilities.UsesBrowserLocalStorage, Is.True);
            Assert.That(capabilities.RequiresFileSystemSync, Is.True);
            Assert.That(capabilities.SupportsSteamCloudUi, Is.False);
            Assert.That(
                capabilities.LocalStorageLabel,
                Is.EqualTo(RuntimePlatformCapabilities.BrowserLocalStorageLabel));
            Assert.That(RuntimePlatformCapabilities.BrowserLocalStorageNotice, Does.Contain("브라우저"));
            Assert.That(RuntimePlatformCapabilities.BrowserLocalStorageNotice, Does.Not.Contain("Steam"));
        }

        [Test]
        public void DesktopPlayerDoesNotUseBrowserFileSystemSync()
        {
            var capabilities = RuntimePlatformCapabilities.Resolve(RuntimePlatform.WindowsPlayer);

            Assert.That(capabilities.UsesBrowserLocalStorage, Is.False);
            Assert.That(capabilities.RequiresFileSystemSync, Is.False);
            Assert.That(
                capabilities.LocalStorageLabel,
                Is.EqualTo(RuntimePlatformCapabilities.DeviceLocalStorageLabel));
        }

        [Test]
        public void EditorSyncRequestIsSafeNoOp()
        {
            Assert.That(BrowserPersistence.RequiresExplicitSync, Is.False);
            Assert.DoesNotThrow(BrowserPersistence.RequestSync);
        }
    }
}
