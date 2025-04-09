using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace QuickJS
{
    public enum JavascriptEngineType
    {
        
        [InspectorName("ClearScript")]
        ClearScript = 0,
        
        [InspectorName("QuickJS")]
        QuickJS = 1,
    }

    
    public enum JavascriptDocumentType
    {
        Script = 0,
        Module = 1,
    }
    
    public interface IJavaScriptEngine : IDisposable
    {
        string Key { get; }
        object NativeEngine { get; }
        EngineCapabilities Capabilities { get; }

        void Execute(string code, string fileName = null, JavascriptDocumentType documentType = JavascriptDocumentType.Script);
        Exception TryExecute(string code, string fileName = null, JavascriptDocumentType documentType = JavascriptDocumentType.Script);
        object Evaluate(string code, string fileName = null);

        object GetGlobal(string key);
        void SetGlobal<T>(string key, T value);
        void DeleteGlobal(string key);

        void SetProperty<T>(object obj, string key, T value);
        object CreateScriptObject(IEnumerable<KeyValuePair<string, object>> props);
        bool IsScriptObject(object obj);

        object CreateTypeReference(Type type);
        object CreateNamespaceReference(string ns, params Assembly[] assemblies);
        IEnumerable<object> TraverseScriptArray(object obj);
        IEnumerator<KeyValuePair<string, object>> TraverseScriptObject(object obj);

        void Update();
    }
    
    public interface IJavaScriptEngineFactory
    {
        JavascriptEngineType EngineType { get; }
        IJavaScriptEngine Create(EngineContext context, bool debug, bool awaitDebugger, Action<IJavaScriptEngine> onInitialize);
    }
}