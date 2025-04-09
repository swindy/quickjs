using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickJS
{
    public class URL
    {
        private static string[] PathSplitArray = new string[] { "/" };

        public string baseUrl { get; protected set; }

        string _href;
        public string href
        {
            get => _href;
            set => SetHref(value);
        }
        public string protocol { get; protected set; }
        public string hostname { get; protected set; }
        public string origin { get; protected set; }
        public string host { get; protected set; }
        public string port { get; protected set; }
        public string pathname { get; protected set; }
        public string rawPathname { get; protected set; }



        public string _search;
        public string _hash;
        public string search {
            get => _search;
            set {
                _search = NormalizeWithPrefix(value, "?");
                RebuildHref();
            }
        }
        public string hash {
            get => _hash;
            set {
                _hash = NormalizeWithPrefix(value, "#");
                RebuildHref();
            }
        }

        private URLSearchParams _searchParams;
        public URLSearchParams searchParams => _searchParams = _searchParams ?? new URLSearchParams(this);


        public URL(string url) : this(url, null)
        {
        }

        public URL(string url, string baseUrl)
        {
            this.baseUrl = baseUrl;
            SetHref(url);
        }

        protected void SetHref(string url)
        {
            string href;

            if (string.IsNullOrWhiteSpace(url))
            {
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    href = baseUrl;
                else
                    href = "";

            }
            else if (string.IsNullOrWhiteSpace(baseUrl))
            {
                href = url;
            }
            else
            {
                var baseCl = new URL(baseUrl);
                var justUrl = new URL(url);

                if (!string.IsNullOrWhiteSpace(justUrl.protocol)) href = url;
                else
                {
                    var newSegments = new List<string>();

                    if (!url.FastStartsWith("/") || string.IsNullOrWhiteSpace(baseCl.protocol))
                    {
                        var basePathSegments = baseCl.GetPathSegments();
                        AddToPath(newSegments, basePathSegments);

                        if (newSegments.Count > 0)
                            newSegments.RemoveAt(newSegments.Count - 1);
                    }

                    var pathSegments = GetPathSegments(justUrl.rawPathname);
                    AddToPath(newSegments, pathSegments);

                    var newPath = string.Join("/", newSegments);
                    href = (string.IsNullOrWhiteSpace(baseCl.origin) ? "" : baseCl.origin + "/") +
                        newPath + justUrl.search + justUrl.hash;
                }
            }

            var hashSplit = href.Split('#');
            var hashless = hashSplit[0];
            var hash = hashSplit.Length > 1 ? ("#" + hashSplit[1]) : "";

            var searchSplit = hashless.Split('?');
            var search = searchSplit.Length > 1 ? ("?" + searchSplit[1]) : "";
            var searchless = searchSplit[0];

            var hrefSplit = searchless.Split(new string[] { "//" }, 2, StringSplitOptions.None);

            var hasProtocol = hrefSplit.Length > 1;
            var protocol = hasProtocol ? hrefSplit.First() : null;

            var hrefWithoutProtocol = string.Join("//", hrefSplit.Skip(hasProtocol ? 1 : 0));
            var hrefWithoutProtocolSplit = hrefWithoutProtocol.Split(new string[] { "/" }, 2, StringSplitOptions.None);


            var hostCandidate = hrefWithoutProtocolSplit.FirstOrDefault();
            var hasHost = hasProtocol || hostCandidate.Contains(":") || (hostCandidate.IndexOf(".") > 0);


            var host = hasHost ? hrefWithoutProtocolSplit.FirstOrDefault() : null;
            var hostSplit = host?.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            var hostName = hostSplit?.First();
            var port = hostSplit != null ? (hostSplit.ElementAtOrDefault(1) ?? "") : null;

            var origin = hasHost ? (protocol + "//" + host) : null;

            var rawPathName = string.Join("/", hrefWithoutProtocolSplit.Skip(hasHost ? 1 : 0));

            var pathName = NormalizePath(rawPathName);
            if (!pathName.FastStartsWith("/") && !string.IsNullOrWhiteSpace(origin)) pathName = "/" + pathName;

            var newHref = (origin ?? "") + pathName + search + hash;

            this._href = newHref;
            this.protocol = protocol;
            this.hostname = hostName;
            this.origin = origin;
            this.host = host;
            this.port = port;
            this._search = search;
            this._hash = hash;
            this.pathname = pathName;
            this.rawPathname = rawPathName;
        }

        protected void RebuildHref()
        {
            var newHref = (origin ?? "") + pathname + search + hash;
            SetHref(newHref);
        }

        private string NormalizeWithPrefix(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.FastStartsWith(prefix)) return value;
            return prefix + value;
        }

        private string[] GetPathSegments() => GetPathSegments(pathname);

        private static string[] GetPathSegments(string pathname)
        {
            if (string.IsNullOrWhiteSpace(pathname)) return new string[0];
            if (pathname == "/") return new string[0];
            //if (pathname.FastStartsWith("/")) pathname = pathname.Substring(1);
            return pathname.Split(PathSplitArray, StringSplitOptions.None);
        }

        private static string NormalizePath(string pathname)
        {
            if (string.IsNullOrWhiteSpace(pathname)) return "";
            var pathSegments = GetPathSegments(pathname);

            var path = new List<string>();
            AddToPath(path, pathSegments);

            return string.Join("/", path.ToArray());
        }

        private static void AddToPath(List<string> path, ICollection<string> segments)
        {
            var addFolderAtEnd = false;

            foreach (var p in segments)
            {
                if (string.IsNullOrWhiteSpace(p))
                {
                    addFolderAtEnd = true;
                    continue;
                }
                else if (p == ".")
                {
                    addFolderAtEnd = true;
                    continue;
                }
                else if (p == "..")
                {
                    addFolderAtEnd = true;
                    if (path.Count > 0)
                        path.RemoveAt(path.Count - 1);
                }
                else
                {
                    addFolderAtEnd = false;
                    path.Add(p);
                }
            }

            if (addFolderAtEnd) path.Add("");
        }

        public override String ToString() => _href;
    }
}