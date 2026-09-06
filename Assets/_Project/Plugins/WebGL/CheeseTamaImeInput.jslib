mergeInto(LibraryManager.library, {
  $CheeseTamaImeInput: {
    element: null,
    targetName: '',
    inputHandler: null,
    blurHandler: null,
    keydownHandler: null,

    close: function (requestedTarget) {
      if (requestedTarget
          && CheeseTamaImeInput.targetName
          && requestedTarget !== CheeseTamaImeInput.targetName) {
        return;
      }

      var input = CheeseTamaImeInput.element;
      if (input) {
        input.removeEventListener('input', CheeseTamaImeInput.inputHandler);
        input.removeEventListener('blur', CheeseTamaImeInput.blurHandler);
        input.removeEventListener('keydown', CheeseTamaImeInput.keydownHandler);
        if (input.parentNode) {
          input.parentNode.removeChild(input);
        }
      }

      CheeseTamaImeInput.element = null;
      CheeseTamaImeInput.targetName = '';
      CheeseTamaImeInput.inputHandler = null;
      CheeseTamaImeInput.blurHandler = null;
      CheeseTamaImeInput.keydownHandler = null;
    },

    open: function (targetName, initialValue) {
      CheeseTamaImeInput.close('');

      var input = document.createElement('input');
      input.type = 'text';
      input.value = initialValue || '';
      input.autocomplete = 'off';
      input.autocapitalize = 'off';
      input.spellcheck = false;
      input.setAttribute('aria-hidden', 'true');
      input.setAttribute('data-cheesetama-ime', 'true');
      input.style.position = 'fixed';
      input.style.left = '50%';
      input.style.bottom = '12px';
      input.style.width = 'min(480px, calc(100vw - 32px))';
      input.style.height = '40px';
      input.style.transform = 'translateX(-50%)';
      input.style.opacity = '0.01';
      input.style.pointerEvents = 'none';
      input.style.zIndex = '2147483647';

      CheeseTamaImeInput.targetName = targetName;
      CheeseTamaImeInput.inputHandler = function () {
        if (typeof SendMessage !== 'function') {
          return;
        }

        SendMessage(
          CheeseTamaImeInput.targetName,
          'ApplyWebGlImeText',
          input.value || ''
        );
      };
      CheeseTamaImeInput.blurHandler = function () {
        if (typeof SendMessage === 'function') {
          SendMessage(targetName, 'ReleaseWebGlImeCapture', '');
        }
        setTimeout(function () {
          if (CheeseTamaImeInput.element === input
              && document.activeElement !== input) {
            CheeseTamaImeInput.close(targetName);
          }
        }, 0);
      };
      CheeseTamaImeInput.keydownHandler = function (event) {
        if (event.key !== 'Escape') {
          return;
        }

        event.preventDefault();
        event.stopPropagation();
        if (typeof SendMessage === 'function') {
          SendMessage(targetName, 'HandleWebGlImeEscape', '');
        }
        CheeseTamaImeInput.close(targetName);
      };
      input.addEventListener('input', CheeseTamaImeInput.inputHandler);
      input.addEventListener('blur', CheeseTamaImeInput.blurHandler);
      input.addEventListener('keydown', CheeseTamaImeInput.keydownHandler);
      document.body.appendChild(input);
      CheeseTamaImeInput.element = input;
      try {
        input.focus({ preventScroll: true });
      } catch (error) {
        input.focus();
      }
      input.setSelectionRange(input.value.length, input.value.length);
    }
  },

  CheeseTamaOpenImeInput__deps: ['$CheeseTamaImeInput'],
  CheeseTamaOpenImeInput: function (gameObjectNamePointer, initialValuePointer) {
    CheeseTamaImeInput.open(
      UTF8ToString(gameObjectNamePointer),
      UTF8ToString(initialValuePointer)
    );
  },

  CheeseTamaCloseImeInput__deps: ['$CheeseTamaImeInput'],
  CheeseTamaCloseImeInput: function (gameObjectNamePointer) {
    CheeseTamaImeInput.close(UTF8ToString(gameObjectNamePointer));
  }
});
