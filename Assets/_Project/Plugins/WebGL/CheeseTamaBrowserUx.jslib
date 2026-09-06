mergeInto(LibraryManager.library, {
  $CheeseTamaBrowserUx: {
    canvas: null,
    contextMenuHandler: null,
    pointerDownHandler: null,
    returnPending: false,

    install: function () {
      var canvas = Module['canvas'];
      if (!canvas || CheeseTamaBrowserUx.canvas === canvas) {
        return;
      }

      CheeseTamaBrowserUx.remove();
      CheeseTamaBrowserUx.canvas = canvas;
      CheeseTamaBrowserUx.contextMenuHandler = function (event) {
        event.preventDefault();
      };
      CheeseTamaBrowserUx.pointerDownHandler = function () {
        if (document.activeElement === canvas || typeof canvas.focus !== 'function') {
          return;
        }

        try {
          canvas.focus({ preventScroll: true });
        } catch (error) {
          canvas.focus();
        }
      };

      canvas.addEventListener('contextmenu', CheeseTamaBrowserUx.contextMenuHandler);
      canvas.addEventListener('pointerdown', CheeseTamaBrowserUx.pointerDownHandler);

      if (Module['deinitializers']) {
        Module['deinitializers'].push(CheeseTamaBrowserUx.remove);
      }
    },

    remove: function () {
      var canvas = CheeseTamaBrowserUx.canvas;
      if (canvas) {
        canvas.removeEventListener('contextmenu', CheeseTamaBrowserUx.contextMenuHandler);
        canvas.removeEventListener('pointerdown', CheeseTamaBrowserUx.pointerDownHandler);
      }

      CheeseTamaBrowserUx.canvas = null;
      CheeseTamaBrowserUx.contextMenuHandler = null;
      CheeseTamaBrowserUx.pointerDownHandler = null;
    }
  },

  CheeseTamaInstallBrowserUx__deps: ['$CheeseTamaBrowserUx'],
  CheeseTamaInstallBrowserUx: function () {
    CheeseTamaBrowserUx.install();
  },

  CheeseTamaReturnToGameStart__deps: ['$CheeseTamaBrowserUx'],
  CheeseTamaReturnToGameStart: function () {
    if (CheeseTamaBrowserUx.returnPending) {
      return;
    }

    CheeseTamaBrowserUx.returnPending = true;
    var hasReturned = false;
    var returnToGameStart = function () {
      if (hasReturned) {
        return;
      }

      hasReturned = true;
      if (typeof window.CheeseTamaReturnToGameStart === 'function') {
        window.CheeseTamaReturnToGameStart();
        return;
      }

      window.location.reload();
    };

    // SaveGame closes the virtual file before this bridge runs. Wait for an explicit
    // memory-to-IndexedDB flush so a slow browser cannot lose the last change on reload.
    // The timeout keeps the home action recoverable if a browser storage callback stalls.
    window.setTimeout(returnToGameStart, 5000);
    if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
      window.setTimeout(returnToGameStart, 250);
      return;
    }

    try {
      FS.syncfs(false, function (error) {
        if (error) {
          console.error('[CheeseTama] Final browser save synchronization failed.');
        }

        returnToGameStart();
      });
    } catch (error) {
      console.error('[CheeseTama] Final browser save synchronization could not start.');
      returnToGameStart();
    }
  }
});
