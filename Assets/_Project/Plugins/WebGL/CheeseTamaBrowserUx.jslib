mergeInto(LibraryManager.library, {
  $CheeseTamaBrowserUx: {
    canvas: null,
    contextMenuHandler: null,
    pointerDownHandler: null,

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
  }
});
