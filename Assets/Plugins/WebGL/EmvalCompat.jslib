// Compatibility shim for Emscripten 4.x (Unity 6000.6+).
// com.unity.dt.app-ui's prebuilt libAppUITextMateLib.a was compiled with an older
// Emscripten whose embind used _emval_take_value / _emval_as. Those were removed in
// Emscripten 4.x, causing "wasm-ld: undefined symbol: _emval_take_value / _emval_as".
// These re-implement them on top of the current embind internals.
// Requires the -lembind linker flag (added by Assets/Editor/WebGLEmbindFix.cs).
mergeInto(LibraryManager.library, {
  _emval_take_value__deps: ['$Emval', '$requireRegisteredType'],
  _emval_take_value__sig: 'ppp',
  _emval_take_value: function (type, arg) {
    type = requireRegisteredType(type, '_emval_take_value');
    var v = type.readValueFromPointer(arg);
    return Emval.toHandle(v);
  },

  _emval_as__deps: ['$Emval', '$requireRegisteredType', '$emval_returnValue'],
  _emval_as__sig: 'dppp',
  _emval_as: function (handle, returnType, destructorsRef) {
    handle = Emval.toValue(handle);
    returnType = requireRegisteredType(returnType, 'emval::as');
    return emval_returnValue(returnType.toWireType.bind(returnType), destructorsRef, handle);
  },
});
