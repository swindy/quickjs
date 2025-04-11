using System;
using QuickJS.Core;
using UnityEngine;

namespace QuickJS
{
    public class EngineContext
    {
        
        public class EngineOptions
        {
            public GlobalRecord Globals;
            public ScriptSource Source;
            // public ITimer Timer;
            public Action OnRestart;
            public JavascriptEngineType EngineType;
            public bool Debug;
            public bool AwaitDebugger;
            public bool StackTrace;
            public bool UseReflectBind;
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
        
        public EngineOptions Options { get; }
        
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
        
        public ITimer Timer { get; private set; }
        
        public IDispatcher Dispatcher { get; }
        
        public Callback FireEventByRefCallback;
        
        public EngineContext(EngineOptions engineOptions)
        {
            this.Options = engineOptions;
            Source = engineOptions.Source;
            
            Timer = UnscaledTimer.Instance;
            Dispatcher = CreateDispatcher();
            EngineFactory = JavascriptEngineHelpers.GetEngineFactory(engineOptions.EngineType);

#if UNITY_EDITOR
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
                    callback?.Invoke();
                });
            }
            else callback?.Invoke();
        }
        
        
        public void RunMainScript(ScriptSource source)
        {
            source.GetScript((code) =>
            {
                RunMainScript(code);
            }, Dispatcher, true);
            
        }

        public void RunMainScript(string code)
        {
            Initialize(() =>
            {
                Engine.TryExecute(code, "QuickJs/main");
            });
        }

        public void Update()
        {
            Engine?.Update();
        }
        
        public void Dispose()
        {
            // CommandsCallback = null;
            FireEventByRefCallback = null;
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
            Engine?.Dispose();
        }
        
        public IDispatcher CreateDispatcher() => Application.isPlaying && !IsEditorContext ?
            RuntimeDispatcherBehavior.Create(this, Timer) as IDispatcher :
            new EditorDispatcher(this);
        
        void CreateBaseEngine(bool debug, bool awaitDebugger, Action onInitialize)
        {
            EngineFactory.Create(this, debug, awaitDebugger, (engine) => {
                this.engine = engine;
                
                onInitialize?.Invoke();
            });
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