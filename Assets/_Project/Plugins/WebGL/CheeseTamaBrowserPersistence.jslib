mergeInto(LibraryManager.library, {
  $CheeseTamaBrowserPersistence: {
    inFlight: false,
    pending: false,

    request: function () {
      // Unity's project build postprocessor enables this option for the normal web
      // artifact. Auto-persist already coalesces IDBFS writes, so do not start a
      // second synchronization pipeline when it is active.
      if (typeof Module !== 'undefined' && Module.autoSyncPersistentDataPath) {
        return;
      }

      if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
        console.error('[CheeseTama] Browser save synchronization is unavailable.');
        return;
      }

      if (CheeseTamaBrowserPersistence.inFlight) {
        CheeseTamaBrowserPersistence.pending = true;
        return;
      }

      CheeseTamaBrowserPersistence.inFlight = true;
      FS.syncfs(false, function (error) {
        CheeseTamaBrowserPersistence.inFlight = false;
        if (error) {
          console.error('[CheeseTama] Browser save synchronization failed.');
        }

        if (CheeseTamaBrowserPersistence.pending) {
          CheeseTamaBrowserPersistence.pending = false;
          CheeseTamaBrowserPersistence.request();
        }
      });
    }
  },

  CheeseTamaSyncFileSystem__deps: ['$CheeseTamaBrowserPersistence'],
  CheeseTamaSyncFileSystem: function () {
    CheeseTamaBrowserPersistence.request();
  }
});
