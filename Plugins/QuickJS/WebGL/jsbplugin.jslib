/**
 * Build with the following command:
 * npx -p typescript tsc
 *
 * BEWARE: Using some syntaxes will make Emscripten fail while building
 * Such known syntaxes: Object spread (...), BigInt literals
 * The output is targeted for es5 as Emscripten only understands that syntax
 */
var UnityJSBPlugin = {
    $unityJsbState__postset: 'unityJsbState.atoms = unityJsbState.createAtoms();\n',
    $unityJsbState: {
        createObjectReferences: function () {
            var getTag = function (object) {
                if (object === undefined)
                    return 3 /* Tags.JS_TAG_UNDEFINED */;
                if (object === null)
                    return 2 /* Tags.JS_TAG_NULL */;
                if (typeof object === 'number')
                    return 7 /* Tags.JS_TAG_FLOAT64 */;
                if (typeof object === 'boolean')
                    return 1 /* Tags.JS_TAG_BOOL */;
                if (typeof object === 'symbol')
                    return -8 /* Tags.JS_TAG_SYMBOL */;
                if (typeof object === 'string')
                    return -7 /* Tags.JS_TAG_STRING */;
                if (typeof object === 'bigint')
                    return -10 /* Tags.JS_TAG_BIG_INT */;
                if (object instanceof Error)
                    return 6 /* Tags.JS_TAG_EXCEPTION */;
                return -1 /* Tags.JS_TAG_OBJECT */;
            };
            var record = {};
            var map = new Map();
            var payloadMap = new Map();
            var res = {
                record: record,
                lastId: 0,
                allocate: function (object) {
                    var ptr = _malloc(16 /* Sizes.JSValue */);
                    var id = res.push(object, ptr);
                    return [ptr, id];
                },
                batchAllocate: function (objects) {
                    var size = 16 /* Sizes.JSValue */;
                    var len = objects.length;
                    var arr = _malloc(size * len);
                    var ids = Array(len);
                    for (var index = 0; index < len; index++) {
                        var object = objects[index];
                        var id = res.push(object, arr + (index * size));
                        ids[index] = id;
                    }
                    return [arr, ids];
                },
                batchGet: function (ptrs, count) {
                    var size = 16 /* Sizes.JSValue */;
                    var arr = new Array(count);
                    for (var index = 0; index < count; index++) {
                        var object = res.get(ptrs + index * size);
                        arr[index] = object;
                    }
                    return arr;
                },
                push: function (object, ptr) {
                    if (typeof object === 'undefined') {
                        res.duplicateId(0, ptr);
                        return;
                    }
                    if (typeof object === 'number') {
                        if (typeof ptr === 'number') {
                            HEAPF64[ptr >> 3] = object;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(7 /* Tags.JS_TAG_FLOAT64 */);
                        }
                        return;
                    }
                    if (typeof object === 'boolean') {
                        if (typeof ptr === 'number') {
                            HEAP32[ptr >> 2] = object ? 1 : 0;
                            HEAP32[(ptr >> 2) + 1] = 0;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(1 /* Tags.JS_TAG_BOOL */);
                        }
                        return;
                    }
                    var foundId = map.get(object);
                    if (foundId > 0) {
                        res.duplicateId(foundId, ptr);
                        return foundId;
                    }
                    var id = ++res.lastId;
                    record[id] = {
                        id: id,
                        refCount: 0,
                        value: object,
                        tag: getTag(object),
                    };
                    map.set(object, id);
                    res.duplicateId(id, ptr);
                    return id;
                },
                get: function (val) {
                    var tag = Number(unityJsbState.HEAP64()[(val >> 3) + 1]);
                    if (tag === 0 /* Tags.JS_TAG_INT */) {
                        return HEAP32[val >> 2];
                    }
                    else if (tag === 1 /* Tags.JS_TAG_BOOL */) {
                        return !!HEAP32[val >> 2];
                    }
                    else if (tag === 7 /* Tags.JS_TAG_FLOAT64 */) {
                        return HEAPF64[val >> 3];
                    }
                    else {
                        var id = HEAP32[val >> 2];
                        if (id === 0)
                            return undefined;
                        var ho = record[id];
                        return ho.value;
                    }
                },
                getRecord: function (val) {
                    var tag = Number(unityJsbState.HEAP64()[(val >> 3) + 1]);
                    if (tag === 0 /* Tags.JS_TAG_INT */) {
                        var value = HEAP32[val >> 2];
                        return {
                            id: -1,
                            refCount: 0,
                            value: value,
                            tag: tag,
                        };
                    }
                    else if (tag === 1 /* Tags.JS_TAG_BOOL */) {
                        var boolValue = !!HEAP32[val >> 2];
                        return {
                            id: -1,
                            refCount: 0,
                            value: boolValue,
                            tag: tag,
                        };
                    }
                    else if (tag === 7 /* Tags.JS_TAG_FLOAT64 */) {
                        var value = HEAPF64[val >> 3];
                        return {
                            id: -1,
                            refCount: 0,
                            value: value,
                            tag: tag,
                        };
                    }
                    else {
                        var id = HEAP32[val >> 2];
                        if (id === 0)
                            return {
                                id: 0,
                                refCount: 0,
                                value: undefined,
                                tag: 3 /* Tags.JS_TAG_UNDEFINED */,
                                type: 0 /* BridgeObjectType.None */,
                                payload: -1,
                            };
                        var ho = record[id];
                        return ho;
                    }
                },
                duplicate: function (obj, ptr) {
                    var tag = Number(unityJsbState.HEAP64()[(obj >> 3) + 1]);
                    if (tag === 7 /* Tags.JS_TAG_FLOAT64 */) {
                        if (typeof ptr === 'number') {
                            var val = HEAPF64[(obj >> 3)];
                            HEAPF64[ptr >> 3] = val;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(tag);
                        }
                        return;
                    }
                    else if (tag === 0 /* Tags.JS_TAG_INT */) {
                        if (typeof ptr === 'number') {
                            var val = HEAP32[(obj >> 2)];
                            HEAP32[(ptr >> 2)] = val;
                            HEAP32[(ptr >> 2) + 1] = 0;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(tag);
                        }
                        return;
                    }
                    else if (tag === 1 /* Tags.JS_TAG_BOOL */) {
                        if (typeof ptr === 'number') {
                            var valBool = !!HEAP32[(obj >> 2)];
                            HEAP32[(ptr >> 2)] = valBool ? 1 : 0;
                            HEAP32[(ptr >> 2) + 1] = 0;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(tag);
                        }
                        return;
                    }
                    var id = HEAP32[obj >> 2];
                    res.duplicateId(id, ptr);
                },
                duplicateId: function (id, ptr) {
                    if (id === 0) {
                        if (typeof ptr === 'number') {
                            HEAP32[ptr >> 2] = 0;
                            HEAP32[(ptr >> 2) + 1] = 0;
                            unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(3 /* Tags.JS_TAG_UNDEFINED */);
                        }
                        return;
                    }
                    var ho = record[id];
                    ho.refCount += 1;
                    if (typeof ptr === 'number') {
                        HEAP32[ptr >> 2] = id;
                        HEAP32[(ptr >> 2) + 1] = 0;
                        unityJsbState.HEAP64()[(ptr >> 3) + 1] = BigInt(ho.tag);
                    }
                },
                pop: function (obj) {
                    var tag = Number(unityJsbState.HEAP64()[(obj >> 3) + 1]);
                    if (tag === 7 /* Tags.JS_TAG_FLOAT64 */
                        || tag === 0 /* Tags.JS_TAG_INT */
                        || tag === 1 /* Tags.JS_TAG_BOOL */)
                        return;
                    var id = HEAP32[obj >> 2];
                    res.popId(id);
                },
                popId: function (id) {
                    if (!id)
                        return;
                    var ho = record[id];
                    ho.refCount -= 1;
                    console.assert(ho.refCount >= 0);
                },
                deleteRecord: function (id) {
                    var rec = record[id];
                    delete record[id];
                    res.clearPayload(rec.value);
                    map.delete(rec.value);
                },
                payloadMap: payloadMap,
                setPayload: function (obj, type, payload) {
                    payloadMap.set(obj, {
                        type: type,
                        payload: payload,
                    });
                },
                getPayload: function (obj) {
                    var res = payloadMap.get(obj);
                    if (res)
                        return res;
                    else {
                        return {
                            type: 0 /* BridgeObjectType.None */,
                            payload: 0,
                        };
                    }
                },
                clearPayload: function (obj) {
                    payloadMap.delete(obj);
                },
            };
            return res;
        },
        createAtoms: function () {
            var record = {};
            var map = new Map();
            var res = {
                record: record,
                lastId: 0,
                get: function (ref) {
                    if (ref === 0)
                        return undefined;
                    return record[ref].value;
                },
                push: function (str) {
                    if (str === undefined)
                        return 0;
                    var mapped = map.get(str);
                    var id;
                    if (!mapped) {
                        id = ++res.lastId;
                        var item = record[id] = {
                            id: id,
                            value: str,
                            refCount: 1,
                        };
                        map.set(str, item);
                    }
                    else {
                        id = mapped.id;
                        mapped.refCount++;
                    }
                    return id;
                },
                pushId: function (id) {
                    if (id === 0)
                        return;
                    var recorded = record[id];
                    console.assert(!!recorded);
                    if (!recorded)
                        return 0;
                    recorded.refCount++;
                    return id;
                },
                pop: function (id) {
                    if (id === 0)
                        return;
                    var recorded = record[id];
                    console.assert(!!recorded);
                    if (!recorded)
                        return;
                    recorded.refCount--;
                    console.assert(recorded.refCount >= 0);
                    if (recorded.refCount == 0) {
                        map.delete(recorded.value);
                        delete record[id];
                    }
                },
            };
            return res;
        },
        stringify: function (ptr, bufferLength) { return (typeof UTF8ToString !== 'undefined' ? UTF8ToString : Pointer_stringify)(ptr, bufferLength); },
        bufferify: function (arg) {
            var bufferSize = lengthBytesUTF8(arg) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(arg, buffer, bufferSize);
            return [buffer, bufferSize];
        },
        dynCall: function () { return (typeof Runtime !== 'undefined' ? Runtime.dynCall : dynCall).apply(typeof Runtime !== 'undefined' ? Runtime : undefined, arguments); },
        runtimes: {},
        contexts: {},
        lastRuntimeId: 1,
        lastContextId: 1,
        getRuntime: function (rt) {
            var rtId = rt;
            return unityJsbState.runtimes[rtId];
        },
        getContext: function (ctx) {
            var ctxId = ctx;
            return unityJsbState.contexts[ctxId];
        },
        HEAP64: function () {
            return new BigInt64Array(HEAPF64.buffer);
        },
        HEAPU64: function () {
            return new BigUint64Array(HEAPF64.buffer);
        },
    },
    JSB_Init: function () {
        return 10 /* Constants.CS_JSB_VERSION */;
    },
    JSB_NewRuntime: function (finalizer) {
        // TODO: understand what to do with finalizer
        var id = unityJsbState.lastRuntimeId++;
        var refs = unityJsbState.createObjectReferences();
        unityJsbState.runtimes[id] = {
            id: id,
            contexts: {},
            refs: refs,
            isDestroyed: false,
            garbageCollect: function () {
                var lastId = refs.lastId;
                var record = refs.record;
                var aliveItemCount = 0;
                for (var index = 0; index <= lastId; index++) {
                    var element = record[index];
                    if (element) {
                        if (element.refCount <= 0) {
                            refs.deleteRecord(index);
                        }
                        else {
                            aliveItemCount++;
                        }
                    }
                }
                return aliveItemCount;
            },
        };
        return id;
    },
    JSB_GetRuntimeOpaque: function (rtId) {
        return unityJsbState.getRuntime(rtId).opaque;
    },
    JSB_SetRuntimeOpaque: function (rtId, opaque) {
        unityJsbState.getRuntime(rtId).opaque = opaque;
    },
    JS_GetContextOpaque: function (ctx) {
        return unityJsbState.getContext(ctx).opaque;
    },
    JS_SetContextOpaque: function (ctx, opaque) {
        unityJsbState.getContext(ctx).opaque = opaque;
    },
    JSB_FreeRuntime: function (rtId) {
        var runtime = unityJsbState.getRuntime(rtId);
        var ctxIds = Object.keys(runtime.contexts);
        for (var index = 0; index < ctxIds.length; index++) {
            var ctxId = ctxIds[index];
            var context = runtime.contexts[ctxId];
            context.free();
        }
        var aliveItemCount = runtime.garbageCollect();
        runtime.isDestroyed = true;
        delete unityJsbState.runtimes[runtime.id];
        return aliveItemCount === 0;
    },
    JS_GetRuntime: function (ctxId) {
        var context = unityJsbState.getContext(ctxId);
        return context.runtimeId;
    },
    JS_NewContext: function (rtId) {
        var _a, _b;
        var id = unityJsbState.lastContextId++;
        var runtime = unityJsbState.getRuntime(rtId);
        var iframe = document.createElement('iframe');
        iframe.name = 'unity-jsb-context-' + id;
        iframe.style.display = 'none';
        document.head.appendChild(iframe);
        var contentWindow = iframe.contentWindow;
        var fetch = contentWindow.fetch.bind(contentWindow);
        var URL = contentWindow.URL;
        var XMLHttpRequest = contentWindow.XMLHttpRequest;
        var XMLHttpRequestUpload = contentWindow.XMLHttpRequestUpload;
        var WebSocket = contentWindow.WebSocket;
        var baseTag = null;
        // #region Promise monkey patch
        // This patches the Promise so that microtasks are not run after the context is destroyed
        var Promise = contentWindow.Promise;
        var originalThen = Promise.prototype.then;
        var originalCatch = Promise.prototype.catch;
        var originalFinally = Promise.prototype.finally;
        Promise.prototype.then = function promiseThenPatch(onFulfilled, onRejected) {
            return originalThen.call(this, !onFulfilled ? undefined : function onFulfilledPatch() { if (!context.isDestroyed)
                return onFulfilled.apply(this, arguments); }, !onRejected ? undefined : function onRejectedPatch() { if (!context.isDestroyed)
                return onRejected.apply(this, arguments); });
        };
        Promise.prototype.catch = function promiseCatchPatch(onRejected) {
            return originalCatch.call(this, !onRejected ? undefined : function onRejectedPatch() { if (!context.isDestroyed)
                return onRejected.apply(this, arguments); });
        };
        if (originalFinally) {
            Promise.prototype.finally = function promiseFinallyPatch(onFinally) {
                return originalFinally.call(this, !onFinally ? undefined : function onFinallyPatch() { if (!context.isDestroyed)
                    return onFinally.apply(this, arguments); });
            };
        }
        // #endregion
        var extraGlobals = {
            location: undefined,
            document: undefined,
            addEventListener: undefined,
            btoa: (_a = window.btoa) === null || _a === void 0 ? void 0 : _a.bind(window),
            atob: (_b = window.atob) === null || _b === void 0 ? void 0 : _b.bind(window),
            $$webglWindow: window,
            WebSocket: WebSocket,
            fetch: fetch,
            URL: URL,
            XMLHttpRequest: XMLHttpRequest,
            XMLHttpRequestUpload: XMLHttpRequestUpload,
            Promise: Promise,
        };
        var globals = new Proxy(extraGlobals, {
            get: function (target, p, receiver) {
                if (p in target)
                    return target[p];
                var res = window[p];
                return res;
            },
            set: function (target, p, val, receiver) {
                target[p] = val;
                return true;
            },
            has: function (target, key) {
                return (key in window) || (key in target);
            },
        });
        extraGlobals.globalThis =
            extraGlobals.global =
                extraGlobals.window =
                    extraGlobals.parent =
                        extraGlobals.self =
                            extraGlobals.this =
                                globals;
        var evaluate = function (code, filename) {
            var sourceMap = !filename ? '' : '\n//# sourceURL=unity-jsb:///' + filename;
            return (function (evalCode) {
                //@ts-ignore
                with (globals) {
                    return eval(evalCode);
                }
            }).call(globals, code + sourceMap);
        };
        var context = {
            id: id,
            runtime: runtime,
            runtimeId: rtId,
            window: window,
            globalObject: globals,
            evaluate: evaluate,
            iframe: iframe,
            contentWindow: contentWindow,
            isDestroyed: false,
            free: function () {
                if (iframe.parentNode)
                    iframe.parentNode.removeChild(iframe);
                context.isDestroyed = true;
                delete runtime.contexts[context.id];
                delete unityJsbState.contexts[context.id];
            },
            setBaseUrl: function (url) {
                if (!baseTag) {
                    baseTag = document.createElement('base');
                }
                baseTag.setAttribute('href', url);
                if (baseTag.parentNode && !url) {
                    baseTag.parentNode.removeChild(baseTag);
                }
                else if (!baseTag.parentNode && url) {
                    iframe.contentWindow.document.head.appendChild(baseTag);
                }
            },
        };
        runtime.contexts[id] = context;
        unityJsbState.contexts[id] = context;
        return id;
    },
    JS_FreeContext: function (ctxId) {
        var context = unityJsbState.getContext(ctxId);
        context.free();
    },
    JS_SetBaseUrl: function (ctxId, url) {
        var context = unityJsbState.getContext(ctxId);
        var urlStr = unityJsbState.stringify(url);
        context.setBaseUrl(urlStr);
    },
    JS_GetGlobalObject: function (returnValue, ctxId) {
        var context = unityJsbState.getContext(ctxId);
        if (!context.globalObjectId) {
            context.runtime.refs.push(context.globalObject, returnValue);
        }
        else {
            context.runtime.refs.duplicateId(context.globalObjectId, returnValue);
        }
    },
    JS_Eval: function (ptr, ctx, input, input_len, filename, eval_flags) {
        var context = unityJsbState.getContext(ctx);
        try {
            var code = unityJsbState.stringify(input, input_len);
            var filenameStr = unityJsbState.stringify(filename);
            var res = context.evaluate(code, filenameStr);
            context.runtime.refs.push(res, ptr);
        }
        catch (err) {
            context.lastException = err;
            context.runtime.refs.push(err, ptr);
            console.error(err);
        }
    },
    JS_IsInstanceOf: function (ctxId, val, obj) {
        var context = unityJsbState.getContext(ctxId);
        var valVal = context.runtime.refs.get(val);
        var ctorVal = context.runtime.refs.get(obj);
        return !!(valVal instanceof ctorVal);
    },
    JS_GetException: function (ptr, ctx) {
        var context = unityJsbState.getContext(ctx);
        context.runtime.refs.push(context.lastException, ptr);
    },
    JSB_FreeValue: function (ctx, v) {
        var context = unityJsbState.getContext(ctx);
        context.runtime.refs.pop(v);
    },
    JSB_FreeValueRT: function (rt, v) {
        var runtime = unityJsbState.getRuntime(rt);
        runtime.refs.pop(v);
    },
    JSB_FreePayload: function (ret, ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var payload = context.runtime.refs.getPayload(obj);
        HEAP32[ret >> 2] = payload.type;
        HEAP32[(ret >> 2) + 1] = payload.payload;
        context.runtime.refs.clearPayload(obj);
    },
    JSB_DupValue: function (ptr, ctx, v) {
        var context = unityJsbState.getContext(ctx);
        context.runtime.refs.duplicate(v, ptr);
    },
    JS_RunGC: function (rt) {
        var runtime = unityJsbState.getRuntime(rt);
        runtime.garbageCollect();
    },
    JS_ComputeMemoryUsage: function (rt, s) {
        // TODO: https://blog.unity.com/technology/unity-webgl-memory-the-unity-heap
    },
    JS_GetPropertyUint32: function (ptr, ctxId, val, index) {
        var context = unityJsbState.getContext(ctxId);
        var obj = context.runtime.refs.get(val);
        var res = obj[index];
        context.runtime.refs.push(res, ptr);
    },
    JS_GetPropertyInternal: function (ptr, ctxId, val, prop, receiver, throwRefError) {
        var context = unityJsbState.getContext(ctxId);
        var valObj = context.runtime.refs.get(val);
        var propStr = unityJsbState.atoms.get(prop);
        var res = valObj[propStr];
        context.runtime.refs.push(res, ptr);
    },
    JS_GetPropertyStr: function (ptr, ctxId, val, prop) {
        var context = unityJsbState.getContext(ctxId);
        var valObj = context.runtime.refs.get(val);
        var propStr = unityJsbState.stringify(prop);
        var res = valObj[propStr];
        context.runtime.refs.push(res, ptr);
    },
    JS_Invoke: function (ptr, ctx, this_obj, prop, argc, argv) {
        var context = unityJsbState.getContext(ctx);
        var propVal = unityJsbState.atoms.get(prop);
        var thisVal = context.runtime.refs.get(this_obj);
        var func = thisVal[propVal];
        var args = context.runtime.refs.batchGet(argv, argc);
        var res;
        try {
            res = func.apply(thisVal, args);
        }
        catch (err) {
            context.lastException = err;
            res = err;
        }
        context.runtime.refs.push(res, ptr);
    },
    JS_Call: function (ptr, ctx, func_obj, this_obj, argc, argv) {
        var context = unityJsbState.getContext(ctx);
        var func = context.runtime.refs.get(func_obj);
        var thisVal = context.runtime.refs.get(this_obj);
        var args = context.runtime.refs.batchGet(argv, argc);
        var res;
        try {
            res = func.apply(thisVal, args);
        }
        catch (err) {
            context.lastException = err;
            res = err;
        }
        context.runtime.refs.push(res, ptr);
    },
    JS_CallConstructor: function (ptr, ctx, func_obj, argc, argv) {
        var context = unityJsbState.getContext(ctx);
        var func = context.runtime.refs.get(func_obj);
        var args = context.runtime.refs.batchGet(argv, argc);
        var res;
        try {
            res = Reflect.construct(func, args);
        }
        catch (err) {
            context.lastException = err;
            res = err;
        }
        context.runtime.refs.push(res, ptr);
    },
    JS_SetConstructor: function (ctx, ctor, proto) {
        var context = unityJsbState.getContext(ctx);
        var ctorVal = context.runtime.refs.get(ctor);
        var protoVal = context.runtime.refs.get(proto);
        ctorVal.prototype = protoVal;
        protoVal.constructor = ctorVal;
        var ctorPayload = context.runtime.refs.getPayload(ctorVal);
        if (ctorPayload.type === 1 /* BridgeObjectType.TypeRef */) {
            context.runtime.refs.setPayload(protoVal, ctorPayload.type, ctorPayload.payload);
        }
    },
    JS_SetPrototype: function (ctx, obj, proto) {
        var context = unityJsbState.getContext(ctx);
        var objVal = context.runtime.refs.get(obj);
        var protoVal = context.runtime.refs.get(proto);
        Reflect.setPrototypeOf(objVal, protoVal);
        return true;
    },
    JS_DefineProperty: function (ctx, this_obj, prop, val, getter, setter, flags) {
        var context = unityJsbState.getContext(ctx);
        var thisVal = context.runtime.refs.get(this_obj);
        var getterVal = context.runtime.refs.get(getter);
        var setterVal = context.runtime.refs.get(setter);
        var valVal = context.runtime.refs.get(val);
        var propVal = unityJsbState.atoms.get(prop);
        var configurable = !!(flags & 1 /* JSPropFlags.JS_PROP_CONFIGURABLE */);
        var hasConfigurable = configurable || !!(flags & 256 /* JSPropFlags.JS_PROP_HAS_CONFIGURABLE */);
        var enumerable = !!(flags & 4 /* JSPropFlags.JS_PROP_ENUMERABLE */);
        var hasEnumerable = enumerable || !!(flags & 1024 /* JSPropFlags.JS_PROP_HAS_ENUMERABLE */);
        var writable = !!(flags & 2 /* JSPropFlags.JS_PROP_WRITABLE */);
        var hasWritable = writable || !!(flags & 512 /* JSPropFlags.JS_PROP_HAS_WRITABLE */);
        var shouldThrow = !!(flags & 16384 /* JSPropFlags.JS_PROP_THROW */) || !!(flags & 32768 /* JSPropFlags.JS_PROP_THROW_STRICT */);
        try {
            var opts = {
                get: getterVal,
                set: setterVal,
            };
            if (!getter && !setter) {
                opts.value = valVal;
            }
            if (hasConfigurable)
                opts.configurable = configurable;
            if (hasEnumerable)
                opts.enumerable = enumerable;
            if (!getter && !setter && hasWritable)
                opts.writable = writable;
            Object.defineProperty(thisVal, propVal, opts);
            return true;
        }
        catch (err) {
            context.lastException = err;
            if (shouldThrow) {
                console.error(err);
                return -1;
            }
        }
        return false;
    },
    JS_DefinePropertyValue: function (ctx, this_obj, prop, val, flags) {
        var context = unityJsbState.getContext(ctx);
        var runtime = context.runtime;
        var thisVal = runtime.refs.get(this_obj);
        var valVal = runtime.refs.get(val);
        var propVal = unityJsbState.atoms.get(prop);
        var configurable = !!(flags & 1 /* JSPropFlags.JS_PROP_CONFIGURABLE */);
        var hasConfigurable = configurable || !!(flags & 256 /* JSPropFlags.JS_PROP_HAS_CONFIGURABLE */);
        var enumerable = !!(flags & 4 /* JSPropFlags.JS_PROP_ENUMERABLE */);
        var hasEnumerable = enumerable || !!(flags & 1024 /* JSPropFlags.JS_PROP_HAS_ENUMERABLE */);
        var writable = !!(flags & 2 /* JSPropFlags.JS_PROP_WRITABLE */);
        var hasWritable = writable || !!(flags & 512 /* JSPropFlags.JS_PROP_HAS_WRITABLE */);
        var shouldThrow = !!(flags & 16384 /* JSPropFlags.JS_PROP_THROW */) || !!(flags & 32768 /* JSPropFlags.JS_PROP_THROW_STRICT */);
        // SetProperty frees the value automatically
        runtime.refs.pop(val);
        try {
            var opts = {
                value: valVal,
            };
            if (hasConfigurable)
                opts.configurable = configurable;
            if (hasEnumerable)
                opts.enumerable = enumerable;
            if (hasWritable)
                opts.writable = writable;
            Object.defineProperty(thisVal, propVal, opts);
            return true;
        }
        catch (err) {
            context.lastException = err;
            if (shouldThrow) {
                console.error(err);
                return -1;
            }
        }
        return false;
    },
    JS_HasProperty: function (ctx, this_obj, prop) {
        var context = unityJsbState.getContext(ctx);
        var thisVal = context.runtime.refs.get(this_obj);
        var propVal = unityJsbState.atoms.get(prop);
        var res = Reflect.has(thisVal, propVal);
        return !!res;
    },
    JS_SetPropertyInternal: function (ctx, this_obj, prop, val, flags) {
        var context = unityJsbState.getContext(ctx);
        var runtime = context.runtime;
        var thisVal = runtime.refs.get(this_obj);
        var valVal = runtime.refs.get(val);
        var propVal = unityJsbState.atoms.get(prop);
        // SetProperty frees the value automatically
        runtime.refs.pop(val);
        var shouldThrow = !!(flags & 16384 /* JSPropFlags.JS_PROP_THROW */) || !!(flags & 32768 /* JSPropFlags.JS_PROP_THROW_STRICT */);
        try {
            thisVal[propVal] = valVal;
            return true;
        }
        catch (err) {
            context.lastException = err;
            if (shouldThrow) {
                console.error(err);
                return -1;
            }
        }
        return false;
    },
    JS_SetPropertyUint32: function (ctx, this_obj, idx, val) {
        var context = unityJsbState.getContext(ctx);
        var runtime = context.runtime;
        var thisVal = context.runtime.refs.get(this_obj);
        var valVal = context.runtime.refs.get(val);
        var propVal = idx;
        // SetProperty frees the value automatically
        runtime.refs.pop(val);
        try {
            thisVal[propVal] = valVal;
            return true;
        }
        catch (err) {
            context.lastException = err;
        }
        return false;
    },
    jsb_get_payload_header: function (ret, ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var rec = context.runtime.refs.getPayload(obj);
        HEAP32[ret >> 2] = rec.type;
        HEAP32[(ret >> 2) + 1] = rec.payload;
    },
    JS_ToCStringLen2: function (ctx, len, val, cesu8) {
        var context = unityJsbState.getContext(ctx);
        var str = context.runtime.refs.get(val);
        if (typeof str === 'undefined') {
            HEAP32[(len >> 2)] = 0;
            return 0;
        }
        var _a = unityJsbState.bufferify(str), buffer = _a[0], length = _a[1];
        HEAP32[(len >> 2)] = length - 1;
        return buffer;
    },
    JS_FreeCString: function (ctx, ptr) {
        _free(ptr);
    },
    JS_GetArrayBuffer: function (ctx, psize, obj) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(obj);
        if (value instanceof ArrayBuffer) {
            HEAP32[psize >> 2] = value.byteLength;
            return value;
        }
        return 0;
    },
    // #region Atoms
    JS_NewAtomLen: function (ctx, str, len) {
        var context = unityJsbState.getContext(ctx);
        var val = unityJsbState.stringify(str, len);
        return unityJsbState.atoms.push(val);
    },
    JS_AtomToString: function (ptr, ctx, atom) {
        var context = unityJsbState.getContext(ctx);
        var str = unityJsbState.atoms.get(atom);
        context.runtime.refs.push(str, ptr);
    },
    JS_FreeAtom: function (ctx, v) {
        unityJsbState.atoms.pop(v);
    },
    JS_DupAtom: function (ctx, v) {
        return unityJsbState.atoms.pushId(v);
    },
    JSB_ATOM_constructor: function () {
        return unityJsbState.atoms.push('constructor');
    },
    JSB_ATOM_Error: function () {
        return unityJsbState.atoms.push('Error');
    },
    JSB_ATOM_fileName: function () {
        return unityJsbState.atoms.push('fileName');
    },
    JSB_ATOM_Function: function () {
        return unityJsbState.atoms.push('Function');
    },
    JSB_ATOM_length: function () {
        return unityJsbState.atoms.push('length');
    },
    JSB_ATOM_lineNumber: function () {
        return unityJsbState.atoms.push('lineNumber');
    },
    JSB_ATOM_message: function () {
        return unityJsbState.atoms.push('message');
    },
    JSB_ATOM_name: function () {
        return unityJsbState.atoms.push('name');
    },
    JSB_ATOM_Number: function () {
        return unityJsbState.atoms.push('Number');
    },
    JSB_ATOM_prototype: function () {
        return unityJsbState.atoms.push('prototype');
    },
    JSB_ATOM_Proxy: function () {
        return unityJsbState.atoms.push('Proxy');
    },
    JSB_ATOM_stack: function () {
        return unityJsbState.atoms.push('stack');
    },
    JSB_ATOM_String: function () {
        return unityJsbState.atoms.push('String');
    },
    JSB_ATOM_Object: function () {
        return unityJsbState.atoms.push('Object');
    },
    JSB_ATOM_Operators: function () {
        return unityJsbState.atoms.push('Operators');
    },
    JSB_ATOM_Symbol_operatorSet: function () {
        return unityJsbState.atoms.push('operatorSet');
    },
    // #endregion
    // #region Is
    JS_IsArray: function (ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var valVal = context.runtime.refs.get(val);
        var res = Array.isArray(valVal);
        return !!res;
    },
    JS_IsConstructor: function (ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var res = !!obj.prototype && !!obj.prototype.constructor.name;
        return !!res;
    },
    JS_IsError: function (ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var valVal = context.runtime.refs.get(val);
        var res = valVal instanceof Error;
        return !!res;
    },
    JS_IsFunction: function (ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var valVal = context.runtime.refs.get(val);
        var res = typeof valVal === 'function';
        return !!res;
    },
    // #endregion
    JS_ParseJSON: function (ptr, ctx, buf, buf_len, filename) {
        var context = unityJsbState.getContext(ctx);
        var str = unityJsbState.stringify(buf, buf_len);
        var res = JSON.parse(str);
        context.runtime.refs.push(res, ptr);
    },
    JS_JSONStringify: function (ptr, ctx, obj, replacer, space) {
        var context = unityJsbState.getContext(ctx);
        var objVal = context.runtime.refs.get(obj);
        var rpVal = context.runtime.refs.get(replacer);
        var spVal = context.runtime.refs.get(space);
        var res = JSON.stringify(objVal, rpVal, spVal);
        context.runtime.refs.push(res, ptr);
    },
    // #region New
    JS_NewArray: function (ptr, ctx) {
        var context = unityJsbState.getContext(ctx);
        var res = [];
        context.runtime.refs.push(res, ptr);
    },
    JS_NewArrayBufferCopy: function (ptr, ctx, buf, len) {
        var context = unityJsbState.getContext(ctx);
        var nptr = _malloc(len);
        var res = new Uint8Array(HEAPU8.buffer, nptr, len);
        var existing = new Uint8Array(HEAPU8.buffer, buf, len);
        res.set(existing);
        context.runtime.refs.push(res, ptr);
    },
    JSB_NewFloat64: function (ptr, ctx, d) {
        var context = unityJsbState.getContext(ctx);
        context.runtime.refs.push(d, ptr);
    },
    JSB_NewInt64: function (ptr, ctx, d) {
        var context = unityJsbState.getContext(ctx);
        context.runtime.refs.push(d, ptr);
    },
    JS_NewObject: function (ptr, ctx) {
        var context = unityJsbState.getContext(ctx);
        var res = {};
        context.runtime.refs.push(res, ptr);
    },
    JS_NewString: function (ptr, ctx, str) {
        var context = unityJsbState.getContext(ctx);
        var res = unityJsbState.stringify(str);
        context.runtime.refs.push(res, ptr);
    },
    JS_NewStringLen: function (ptr, ctx, str, len) {
        var context = unityJsbState.getContext(ctx);
        var val = unityJsbState.stringify(str, len);
        context.runtime.refs.push(val, ptr);
    },
    JSB_NewEmptyString: function (ptr, ctx) {
        var context = unityJsbState.getContext(ctx);
        var res = "";
        context.runtime.refs.push(res, ptr);
    },
    // #endregion
    // #region Bridge
    JSB_NewCFunction: function (ret, ctx, func, atom, length, cproto, magic) {
        var context = unityJsbState.getContext(ctx);
        var refs = context.runtime.refs;
        var name = unityJsbState.atoms.get(atom) || 'jscFunction';
        function jscFunction() {
            var args = arguments;
            var thisObj = this === window ? context.globalObject : this;
            var _a = refs.allocate(thisObj), thisPtr = _a[0], thisId = _a[1];
            var ret = _malloc(16 /* Sizes.JSValue */);
            if (cproto === 0 /* JSCFunctionEnum.JS_CFUNC_generic */) {
                var argc = args.length;
                var _b = refs.batchAllocate(Array.from(args)), argv = _b[0], argIds = _b[1];
                unityJsbState.dynCall('viiiii', func, [ret, ctx, thisPtr, argc, argv]);
                argIds.forEach(refs.popId);
                _free(argv);
            }
            else if (cproto === 9 /* JSCFunctionEnum.JS_CFUNC_setter */) {
                var _c = refs.allocate(args[0]), val = _c[0], valId = _c[1];
                unityJsbState.dynCall('viiii', func, [ret, ctx, thisPtr, val]);
                refs.popId(valId);
                _free(val);
            }
            else if (cproto === 8 /* JSCFunctionEnum.JS_CFUNC_getter */) {
                unityJsbState.dynCall('viii', func, [ret, ctx, thisPtr]);
            }
            else {
                throw new Error("Unknown type of function specified: name=".concat(name, " type=").concat(cproto));
            }
            refs.popId(thisId);
            _free(thisPtr);
            var returnValue = refs.get(ret);
            refs.pop(ret);
            _free(ret);
            return returnValue;
        }
        jscFunction['$$csharpFunctionName'] = name;
        refs.push(jscFunction, ret);
    },
    JSB_NewCFunctionMagic: function (ret, ctx, func, atom, length, cproto, magic) {
        var context = unityJsbState.getContext(ctx);
        var refs = context.runtime.refs;
        var name = unityJsbState.atoms.get(atom) || 'jscFunctionMagic';
        function jscFunctionMagic() {
            var args = arguments;
            var thisObj = this === window ? context.globalObject : this;
            var _a = refs.allocate(thisObj), thisPtr = _a[0], thisId = _a[1];
            var ret = _malloc(16 /* Sizes.JSValue */);
            if (cproto === 1 /* JSCFunctionEnum.JS_CFUNC_generic_magic */) {
                var argc = args.length;
                var _b = refs.batchAllocate(Array.from(args)), argv = _b[0], argIds = _b[1];
                unityJsbState.dynCall('viiiiii', func, [ret, ctx, thisPtr, argc, argv, magic]);
                argIds.forEach(refs.popId);
                _free(argv);
            }
            else if (cproto === 3 /* JSCFunctionEnum.JS_CFUNC_constructor_magic */) {
                var argc = args.length;
                var _c = refs.batchAllocate(Array.from(args)), argv = _c[0], argIds = _c[1];
                unityJsbState.dynCall('viiiiii', func, [ret, ctx, thisPtr, argc, argv, magic]);
                argIds.forEach(refs.popId);
                _free(argv);
            }
            else if (cproto === 11 /* JSCFunctionEnum.JS_CFUNC_setter_magic */) {
                var _d = refs.allocate(args[0]), val = _d[0], valId = _d[1];
                unityJsbState.dynCall('viiiii', func, [ret, ctx, thisPtr, val, magic]);
                refs.popId(valId);
                _free(val);
            }
            else if (cproto === 10 /* JSCFunctionEnum.JS_CFUNC_getter_magic */) {
                unityJsbState.dynCall('viiii', func, [ret, ctx, thisPtr, magic]);
            }
            else {
                throw new Error("Unknown type of function specified: name=".concat(name, " type=").concat(cproto));
            }
            refs.popId(thisId);
            _free(thisPtr);
            var returnValue = refs.get(ret);
            refs.pop(ret);
            _free(ret);
            return returnValue;
        }
        ;
        jscFunctionMagic['$$csharpFunctionName'] = name;
        refs.push(jscFunctionMagic, ret);
        if (cproto === 3 /* JSCFunctionEnum.JS_CFUNC_constructor_magic */) {
            refs.setPayload(jscFunctionMagic, 1 /* BridgeObjectType.TypeRef */, magic);
        }
    },
    jsb_new_bridge_object: function (ret, ctx, proto, object_id) {
        var context = unityJsbState.getContext(ctx);
        var protoVal = context.runtime.refs.get(proto);
        var res = Object.create(protoVal);
        context.runtime.refs.push(res, ret);
        context.runtime.refs.setPayload(res, 2 /* BridgeObjectType.ObjectRef */, object_id);
    },
    jsb_new_bridge_value: function (ret, ctx, proto, size) {
        var context = unityJsbState.getContext(ctx);
        var protoVal = context.runtime.refs.get(proto);
        var res = Object.create(protoVal);
        res.$$values = new Array(size).fill(0);
        context.runtime.refs.push(res, ret);
    },
    JSB_NewBridgeClassObject: function (ret, ctx, new_target, object_id) {
        var context = unityJsbState.getContext(ctx);
        var res = context.runtime.refs.get(new_target);
        context.runtime.refs.push(res, ret);
        context.runtime.refs.setPayload(res, 2 /* BridgeObjectType.ObjectRef */, object_id);
    },
    JSB_NewBridgeClassValue: function (ret, ctx, new_target, size) {
        var context = unityJsbState.getContext(ctx);
        var res = context.runtime.refs.get(new_target);
        res.$$values = new Array(size).fill(0);
        context.runtime.refs.push(res, ret);
    },
    JSB_GetBridgeClassID: function () {
        // TODO: I have no idea
        return 0;
    },
    jsb_construct_bridge_object: function (ret, ctx, ctor, object_id) {
        var context = unityJsbState.getContext(ctx);
        var ctorVal = context.runtime.refs.get(ctor);
        var res = Reflect.construct(ctorVal, []);
        context.runtime.refs.push(res, ret);
        context.runtime.refs.setPayload(res, 2 /* BridgeObjectType.ObjectRef */, object_id);
    },
    jsb_crossbind_constructor: function (ret, ctx, new_target) {
        var context = unityJsbState.getContext(ctx);
        var target = context.runtime.refs.get(new_target);
        // TODO: I have no idea
        var res = function () {
            return new target();
        };
        context.runtime.refs.push(res, ret);
    },
    // #endregion
    // #region Errors
    JSB_ThrowError: function (ret, ctx, buf, buf_len) {
        var context = unityJsbState.getContext(ctx);
        var str = unityJsbState.stringify(buf, buf_len);
        var err = new Error(str);
        console.error(err);
        context.runtime.refs.push(err, ret);
        // TODO: throw?
    },
    JSB_ThrowTypeError: function (ret, ctx, msg) {
        var context = unityJsbState.getContext(ctx);
        var str = 'Type Error';
        var err = new Error(str);
        console.error(err);
        context.runtime.refs.push(err, ret);
        // TODO: throw?
    },
    JSB_ThrowRangeError: function (ret, ctx, msg) {
        var context = unityJsbState.getContext(ctx);
        var str = 'Range Error';
        var err = new Error(str);
        console.error(err);
        context.runtime.refs.push(err, ret);
        // TODO: throw?
    },
    JSB_ThrowInternalError: function (ret, ctx, msg) {
        var context = unityJsbState.getContext(ctx);
        var str = 'Internal Error';
        var err = new Error(str);
        console.error(err);
        context.runtime.refs.push(err, ret);
        // TODO: throw?
    },
    JSB_ThrowReferenceError: function (ret, ctx, msg) {
        var context = unityJsbState.getContext(ctx);
        var str = 'Reference Error';
        var err = new Error(str);
        console.error(err);
        context.runtime.refs.push(err, ret);
        // TODO: throw?
    },
    // #endregion
    // #region Low level Set
    js_strndup: function (ctx, s, n) {
        var buffer = _malloc(n + 1);
        _memcpy(buffer, s, n);
        HEAPU8[buffer + n] = 0;
        return buffer;
    },
    jsb_set_floats: function (ctx, val, n, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = n / 4 /* Sizes.Single */;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        for (var index = 0; index < count; index++) {
            var val_1 = HEAPF32[(v0 >> 2) + index];
            obj.$$values[index] = val_1;
        }
        return true;
    },
    jsb_set_bytes: function (ctx, val, n, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = n / 4 /* Sizes.Single */;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        for (var index = 0; index < count; index++) {
            var val_2 = HEAP32[(v0 >> 2) + index];
            obj.$$values[index] = val_2;
        }
        return true;
    },
    jsb_set_byte_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAP32[(v0 >> 2)];
        obj.$$values[1] = HEAP32[(v1 >> 2)];
        obj.$$values[2] = HEAP32[(v2 >> 2)];
        obj.$$values[3] = HEAP32[(v3 >> 2)];
        return true;
    },
    jsb_set_float_2: function (ctx, val, v0, v1) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 2;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAPF32[(v0 >> 2)];
        obj.$$values[1] = HEAPF32[(v1 >> 2)];
        return true;
    },
    jsb_set_float_3: function (ctx, val, v0, v1, v2) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 3;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAPF32[(v0 >> 2)];
        obj.$$values[1] = HEAPF32[(v1 >> 2)];
        obj.$$values[2] = HEAPF32[(v2 >> 2)];
        return true;
    },
    jsb_set_float_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAPF32[(v0 >> 2)];
        obj.$$values[1] = HEAPF32[(v1 >> 2)];
        obj.$$values[2] = HEAPF32[(v2 >> 2)];
        obj.$$values[3] = HEAPF32[(v3 >> 2)];
        return true;
    },
    jsb_set_int_1: function (ctx, val, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 1;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAP32[(v0 >> 2)];
        return true;
    },
    jsb_set_int_2: function (ctx, val, v0, v1) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 2;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAP32[(v0 >> 2)];
        obj.$$values[1] = HEAP32[(v1 >> 2)];
        return true;
    },
    jsb_set_int_3: function (ctx, val, v0, v1, v2) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 3;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAP32[(v0 >> 2)];
        obj.$$values[1] = HEAP32[(v1 >> 2)];
        obj.$$values[2] = HEAP32[(v2 >> 2)];
        return true;
    },
    jsb_set_int_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        obj.$$values[0] = HEAP32[(v0 >> 2)];
        obj.$$values[1] = HEAP32[(v1 >> 2)];
        obj.$$values[2] = HEAP32[(v2 >> 2)];
        obj.$$values[3] = HEAP32[(v3 >> 2)];
        return true;
    },
    // #endregion
    // #region Low Level Get
    jsb_get_bytes: function (ctx, val, n, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = n / 4 /* Sizes.Single */;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        for (var index = 0; index < count; index++) {
            var val_3 = obj.$$values[index];
            HEAP32[(v0 >> 2) + index] = val_3;
        }
        return true;
    },
    jsb_get_floats: function (ctx, val, n, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = n / 4 /* Sizes.Single */;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        for (var index = 0; index < count; index++) {
            var val_4 = obj.$$values[index];
            HEAPF32[(v0 >> 2) + index] = val_4;
        }
        return true;
    },
    jsb_get_byte_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAP32[(v0 >> 2)] = obj.$$values[0];
        HEAP32[(v1 >> 2)] = obj.$$values[1];
        HEAP32[(v2 >> 2)] = obj.$$values[2];
        HEAP32[(v3 >> 2)] = obj.$$values[3];
        return true;
    },
    jsb_get_float_2: function (ctx, val, v0, v1) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 2;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAPF32[(v0 >> 2)] = obj.$$values[0];
        HEAPF32[(v1 >> 2)] = obj.$$values[1];
        return true;
    },
    jsb_get_float_3: function (ctx, val, v0, v1, v2) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 3;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAPF32[(v0 >> 2)] = obj.$$values[0];
        HEAPF32[(v1 >> 2)] = obj.$$values[1];
        HEAPF32[(v2 >> 2)] = obj.$$values[2];
        return true;
    },
    jsb_get_float_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAPF32[(v0 >> 2)] = obj.$$values[0];
        HEAPF32[(v1 >> 2)] = obj.$$values[1];
        HEAPF32[(v2 >> 2)] = obj.$$values[2];
        HEAPF32[(v3 >> 2)] = obj.$$values[3];
        return true;
    },
    jsb_get_int_1: function (ctx, val, v0) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 1;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAP32[(v0 >> 2)] = obj.$$values[0];
        return true;
    },
    jsb_get_int_2: function (ctx, val, v0, v1) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 2;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAP32[(v0 >> 2)] = obj.$$values[0];
        HEAP32[(v1 >> 2)] = obj.$$values[1];
        return true;
    },
    jsb_get_int_3: function (ctx, val, v0, v1, v2) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 3;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAP32[(v0 >> 2)] = obj.$$values[0];
        HEAP32[(v1 >> 2)] = obj.$$values[1];
        HEAP32[(v2 >> 2)] = obj.$$values[2];
        return true;
    },
    jsb_get_int_4: function (ctx, val, v0, v1, v2, v3) {
        var context = unityJsbState.getContext(ctx);
        var obj = context.runtime.refs.get(val);
        var count = 4;
        if (!Array.isArray(obj.$$values) || count >= obj.$$values.length)
            return false;
        HEAP32[(v0 >> 2)] = obj.$$values[0];
        HEAP32[(v1 >> 2)] = obj.$$values[1];
        HEAP32[(v2 >> 2)] = obj.$$values[2];
        HEAP32[(v3 >> 2)] = obj.$$values[3];
        return true;
    },
    // #endregion
    // #region To
    JS_ToFloat64: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            HEAPF64[pres >> 3] = Number(value);
            return false;
        }
        return -1;
    },
    JS_ToInt32: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            HEAP32[pres >> 2] = Number(value);
            return false;
        }
        return -1;
    },
    JS_ToInt64: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            unityJsbState.HEAP64()[pres >> 3] = BigInt(value);
            return false;
        }
        return -1;
    },
    JS_ToBigInt64: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            unityJsbState.HEAP64()[pres >> 3] = BigInt(value);
            return false;
        }
        return -1;
    },
    JS_ToIndex: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            unityJsbState.HEAPU64()[pres >> 3] = BigInt(value);
            return false;
        }
        return -1;
    },
    JSB_ToUint32: function (ctx, pres, val) {
        var context = unityJsbState.getContext(ctx);
        var value = context.runtime.refs.get(val);
        if (typeof value === 'number' || typeof value === 'bigint') {
            HEAPU32[pres >> 2] = Number(value);
            return false;
        }
        return -1;
    },
    JS_ToBool: function (ctx, val) {
        var context = unityJsbState.getContext(ctx);
        var objVal = context.runtime.refs.get(val);
        return !!objVal;
    },
    // #endregion
    // #region Bytecode
    JS_ReadObject: function (ptr, ctx, buf, buf_len, flags) {
        console.warn('Bytecode is not supported in WebGL Backend');
    },
    JS_WriteObject: function (ctx, psize, obj, flags) {
        console.warn('Bytecode is not supported in WebGL Backend');
        return 0;
    },
    JS_EvalFunction: function (ptr, ctx, fun_obj) {
        console.warn('Bytecode is not supported in WebGL Backend');
    },
    js_free: function (ctx, ptr) {
        // TODO: Not sure what this is but seems related to Bytecode
    },
    // #endregion
    // #region Misc features
    JS_NewPromiseCapability: function (ret, ctx, resolving_funcs) {
        // TODO
        return 0;
    },
    JS_SetHostPromiseRejectionTracker: function (rt, cb, opaque) {
        // TODO:
    },
    JS_SetInterruptHandler: function (rt, cb, opaque) {
        // TODO:
    },
    JS_SetModuleLoaderFunc: function (rt, module_normalize, module_loader, opaque) {
        // TODO:
    },
    JS_GetImportMeta: function (ret, ctx, m) {
        // TODO:
        return 0;
    },
    JS_ResolveModule: function (ctx, obj) {
        // TODO:
        return 0;
    },
    JS_AddIntrinsicOperators: function (ctx) {
        console.warn('Operator overloading is not supported in WebGL Backend');
    },
    JS_ExecutePendingJob: function (rt, pctx) {
        // Automatically handled by browsers
        return false;
    },
    JS_IsJobPending: function (rt, pctx) {
        // Automatically handled by browsers
        return false;
    },
    // #endregion
};
autoAddDeps(UnityJSBPlugin, '$unityJsbState');
mergeInto(LibraryManager.library, UnityJSBPlugin);
