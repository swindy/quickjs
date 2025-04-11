using System;
using QuickJS.Binding;
using QuickJS.IO;
using QuickJS.Unity;
using QuickJS.Utils;
using UnityEngine;

namespace QuickJS
{
    public class JSLauncher : MonoBehaviour
    {
        public enum DebugMode
        {
            None = 0,
            Debug = 1,
            DebugAndAwait = 2,
        }
        
        public string BaseUrl = "http://127.0.0.1:3000";
        
        public string SourceFile = "index.js";
        
        public ScriptSource.ScriptSourceType SourceType = ScriptSource.ScriptSourceType.Resource;

        public ScriptSource.DevServerType DevServer = ScriptSource.DevServerType.InEditor;
        
        public JavascriptEngineType EngineType = JavascriptEngineType.QuickJS;
        
        public DebugMode Debug = DebugMode.None;
        
        public bool StackTrace = false;
        
        [JSToggleHint("ReflectBind Mode")]
        public bool useReflectBind;

        public EngineContext Context { get; private set; }

        private void Awake()
        {
            
        }

        private void OnEnable()
        {
            Startup();
        }

        public void Startup()
        {
            LoadAndRun();
        }
        
        private void LoadAndRun(Action afterStart = null)
        {
            var script = new ScriptSource()
            {
                SourceType = SourceType, 
                SourceFile = SourceFile, 
                DevServer = BaseUrl, 
                UseDevServer = DevServer, 
                Watch = true
            };
            Context = CreateContext(script);
            Context.RunMainScript(script);
        }
        
        protected EngineContext CreateContext(ScriptSource script)
        {
            return new EngineContext(new EngineContext.EngineOptions
            {
                Source = script,
                OnRestart = () => Startup(),
                EngineType = EngineType,
                Debug = Debug != DebugMode.None,
                AwaitDebugger = Debug == DebugMode.DebugAndAwait,
                StackTrace = StackTrace,
                UseReflectBind = useReflectBind,
            });
        }
        
        void Update()
        {
            if (Context != null)
            {
                Context.Update();
            }
        }
        
        void OnDestroy()
        {
            Context.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}