mergeInto(LibraryManager.library, {
  $CheeseTamaSaveTransfer: {
    send: function (gameObjectName, methodName, value) {
      if (typeof SendMessage !== 'function') {
        console.error('[CheeseTama] Save-transfer callback is unavailable.');
        return;
      }

      SendMessage(gameObjectName, methodName, value || '');
    }
  },

  CheeseTamaDownloadSaveTransfer__deps: ['$CheeseTamaSaveTransfer'],
  CheeseTamaDownloadSaveTransfer: function (contentsPointer, fileNamePointer) {
    try {
      var contents = UTF8ToString(contentsPointer);
      var fileName = UTF8ToString(fileNamePointer);
      if (!contents || !fileName) {
        return 0;
      }

      var blob = new Blob([contents], { type: 'application/json;charset=utf-8' });
      var objectUrl = URL.createObjectURL(blob);
      var anchor = document.createElement('a');
      anchor.href = objectUrl;
      anchor.download = fileName;
      anchor.style.display = 'none';
      document.body.appendChild(anchor);
      anchor.click();
      document.body.removeChild(anchor);
      setTimeout(function () {
        URL.revokeObjectURL(objectUrl);
      }, 0);
      return 1;
    } catch (error) {
      console.error('[CheeseTama] Save-transfer download failed.');
      return 0;
    }
  },

  CheeseTamaPickSaveTransfer__deps: ['$CheeseTamaSaveTransfer'],
  CheeseTamaPickSaveTransfer: function (
    gameObjectNamePointer,
    successMethodNamePointer,
    failureMethodNamePointer,
    maximumBytes
  ) {
    var gameObjectName = UTF8ToString(gameObjectNamePointer);
    var successMethodName = UTF8ToString(successMethodNamePointer);
    var failureMethodName = UTF8ToString(failureMethodNamePointer);

    try {
      var input = document.createElement('input');
      input.type = 'file';
      input.accept = '.ctsave.json,.json,application/json';
      input.style.display = 'none';

      var settled = false;
      var cancellationTimeout = 0;
      var handleWindowFocus = null;

      var cleanup = function () {
        if (cancellationTimeout) {
          clearTimeout(cancellationTimeout);
          cancellationTimeout = 0;
        }
        if (handleWindowFocus) {
          window.removeEventListener('focus', handleWindowFocus);
        }
        if (input.parentNode) {
          input.parentNode.removeChild(input);
        }
      };

      var fail = function (message) {
        if (settled) {
          return;
        }

        settled = true;
        CheeseTamaSaveTransfer.send(gameObjectName, failureMethodName, message);
        cleanup();
      };

      var succeed = function (contents) {
        if (settled) {
          return;
        }

        settled = true;
        CheeseTamaSaveTransfer.send(gameObjectName, successMethodName, contents);
        cleanup();
      };

      handleWindowFocus = function () {
        // Some browsers do not emit the input cancel event. Let a pending change
        // event run first, then treat an empty selection after focus restoration
        // as cancellation so the managed bridge never stays permanently busy.
        setTimeout(function () {
          if (!settled && (!input.files || input.files.length === 0)) {
            fail('백업 파일 선택을 취소했습니다.');
          }
        }, 300);
      };
      window.addEventListener('focus', handleWindowFocus);

      input.addEventListener('cancel', function () {
        fail('백업 파일 선택을 취소했습니다.');
      });

      input.addEventListener('change', function () {
        var file = input.files && input.files.length > 0 ? input.files[0] : null;
        if (!file) {
          fail('백업 파일 선택을 취소했습니다.');
          return;
        }

        if (file.size <= 0 || file.size > maximumBytes) {
          fail('선택한 백업 파일의 용량이 올바르지 않습니다.');
          return;
        }

        var reader = new FileReader();
        reader.addEventListener('load', function () {
          succeed(typeof reader.result === 'string' ? reader.result : '');
        });
        reader.addEventListener('error', function () {
          fail('브라우저에서 백업 파일을 읽지 못했습니다.');
        });
        reader.readAsText(file, 'utf-8');
      });

      document.body.appendChild(input);
      cancellationTimeout = setTimeout(function () {
        fail('백업 파일 선택 시간이 만료되었습니다. 다시 시도해 주세요.');
      }, 300000);
      input.click();
    } catch (error) {
      if (typeof fail === 'function') {
        fail('브라우저 파일 선택 창을 열지 못했습니다.');
      } else {
        CheeseTamaSaveTransfer.send(
          gameObjectName,
          failureMethodName,
          '브라우저 파일 선택 창을 열지 못했습니다.'
        );
      }
    }
  }
});
