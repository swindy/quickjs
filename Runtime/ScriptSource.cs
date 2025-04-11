using System;
using System.Collections;
using System.IO;
using QuickJS.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace QuickJS
{
    [Serializable]
    public class ScriptSource
    {
        public enum DevServerType
        {
            Never = 0,
            InEditor = 1,
            Always = 2,
        }
        
        public enum ScriptSourceType
        {
            File = 0,
            Url = 1,
            Resource = 2,
            Raw = 3,
        }
        
        public ScriptSourceType SourceType = ScriptSourceType.Resource;
        public TextAsset SourceAsset;
        public bool Watch = true;
        public string SourceFile;
        public string SourceText;
        public string ResourcesPath;
        public DevServerType UseDevServer = DevServerType.InEditor;
        public bool ShouldUseDevServer =>
#if UNITY_EDITOR
            UseDevServer == DevServerType.InEditor || UseDevServer == DevServerType.Always;
#else
            UseDevServer == DevServerType.Always;
#endif
        
        private bool DevServerFailed = false;
        
        public bool IsDevServer => !DevServerFailed && ShouldUseDevServer && !string.IsNullOrWhiteSpace(DevServer);
        
        
        public string DevServer = "http://localhost:3000";
        
        public string DevServerFile
        {
            get
            {
                var serverUrl = new Uri(DevServer);
                var path = serverUrl.PathAndQuery;
                if (string.IsNullOrWhiteSpace(path) || path == "/")
                {
                    if (Uri.TryCreate(serverUrl, SourceFile, out var res)) return res.AbsoluteUri;
                }
                return serverUrl.AbsoluteUri;
            }
        }
        
        public string FileName
        {
            get
            {
                string res = null;

                switch (SourceType)
                {
                    case ScriptSourceType.File:
                        res = SourceFile;
                        break;
                    case ScriptSourceType.Url:
                        res = SourceFile;
                        break;
                    case ScriptSourceType.Resource:
                        res = SourceFile;
                        break;
                    case ScriptSourceType.Raw:
                        res = "Inline Script";
                        break;
                    default:
                        break;
                }

                return res;
            }
        }

        public ScriptSourceType EffectiveScriptSource => IsDevServer ? ScriptSourceType.Url : SourceType;
        
        public ScriptSource() { }
        
        public ScriptSource(ScriptSource source)
        {
            SourceType = source.SourceType;
            SourceAsset = source.SourceAsset;
            SourceFile = source.SourceFile;
            SourceText = source.SourceText;
            ResourcesPath = source.ResourcesPath;
            UseDevServer = source.UseDevServer;
            DevServer = source.DevServer;
        }
        
        public static ScriptSource Resource(string path)
        {
            return new ScriptSource()
            {
                SourceType = ScriptSourceType.Resource,
                SourceFile = path,
                UseDevServer = DevServerType.Never,
                Watch = false
            };
        }
        
        public static ScriptSource Text(string path)
        {
            return new ScriptSource()
            {
                SourceType = ScriptSourceType.Raw,
                SourceText = path,
                UseDevServer = DevServerType.Never,
                Watch = false,
            };
        }
        
        public string GetResolvedSourceUrl(bool useDevServer = true)
        {
            if (useDevServer && IsDevServer) return DevServer;

            if (SourceType == ScriptSourceType.File || SourceType == ScriptSourceType.Resource)
            {
                return SourceFile;
            }
            else if (SourceType == ScriptSourceType.Url)
            {
                var href = SourceFile;

                var abs = Application.absoluteURL;
                var url = new URL(href, abs);
                return url.href;
            }
            return "";
        }
        
        public Uri GetRemoteUrl(bool useDevServer = true)
        {
            Uri uri = null;

            if (useDevServer && IsDevServer)
            {
                Uri.TryCreate(DevServerFile, UriKind.Absolute, out uri);
            }
            else if (SourceType == ScriptSourceType.File)
            {
                Uri.TryCreate(SourceFile, UriKind.RelativeOrAbsolute, out uri);
            }
            else if (SourceType == ScriptSourceType.Resource)
            {
                var url = string.IsNullOrEmpty(ResourcesPath) ? ("Assets/Resources/" + SourceFile) : ResourcesPath;
                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);
            }
            else if (SourceType == ScriptSourceType.Url)
            {
                var href = SourceFile;

                var abs = Application.absoluteURL;
                var url = new URL(href, abs);
                Uri.TryCreate(url.href, UriKind.Absolute, out uri);
            }
            return uri;
        }
        
        public IDisposable GetScript(Action<string> callback, IDispatcher dispatcher, bool useDevServer = true)
        {
            if (useDevServer && IsDevServer)
            {
                var request = UnityEngine.Networking.UnityWebRequest.Get(DevServerFile);

                return new DisposableHandle(dispatcher,
                    dispatcher.StartDeferred(
                        WatchWebRequest(request, callback, err => {
                            DevServerFailed = true;
                            Debug.LogWarning("DevServer seems to be unaccessible. Falling back to the original script. If this is unexpected, make sure the DevServer is running at " + DevServer);
                            GetScript(callback, dispatcher, false);
                        })));
            }


            var filePath = "";

            switch (SourceType)
            {
                case ScriptSourceType.File:
#if UNITY_EDITOR
                    filePath = StripHashAndSearch(SourceFile);
                    callback(File.ReadAllText(filePath));
                    break;
#endif
                case ScriptSourceType.Url:
#if UNITY_EDITOR
                    var request = UnityEngine.Networking.UnityWebRequest.Get(GetResolvedSourceUrl(false));
                    return new DisposableHandle(dispatcher,
                        dispatcher.StartDeferred(WatchWebRequest(request, callback)));
#else
                    throw new Exception("REACT_DISABLE_WEB is defined. Web API cannot be used.");
#endif
                case ScriptSourceType.Resource:
                    var asset = Resources.Load<TextAsset>(StripHashAndSearch(SourceFile));
                    if (asset)
                    {
#if UNITY_EDITOR
                        filePath = GetResourcePath(asset);
#endif
                        callback(asset.text);
                    }
                    else callback(null);
                    break;
                case ScriptSourceType.Raw:
                    callback(SourceText);
                    break;
                default:
                    callback(null);
                    break;
            }
            
            return null;
        }
        
        static internal IEnumerator WatchWebRequest(
            UnityEngine.Networking.UnityWebRequest request,
            Action<string> callback,
            Action<string> errorCallback = null
        )
        {
            yield return request.SendWebRequest();
            if (!string.IsNullOrWhiteSpace(request.error))
                errorCallback?.Invoke(request.error);
            else
                callback(request.downloadHandler.text);
        }
        
#if UNITY_EDITOR
        private static string GetResourcePath(UnityEngine.Object asset)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), UnityEditor.AssetDatabase.GetAssetPath(asset).Replace('/', '\\'));
        }
#endif
        
        private string StripHashAndSearch(string url)
        {
            return url.Split('#')[0].Split('?')[0];
        }
        
    }
}