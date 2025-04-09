using UnityEngine;

namespace QuickJS
{
    public class LocalStorage
    {
        public const string LocalStoragePrefix = "QuickJs_LocalStorage_";

        public LocalStorage()
        {
            
        }

        public void setItem(string x, string value)
        {
            PlayerPrefs.SetString(LocalStoragePrefix + x, value);
        }

        public string getItem(string x)
        {
            return PlayerPrefs.GetString(LocalStoragePrefix + x, null);
        }

        public void removeItem(string x)
        {
            PlayerPrefs.DeleteKey(LocalStoragePrefix + x);
        }


#if UNITY_EDITOR
        [UnityEditor.MenuItem("QuickJs/Clear Local Storage", priority = 0)]
        public static void ClearLocalStorage()
        {
            PlayerPrefs.DeleteAll();
        }
#endif
    }
}