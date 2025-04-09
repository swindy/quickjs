using System;
using QuickJS.Core;
using UnityEngine;

namespace QuickJS
{
    public class EngineContext
    {
        
        public class Options
        {
            public GlobalRecord Globals;
            public ScriptSource Source;
            // public ITimer Timer;
            public Action OnRestart;
            public JavascriptEngineType EngineType;
            public bool Debug;
            public bool AwaitDebugger;
            public Action BeforeStart;
            public Action AfterStart;
            // public PoolingType Pooling;
            // public UnknownPropertyHandling UnknownPropertyHandling;
        }
        
        public bool Initialized { get; private set; }
        
        public bool EngineInitialized { get; private set; }
        
        public bool Debug { get; set; }
        
        public bool AwaitDebugger { get; set; }
        
        public LocalStorage LocalStorage { get; }
        
        public ScriptSource Source { get; }
        
        // public IDispatcher Dispatcher { get; }
        //
        // public ITimer Timer { get; }
        
        public virtual bool IsEditorContext => false;
        
        public Options options { get; }
        
        public readonly JavascriptEngineType EngineType;
        
        public readonly IJavaScriptEngineFactory EngineFactory;
        
        private IJavaScriptEngine engine;
        public IJavaScriptEngine Engine
        {
            get
            {
                if (!Initialized) Initialize(null);

                if (engine == null) throw new InvalidOperationException("Engine is not initialized yet");
                return engine;
            }
        }
        
        public ITimer Timer { get; }
        
        public Callback FireEventByRefCallback;
        
        public EngineContext(Options options)
        {
            this.options = options;
            // Timer = options.Timer;
            LocalStorage = new LocalStorage();
            
            Source = options.Source;
            // Timer = options.Timer;
            // Dispatcher = CreateDispatcher();
            // Globals = options.Globals;
            // OnRestart = options.OnRestart ?? (() => { });
            // CalculatesLayout = options.CalculatesLayout;
            // Location = new Location(this);
            // MediaProvider = options.MediaProvider;
            // CursorAPI = new CursorAPI(this);
           
            
            // Dispatcher.OnEveryUpdate(UpdateElementsRecursively);
            // Dispatcher.OnEveryLateUpdate(LateUpdateElementsRecursively);
            
            EngineFactory = JavascriptEngineHelpers.GetEngineFactory(options.EngineType);

#if UNITY_EDITOR
            // Runtime contexts are disposed on reload (by OnDisable), but this is required for editor contexts
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Dispose;
#endif
        }
        
        
        
        public void Initialize(Action callback)
        {
            if (Initialized)
            {
                callback?.Invoke();
                return;
            }

            Initialized = true;
            if (engine == null)
            {
                CreateBaseEngine(Debug, AwaitDebugger, () => {
                    engine.SetGlobal("Context", this);
                    engine.SetGlobal("localStorage", LocalStorage);

                    // CreateDOMShims(engine);
                    // CreateConsole(engine);
                    // CreateScheduler(engine, Context);
                    // CreatePolyfills(engine);
                    
                    EngineInitialized = true;

                    callback?.Invoke();
                });
            }
            else callback?.Invoke();
        }
        
        public void Dispose()
        {
            // CommandsCallback = null;
            // FireEventByRefCallback = null;
            // GetObjectCallback = null;
            // GetEventAsObjectCallback = null;
            //
            // IsDisposed = true;
            // Host?.Destroy(false);
            // Host = null;
            // Refs.Clear();
            // foreach (var dr in DetachedRoots) dr.Destroy(false);
            // DetachedRoots.Clear();
            // Dispatcher?.Dispose();
            // Globals?.Dispose();
            // foreach (var item in Disposables) item?.Invoke();
            // Script?.Dispose();
        }
        
        void CreateBaseEngine(bool debug, bool awaitDebugger, Action onInitialize)
        {
            EngineFactory.Create(this, debug, awaitDebugger, (engine) => {
                this.engine = engine;
                
                onInitialize?.Invoke();
            });
        }
        
        void CreateDOMShims(IJavaScriptEngine engine)
        {
          
            if (!engine.Capabilities.HasFlag(EngineCapabilities.URL))
            {
                engine.SetGlobal("URL", typeof(URL));
                engine.SetGlobal("URLSearchParams", typeof(URLSearchParams));
            }
            
            if (!engine.Capabilities.HasFlag(EngineCapabilities.Encoding))
            {
                engine.SetGlobal("EncodingHelpers", typeof(EncodingHelpers));
                engine.Execute(@"
                    global.encodeURI          = function(x) {   return EncodingHelpers.encodeURI(x + '')            };
                    global.decodeURI          = function(x) {   return EncodingHelpers.decodeURI(x + '')            };
                    global.encodeURIComponent = function(x) {   return EncodingHelpers.encodeURIComponent(x + '')   };
                    global.decodeURIComponent = function(x) {   return EncodingHelpers.decodeURIComponent(x + '')   };
                ", "QuickJS/shims/encoding");
            }

        }
        
        void CreateConsole(IJavaScriptEngine engine)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            engine.Execute("global.console = global.$$webglWindow.console; void 0;", "ReactUnity/shims/console");
#else
            if (engine.Capabilities.HasFlag(EngineCapabilities.Console)) return;

            var console = new ConsoleProxy(this);

            engine.SetGlobal("__console", console);
            engine.Execute(@"(function() {
                var _console = global.__console;
                global.console = {
                    log:       function log       ()    { _console.log   (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    info:      function info      ()    { _console.info  (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    debug:     function debug     ()    { _console.debug (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    trace:     function trace     ()    { _console.debug (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    warn:      function warn      ()    { _console.warn  (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    error:     function error     ()    { _console.error (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    dir:       function dir       ()    { _console.dir   (arguments[0], Array.prototype.slice.call(arguments, 1)) },
                    clear:     function clear     (arg) { _console.clear(arg)         },
                    assert:    function assert    (arg) { _console.assert(arg)        },
                    count:     function count    (name) { return _console.count(name) },
                };
                void 0;
})()", "QuickJS/shims/console");
            engine.DeleteGlobal("__console");
#endif
        }
        
        
        public Callback CreateEventCallback(string code, object thisVal)
        {
            Engine.SetGlobal("__thisArg", thisVal);
            var fn = EvaluateScript(
                "(function(ts) { return (function(event, sender) {\n" + code + "\n}).bind(ts); })(__thisArg)");
            Engine.DeleteGlobal("__thisArg");

            return Callback.From(fn, this, thisVal);
        }
        
        public object EvaluateScript(string code, string fileName = null)
        {
            return Engine.Evaluate(code, fileName);
        }
        
        public void ExecuteScript(string code, string fileName = null, JavascriptDocumentType documentType = JavascriptDocumentType.Script)
        {
            Engine.Execute(code, fileName, documentType);
        }

        
        public object JsonParse(string str)
        {
            return Engine.Evaluate(str);
        }

        public virtual string ResolvePath(string path)
        {
            var source = Source.GetResolvedSourceUrl();
            return new URL(path, source).href;
        }

        
    }
}