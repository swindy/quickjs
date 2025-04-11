using System;
using System.Collections.Generic;

namespace QuickJS.Utils
{
    using Native;

    public class ObjectCache
    {
        private struct ObjectSlot
        {
            public int next;
            public object target;
            public bool disposable;
        }

        private bool _disposed;

        private int _freeIndex = -1;
        private int _activeMapSlotCount = 0;

        // it holds any two way binding object (with JS finalizer calling)
        // id => host object
        private ObjectSlot[] _objectSlots;
        private int _slotAllocated = 0;

        // host object => jsvalue heapptr (dangerous, no ref count)
        private Dictionary<object, JSValue> _rmap = new Dictionary<object, JSValue>(EqualityComparer.Default);

        private JSWeakMap<ScriptValue> _scriptValueMap = new JSWeakMap<ScriptValue>();

        // 刻意与 ScriptValue 隔离
        private JSWeakMap<ScriptDelegate> _delegateMap = new JSWeakMap<ScriptDelegate>();

        // private JSWeakMap<ScriptPromise> _promiseMap = new JSWeakMap<ScriptPromise>();

        public ObjectCache(uint initialSize)
        {
            _objectSlots = new ObjectSlot[initialSize > 256 ? initialSize : 256];
        }

        public void ForEachManagedObject(Action<object> callback)
        {
            for (int i = 0, count = _slotAllocated; i < count; ++i)
            {
                ref readonly var item = ref _objectSlots[i];
                if (item.next == -1)
                {
                    callback(item.target);
                }
            }
        }

        public int GetManagedObjectCount()
        {
            return _activeMapSlotCount;
        }

        public int GetManagedObjectCap()
        {
            return _slotAllocated;
        }

        public int GetJSObjectCount()
        {
            return _rmap.Count;
        }

        public int GetDelegateCount()
        {
            return _delegateMap.Count;
        }

        public int GetScriptValueCount()
        {
            return _scriptValueMap.Count;
        }

        // public int GetScriptPromiseCount()
        // {
        //     return _promiseMap.Count;
        // }

        public void Destroy()
        {
            if (_disposed)
            {
                return;
            }
#if JSB_DEBUG
            Diagnostics.Logger.Default.Debug("_activeMapSlotCount {0}", _activeMapSlotCount);
            foreach (var entry in _objectSlots)
            {
                Diagnostics.Assert.Debug(entry.target == null, "Entry {0}", entry.target);
            }
            foreach (var entry in _rmap)
            {
                Diagnostics.Logger.Default.Debug("REntry {0} = {1}", entry.Key, entry.Value);
            }
#endif
            _disposed = true;
            _freeIndex = -1;
            _activeMapSlotCount = 0;
            _slotAllocated = 0;
            _rmap.Clear();
            _delegateMap.Clear();
            _scriptValueMap.Clear();
            // _promiseMap.Clear();
        }

        /// <summary>
        /// 建立 object to jsvalue 的映射. 
        /// 外部必须自己保证 object 存在的情况下对应的 js value 不会被释放.
        /// </summary>
        public void AddJSValue(object o, JSValue heapptr)
        {
            if (_disposed)
            {
                return;
            }
            if (o != null)
            {
#if JSB_DEBUG
                JSValue oldPtr;
                if (TryGetJSValue(o, out oldPtr))
                {
                    Diagnostics.Logger.Default.Fatal("exists object => js value mapping {0}: {1} => {2}", o, oldPtr, heapptr);
                }
#endif
                _rmap[o] = heapptr;
            }
        }

        // object `o` must be valid as key
        public bool TryGetJSValue(object o, out JSValue heapptr)
        {
            return _rmap.TryGetValue(o, out heapptr);
        }

        /// <summary>
        /// register a strong reference of object in ObjectCache
        /// </summary>
        public int AddObject(object o, bool disposable)
        {
            if (_disposed)
            {
                Diagnostics.Logger.Default.Debug("calling AddObject after being disposed: {0}", o);
                return -1;
            }
            Diagnostics.Assert.Debug(o != null);
            ++_activeMapSlotCount;
            if (_freeIndex < 0)
            {
                var oldSize = _objectSlots.Length;
                var id = _slotAllocated++;
                if (id >= oldSize)
                {
                    Array.Resize(ref _objectSlots, oldSize <= 8192 ? oldSize * 2 : oldSize + 1024);
                }
                ref var freeEntryRef = ref _objectSlots[id];
                freeEntryRef.next = -1;
                freeEntryRef.target = o;
                freeEntryRef.disposable = disposable;
                return id;
            }
            else
            {
                var id = _freeIndex;
                ref var freeEntryRef = ref _objectSlots[id];
                _freeIndex = freeEntryRef.next;
                freeEntryRef.next = -1;
                freeEntryRef.target = o;
                freeEntryRef.disposable = disposable;
                return id;
            }
        }

        public bool SetObjectDisposable(int id, bool disposable)
        {
            if (id >= 0 && id < _slotAllocated)
            {
                ref var entryRef = ref _objectSlots[id];
                if (entryRef.next == -1)
                {
                    entryRef.disposable = disposable;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetObject(int id, out object o)
        {
            if (id >= 0 && id < _slotAllocated)
            {
                ref readonly var entryRef = ref _objectSlots[id];
                if (entryRef.next == -1)
                {
                    o = entryRef.target;
                    return true;
                }
            }
            o = null;
            return false;
        }

        public bool RemoveObject(in JSPayloadHeader payload)
        {
            return payload.type_id == BridgeObjectType.ObjectRef ? RemoveObject(payload.value) : false;
        }

        public bool RemoveObject(int id)
        {
            object o;
            return RemoveObject(id, out o);
        }

        public bool RemoveObject(int id, out object o)
        {
            if (TryGetObject(id, out o))
            {
                ref var entryRef = ref _objectSlots[id];
                var disposable = entryRef.disposable;
                entryRef.next = _freeIndex;
                entryRef.target = null;
                _freeIndex = id;
                --_activeMapSlotCount;
                _rmap.Remove(o);
                if (disposable)
                {
                    var jsf = o as IDisposable;
                    if (jsf != null)
                    {
                        jsf.Dispose();
                    }
                }
                return true;
            }
            return false;
        }

        // 覆盖已有记录, 无记录返回 false
        public bool ReplaceObject(int id, object o)
        {
            object oldValue;
            if (TryGetObject(id, out oldValue))
            {
                ref var entryRef = ref _objectSlots[id];
                entryRef.target = o;
                JSValue heapptr;
                if (oldValue != null && _rmap.TryGetValue(oldValue, out heapptr))
                {
                    _rmap.Remove(oldValue);
                    _rmap[o] = heapptr;
                }
                return true;
            }
            return false;
        }

        public bool TryGetTypedWeakObject<T>(int id, out T o)
        where T : class
        {
            object obj;
            if (TryGetObject(id, out obj))
            {
                var w = obj as WeakReference;
                o = w != null ? w.Target as T : null;
                return true;
            }
            o = null;
            return false;
        }

        public bool TryGetTypedObject<T>(int id, out T o)
        where T : class
        {
            object obj;
            if (TryGetObject(id, out obj))
            {
                o = obj as T;
                return true;
            }
            o = null;
            return false;
        }

        public bool MatchObjectType(int id, Type type)
        {
            object o;
            if (TryGetObject(id, out o))
            {
                if (o != null)
                {
                    var otype = o.GetType();
                    return otype == type || otype.IsSubclassOf(type) || type.IsAssignableFrom(otype);
                }
                return true;
            }
            return false;
        }

        #region delegate mapping 

        /// <summary>
        /// register a weak reference of ScriptDelegate in ObjectCache
        /// </summary>
        public bool AddDelegate(JSValue jso, ScriptDelegate o)
        {
            if (_disposed)
            {
                Diagnostics.Logger.Default.Debug("calling AddDelegate after disposed: {0}", o);
                return false;
            }
            _delegateMap.Add(jso, o);
            return true;
        }

        public bool TryGetDelegate(JSValue jso, out ScriptDelegate o)
        {
            return _delegateMap.TryGetValue(jso, out o);
        }

        public bool RemoveDelegate(JSValue jso)
        {
            if (_disposed)
            {
                Diagnostics.Logger.Default.Debug("calling RemoveDelegate after disposed: {0}", jso);
                return false;
            }
            return _delegateMap.Remove(jso);
        }

        #endregion

        #region script value mapping 

        /// <summary>
        /// register a weak reference of ScriptValue in ObjectCache
        /// </summary>
        public void AddScriptValue(JSValue jso, ScriptValue o)
        {
            if (_disposed)
            {
                return;
            }
            _scriptValueMap.Add(jso, o);
        }

        public bool TryGetScriptValue(JSValue jso, out ScriptValue o)
        {
            ScriptValue value;
            if (_scriptValueMap.TryGetValue(jso, out value))
            {
                o = value;
                return true;
            }
            o = null;
            return false;
        }

        public bool RemoveScriptValue(JSValue jso)
        {
            if (_disposed)
            {
                return false;
            }
            return _scriptValueMap.Remove(jso);
        }

        #endregion
    }
}