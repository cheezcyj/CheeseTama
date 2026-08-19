using System;
using CheeseTama.Platform;
using NUnit.Framework;

namespace CheeseTama.Tests.EditMode
{
    public sealed class SteamCloudBoundaryTests
    {
#if !STEAMWORKS_NET || UNITY_WEBGL
        [Test]
        public void PlatformRuntimeFailsClosedWhenSteamworksSdkIsAbsent()
        {
            Assert.That(SteamPlatformRuntime.EnsureInitialized(), Is.False);
            Assert.That(SteamPlatformRuntime.Status, Is.EqualTo(SteamPlatformRuntimeStatus.SdkUnavailable));
        }
#endif

        private static readonly DateTimeOffset BaselineTime =
            new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void FactoryUsesLocalFallbackWhenSteamworksDefineIsAbsent()
        {
            var provider = SteamCloudProviderFactory.CreateDefault();

#if STEAMWORKS_NET && !UNITY_WEBGL
            Assert.That(provider.ProviderName, Is.EqualTo("Steamworks.NET"));
#else
            Assert.That(provider, Is.TypeOf<LocalOnlyCloudSaveProvider>());
            Assert.That(provider.Availability, Is.EqualTo(CloudProviderAvailability.Offline));
#endif
        }

        [Test]
        public void OfflineProviderPreservesLocalWithoutRemoteCalls()
        {
            var provider = new FakeCloudProvider { AvailabilityValue = CloudProviderAvailability.Offline };
            var local = CreatePayload("local", 3, BaselineTime);

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.KeptLocalOffline));
            Assert.That(result.EffectiveLocal, Is.SameAs(local));
            Assert.That(provider.DownloadCount, Is.Zero);
            Assert.That(provider.UploadCount, Is.Zero);
        }

        [Test]
        public void MissingRemoteUploadsValidLocalPayload()
        {
            var provider = new FakeCloudProvider();
            var local = CreatePayload("local", 1, BaselineTime);

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.UploadedLocal));
            Assert.That(provider.UploadCount, Is.EqualTo(1));
            Assert.That(provider.UploadedPayload, Is.SameAs(local));
        }

        [Test]
        public void NewerRemoteIsReturnedForExplicitLocalWrite()
        {
            var local = CreatePayload("local", 2, BaselineTime);
            var remote = CreatePayload("remote", 3, BaselineTime.AddMinutes(1));
            var provider = new FakeCloudProvider { RemotePayload = remote };

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.DownloadedRemote));
            Assert.That(result.RequiresLocalWrite, Is.True);
            Assert.That(result.EffectiveLocal, Is.SameAs(remote));
            Assert.That(provider.UploadCount, Is.Zero);
        }

        [Test]
        public void NewerLocalUploadsWithoutDiscardingLocalCopy()
        {
            var local = CreatePayload("local", 4, BaselineTime.AddMinutes(2));
            var remote = CreatePayload("remote", 3, BaselineTime);
            var provider = new FakeCloudProvider { RemotePayload = remote };

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.UploadedLocal));
            Assert.That(result.EffectiveLocal, Is.SameAs(local));
            Assert.That(provider.UploadedPayload, Is.SameAs(local));
        }

        [Test]
        public void EqualRecencyWithDifferentContentDoesNotOverwriteEitherCopy()
        {
            var local = CreatePayload("local", 5, BaselineTime);
            var remote = CreatePayload("remote", 5, BaselineTime);
            var provider = new FakeCloudProvider { RemotePayload = remote };

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.ConflictNeedsResolution));
            Assert.That(result.RequiresUserChoice, Is.True);
            Assert.That(result.EffectiveLocal, Is.SameAs(local));
            Assert.That(result.Remote, Is.SameAs(remote));
            Assert.That(provider.UploadCount, Is.Zero);
        }

        [Test]
        public void InvalidSlotCannotReachProvider()
        {
            var provider = new FakeCloudProvider();
            var local = CloudSavePayload.Create("../primary", "local", 1, BaselineTime);

            var result = new CloudSaveSyncCoordinator().Synchronize(local, provider);

            Assert.That(result.Action, Is.EqualTo(CloudSyncAction.InvalidLocal));
            Assert.That(provider.DownloadCount, Is.Zero);
            Assert.That(provider.UploadCount, Is.Zero);
        }

        private static CloudSavePayload CreatePayload(
            string content,
            long revision,
            DateTimeOffset modifiedUtc)
        {
            return CloudSavePayload.Create(
                CloudSaveSlotRules.PrimarySlotId,
                content,
                revision,
                modifiedUtc);
        }

        private sealed class FakeCloudProvider : ICloudSaveProvider
        {
            public CloudProviderAvailability AvailabilityValue = CloudProviderAvailability.Available;
            public CloudSavePayload RemotePayload;
            public CloudSavePayload UploadedPayload;
            public int UploadCount;
            public int DownloadCount;

            public string ProviderName => "Fake";
            public CloudProviderAvailability Availability => AvailabilityValue;

            public CloudTransferResult Upload(CloudSavePayload payload)
            {
                UploadCount += 1;
                UploadedPayload = payload;
                return CloudTransferResult.Success();
            }

            public CloudTransferResult Download(string slotId)
            {
                DownloadCount += 1;
                return RemotePayload == null
                    ? CloudTransferResult.NotFound()
                    : CloudTransferResult.Success(RemotePayload);
            }
        }
    }
}
