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
        public enum FileLoader
        {
            Default,
            Resources,
            Http,
        }
        
        public enum DebugMode
        {
            None = 0,
            Debug = 1,
            DebugAndAwait = 2,
        }
        
        public FileLoader fileLoader;
        
        public string BaseUrl = "http://127.0.0.1:8183";
        
        public string EntryFileName = "example_main.js";
        
        public ScriptSource Source = new ScriptSource() { Type = ScriptSource.ScriptSourceType.Resource, SourcePath = "index", Watch = true };
        
        public JavascriptEngineType EngineType = JavascriptEngineType.QuickJS;
        
        public DebugMode Debug = DebugMode.None;
        
        public bool StackTrace = false;
        
        [JSToggleHint("ReflectBind Mode")]
        public bool useReflectBind;

        public EngineContext Context { get; private set; }

        private void Awake()
        {
            
        }

        public void Startup()
        {
            LoadAndRun(Source);
        }
        
        private void LoadAndRun(ScriptSource script, Action afterStart = null)
        {
            Context = CreateContext(script);
            // Context.Start(afterStart);
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