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
        
        public FileLoader fileLoader;
        
        public string BaseUrl = "http://127.0.0.1:8183";
        
        public string EntryFileName = "example_main.js";
        
        public bool StackTrace = false;
        
        [JSToggleHint("ReflectBind Mode")]
        public bool useReflectBind;
        
        private ScriptRuntime _rt;
        
        private void Awake()
        {
            IFileSystem fileSystem;
            
            _rt = ScriptEngine.CreateRuntime();
            var asyncManager = new DefaultAsyncManager();
            var pathResolver = new PathResolver();
            pathResolver.AddSearchPath("node_modules");

            if (fileLoader == FileLoader.Resources)
            {
                fileSystem = new ResourcesFileSystem();

                // it's the relative path under Unity Resources directory space
                pathResolver.AddSearchPath("dist");
            }
            else if (fileLoader == FileLoader.Http)
            {
                fileSystem = new HttpFileSystem(BaseUrl);
            }
            else
            {
                // the DefaultFileSystem only demonstrates the minimalistic implementation of file access, it's usually enough for development in editor.
                // you should implement your own filesystem layer for the device-end runtime (based on AssetBundle or zip)
                fileSystem = new DefaultFileSystem();
                pathResolver.AddSearchPath("Scripts/out");
                // pathResolver.AddSearchPath("../Scripts/out");
                // _rt.AddSearchPath("Assets/Examples/Scripts/dist");
            }

            _rt.withStacktrace = StackTrace;
            
            // if (sourceMap)
            // {
            // _rt.EnableSourceMap();
            // }
            
            _rt.AddModuleResolvers();
            _rt.OnInitialized += OnInitialized;
            _rt.Initialize(new ScriptRuntimeArgs
            {
                withDebugServer = false,
                waitingForDebugger = false, 
                debugServerPort = 9229,
                fileSystem = fileSystem,
                pathResolver = pathResolver,
                asyncManager = asyncManager,
                byteBufferAllocator = new ByteBufferPooledAllocator(),
                binder = DefaultBinder.GetBinder(useReflectBind),
                // apiBridge = new Experimental.CustomApiBridgeImpl(),
            });
        }

        private void OnInitialized(ScriptRuntime obj)
        {
            try
            {
                _rt.EvalMain(EntryFileName);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
  
        }

        void Update()
        {
            if (_rt != null)
            {
                _rt.Update((int)(Time.deltaTime * 1000f));
            }
        }
        
        void OnDestroy()
        {
            ScriptEngine.Shutdown();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}