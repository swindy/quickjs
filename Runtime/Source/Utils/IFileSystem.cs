using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace QuickJS.Utils
{
    public interface IFileSystem
    {
        /// <summary>
        /// to check if the file is existed (do not throw exception)
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// the fullpath of the given path (do not throw exception)
        /// </summary>
        string GetFullPath(string path);

        /// <summary>
        /// the content of file, return null if any error occurs (do not throw exception)
        /// </summary>
        byte[] ReadAllBytes(string path);
    }

    public class CompoundedFileSystem : IFileSystem
    {
        private List<IFileSystem> _fileSystems = new List<IFileSystem>();

        public bool Exists(string path)
        {
            for (int i = 0, count = _fileSystems.Count; i < count; ++i)
            {
                var fs = _fileSystems[i];
                if (fs.Exists(path))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetFullPath(string path)
        {
            for (int i = 0, count = _fileSystems.Count; i < count; ++i)
            {
                var fs = _fileSystems[i];
                var fp = fs.GetFullPath(path);
                if (!string.IsNullOrEmpty(fp))
                {
                    return fp;
                }
            }
            return null;
        }

        public byte[] ReadAllBytes(string path)
        {
            for (int i = 0, count = _fileSystems.Count; i < count; ++i)
            {
                var fs = _fileSystems[i];
                var fp = fs.ReadAllBytes(path);
                if (fp != null)
                {
                    return fp;
                }
            }
            return null;
        }
    }

    public class DefaultFileSystem : IFileSystem
    {
        public DefaultFileSystem()
        {
        }

        public bool Exists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public string GetFullPath(string path)
        {
            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte[] ReadAllBytes(string path)
        {
            try
            {
                return System.IO.File.ReadAllBytes(path);
            }
            catch (Exception exception)
            {
                Diagnostics.Logger.IO.Exception(path, exception);
                return null;
            }
        }
    }

#if !JSB_UNITYLESS
    public class ResourcesFileSystem : IFileSystem
    {
        public ResourcesFileSystem()
        {
        }

        public bool Exists(string path)
        {
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(path);
            return asset != null;
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public byte[] ReadAllBytes(string path)
        {
            try
            {
                var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(path);
                return asset.bytes;
            }
            catch (Exception exception)
            {
                Diagnostics.Logger.IO.Exception(path, exception);
                return null;
            }
        }
    }
#endif
    
    public class HttpFileSystem : IFileSystem
    {
        private string _url;

        public HttpFileSystem(string baseUrl)
        {
            _url = baseUrl;
        }

        private string GetRemote(string path)
        {
            try
            {
                var uri = _url.EndsWith("/") ? _url + path : $"{_url}/{path}";
                var request = WebRequest.CreateHttp(uri);
                var response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var reader = new StreamReader(response.GetResponseStream());
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        public bool Exists(string path)
        {
            if (!path.EndsWith(".js") && !path.EndsWith(".json") && !path.EndsWith(".jsonc"))
            {
                return false;
            }
            var asset = GetRemote(path);
            return asset != null;
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public byte[] ReadAllBytes(string path)
        {
            try
            {
                var asset = GetRemote(path);
                return Encoding.UTF8.GetBytes(asset);
            }
            catch (Exception exception)
            {
                QuickJS.Diagnostics.Logger.IO.Error("{0}: {1}\n{2}", path, exception.Message, exception.StackTrace);
                return null;
            }
        }
    }
    
}
