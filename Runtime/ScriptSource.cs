using System;
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
            TextAsset = 0,
            File = 1,
            Url = 2,
            Resource = 3,
            Raw = 4,
        }
        
        public ScriptSourceType SourceType = ScriptSourceType.TextAsset;
        public TextAsset SourceAsset;
        public bool Watch = true;
        public string SourcePath;
        public string SourceText;
        public string ResourcesPath;
        public DevServerType UseDevServer = DevServerType.InEditor;
        public bool ShouldUseDevServer =>
#if UNITY_EDITOR
            UseDevServer == DevServerType.InEditor || UseDevServer == DevServerType.Always;
#else
            UseDevServer == DevServerType.Always;
#endif
        public string DevServer = "http://localhost:3000";
        private const string DevServerFilename = "/index.js";
        private bool DevServerFailed = false;
        
        public ScriptSourceType Type = ScriptSourceType.TextAsset;
        
        public bool IsDevServer => !DevServerFailed && ShouldUseDevServer && !string.IsNullOrWhiteSpace(DevServer);
        
        public string DevServerFile
        {
            get
            {
                var serverUrl = new Uri(DevServer);
                var path = serverUrl.PathAndQuery;
                if (string.IsNullOrWhiteSpace(path) || path == "/")
                {
                    if (Uri.TryCreate(serverUrl, DevServerFilename, out var res)) return res.AbsoluteUri;
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
                    case ScriptSourceType.TextAsset:
                        res = SourceAsset.name;
                        break;
                    case ScriptSourceType.File:
                        res = SourcePath;
                        break;
                    case ScriptSourceType.Url:
                        res = SourcePath;
                        break;
                    case ScriptSourceType.Resource:
                        res = SourcePath;
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
            SourcePath = source.SourcePath;
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
                SourcePath = path,
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
                return SourcePath;
            }
            else if (SourceType == ScriptSourceType.TextAsset)
            {
#if UNITY_EDITOR
                var srcAsset = UnityEditor.AssetDatabase.GetAssetPath(SourceAsset);
                if (!string.IsNullOrWhiteSpace(srcAsset)) return srcAsset;
#endif

                return string.IsNullOrEmpty(ResourcesPath) ? "Assets/Resources/react/index.js" : ResourcesPath;
            }
            else if (SourceType == ScriptSourceType.Url)
            {
                var href = SourcePath;

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
            else if (Type == ScriptSourceType.File)
            {
                Uri.TryCreate(SourcePath, UriKind.RelativeOrAbsolute, out uri);
            }
            else if (Type == ScriptSourceType.Resource)
            {
                var url = string.IsNullOrEmpty(ResourcesPath) ? ("Assets/Resources/" + SourcePath) : ResourcesPath;
                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);
            }
            else if (Type == ScriptSourceType.TextAsset)
            {
                string url = "";
#if UNITY_EDITOR
                var srcAsset = UnityEditor.AssetDatabase.GetAssetPath(SourceAsset);
                if (!string.IsNullOrWhiteSpace(srcAsset))
                    url = srcAsset;
#endif

                if (string.IsNullOrWhiteSpace(url))
                    url = string.IsNullOrEmpty(ResourcesPath) ? "Assets/Resources/react/index.js" : ResourcesPath;

                Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri);
            }
            else if (Type == ScriptSourceType.Url)
            {
                var href = SourcePath;

                var abs = Application.absoluteURL;
                var url = new URL(href, abs);
                Uri.TryCreate(url.href, UriKind.Absolute, out uri);
            }
            return uri;
        }
        
    }
}