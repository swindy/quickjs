using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using QuickJS.Binding;
using QuickJS.IO;
using QuickJS.Native;
using QuickJS.Utils;
using UnityEditor.PackageManager;
using UnityEngine;

namespace QuickJS
{
    public class QuickJsEngine : IJavaScriptEngine
    {
        public string Key { get; } = "quickjs";
        public object NativeEngine => Runtime;
        
        public EngineCapabilities Capabilities { get; } = EngineCapabilities.None
#if !UNITY_EDITOR && UNITY_WEBGL
            | EngineCapabilities.Fetch
            | EngineCapabilities.XHR
            | EngineCapabilities.Encoding
            | EngineCapabilities.WebSocket
            | EngineCapabilities.Console
            | EngineCapabilities.Base64
            | EngineCapabilities.AbortController
            | EngineCapabilities.QueueMicrotask
#endif
            | EngineCapabilities.None;

        private Action<IJavaScriptEngine> OnInitialize;
        
        private EngineContext Context;

        public ScriptRuntime Runtime { get; private set; }
        public QuickJS.ScriptContext MainContext { get; private set; }
        public ScriptValue Global { get; private set; }
        public ITypeDB TypeDB { get; private set; }
        public ObjectCache ObjectCache { get; private set; }

        public ScriptFunction ObjectKeys { get; private set; }

        private bool Initialized;
        
        public QuickJsEngine(EngineContext context, bool debug, bool awaitDebugger, Action<IJavaScriptEngine> onInitialize)
        {
            Context = context;
            OnInitialize = onInitialize;
            

            Runtime = ScriptEngine.CreateRuntime(context?.IsEditorContext ?? false);
            Runtime.withStacktrace = Context.Options.StackTrace;
            
            Runtime.AddModuleResolvers();
            Runtime.OnInitialized += Runtime_OnInitialized;
            Runtime.Initialize(new ScriptRuntimeArgs
            {
                withDebugServer = debug,
                waitingForDebugger = awaitDebugger,
                fileSystem = new DefaultFileSystem(),
                asyncManager = new DefaultAsyncManager(),
                binder = DefaultBinder.GetBinder(Context.Options.UseReflectBind),
                debugServerPort = 9222,
                byteBufferAllocator = new ByteBufferPooledAllocator(),
                pathResolver = new PathResolver(),
                // apiBridge = ApiBridge,
            });
        }

        private void Runtime_OnInitialized(ScriptRuntime runtime)
        {
            MainContext = Runtime.GetMainContext();
            TypeDB = MainContext.GetTypeDB();
            ObjectCache = MainContext.GetObjectCache();

            var global = MainContext.GetGlobalObject();
            Values.js_get_classvalue(MainContext, global, out ScriptValue globalSv);
            Global = globalSv;

            var objCtor = Global.GetProperty<ScriptValue>("Object");
            var keys = objCtor.GetProperty<ScriptFunction>("keys");
            keys.SetBound(objCtor);
            objCtor.Dispose();
            ObjectKeys = keys;

            JSApi.JSB_FreeValueRT(Runtime, global);

            Initialized = true;
            OnInitialize?.Invoke(this);
        }

        public object Evaluate(string code, string fileName = null)
        {
            var res = MainContext.EvalSource<object>(code, fileName ?? "eval");
            Runtime.ExecutePendingJob();
            return res;
        }

        public void Execute(string code, string fileName = null, JavascriptDocumentType documentType = JavascriptDocumentType.Script)
        {
            var voidedCode = code + "\n;;void 0;";
            Evaluate(voidedCode, fileName);
        }

        public Exception TryExecute(string code, string fileName = null, JavascriptDocumentType documentType = JavascriptDocumentType.Script)
        {
            try
            {
                Execute(code, fileName, documentType);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return ex;
            }
            return null;
        }

        public void SetProperty<T>(object obj, string key, T value)
        {
            if (obj is ScriptFunction sf)
            {
                Values.js_get_classvalue(sf.ctx, sf, out ScriptValue svf);
                obj = svf;
            }

            if (obj is ScriptValue sv)
            {
                sv.SetProperty(key, CreateNativeValue(value));
            }
        }

        public object GetGlobal(string key)
        {
            return Global.GetProperty<object>(key);
        }

        public void SetGlobal<T>(string key, T value)
        {
            SetProperty(Global, key, value);
        }

        public void DeleteGlobal(string key)
        {
            SetProperty<object>(Global, key, null);
        }

        public object CreateNativeValue(object v)
        {
            if (v is Type t) return CreateTypeReference(t);
            return v;
        }

        public object CreateTypeReference(Type type)
        {
            TypeDB.GetDynamicType(type, false);
            var ctor = TypeDB.GetConstructorOf(type);
            Values.js_get_classvalue(MainContext, ctor, out ScriptValue res);
            JSApi.JS_FreeValue(MainContext, ctor);
            return res;
        }

        public object CreateNamespaceReference(string ns, params Assembly[] assemblies)
        {
            return new ScriptNamespaceReference(this, ns, assemblies);
        }

        public object CreateScriptObject(IEnumerable<KeyValuePair<string, object>> props)
        {
            var obj = JSApi.JS_NewObject(MainContext);
            if (!Values.js_get_classvalue(MainContext, obj, out ScriptValue sv)) return null;

            foreach (var item in props) SetProperty(sv, item.Key, item.Value);

            Runtime.FreeValue(obj);
            return sv;
        }

        public void Dispose()
        {
            Global?.Dispose();
            Global = null;
            
            ObjectKeys?.Dispose();
            ObjectKeys = null;

            TypeDB = null;
            MainContext = null;
            ObjectCache = null;
            OnInitialize = null;

            Runtime?.Shutdown();
            Runtime = null;
        }

        public IEnumerable<object> TraverseScriptArray(object obj)
        {
            if (obj is IEnumerable eo)
            {
                foreach (var kv in eo) yield return kv;
            }
            else if (obj is ScriptValue jv)
            {
                var len = jv.GetProperty<int>("length");

                for (int i = 0; i < len; i++)
                {
                    yield return jv.GetProperty<object>(i + "");
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> TraverseScriptObject(object obj)
        {
            if (obj is IEnumerable<KeyValuePair<string, object>> eo)
            {
                foreach (var kv in eo) yield return kv;
            }
            else if (obj is ScriptValue jv)
            {
                var res = ObjectKeys.Invoke<string[]>(jv);

                foreach (var kv in res)
                    yield return new KeyValuePair<string, object>(kv, jv.GetProperty<object>(kv));
            }
        }

        public bool IsScriptObject(object obj)
        {
            return obj is ScriptValue jv;
        }

        public void Update()
        {
            if (Initialized) Runtime.Update((int) (Time.deltaTime * 1000));
        }
    }
    
    public class QuickJsEngineFactory : IJavaScriptEngineFactory
    {
        public JavascriptEngineType EngineType => JavascriptEngineType.QuickJS;

        public IJavaScriptEngine Create(EngineContext context, bool debug, bool awaitDebugger, Action<IJavaScriptEngine> onInitialize)
        {
            return new QuickJsEngine(context, debug, awaitDebugger, onInitialize);
        }
    }
}