using System;
using System.Linq;
using QuickJS.Core;

namespace QuickJS
{
    public class Callback : IDisposable
    {
        public object callback;

        private EngineContext context;
        public bool Destroyed { get; private set; } = false;

        public static Callback Noop = new Callback((object)null, null);

        public static Callback From(object value, EngineContext context = null, object thisVal = null,
            bool allowIndexedCallbacks = false)
        {
            if (value == null) return Noop;
            if (value is string s)
            {
                return context.CreateEventCallback(s, thisVal);
            }

            if (value is Callback cb) return cb;
            if (value is int cbi)
            {
                if (allowIndexedCallbacks) return new Callback(cbi, context);
                return Noop;
            }

            return new Callback(value, context);
        }

        public Callback(object callback, EngineContext context = null)
        {
            this.callback = callback;
            this.context = context;
        }

        public Callback(int index, EngineContext context)
        {
            this.context = context;
            this.callback = index;
        }

        public object Call()
        {
            return Call(new object[0]);
        }

        public object Call(params object[] args)
        {
            if (callback == null) return null;
            if (args == null) args = new object[0];

            if (callback is Callback c)
            {
                return c.Call(args);
            }
            else if (callback is Delegate d)
            {
                var parameters = d.Method.GetParameters();
                var argCount = parameters.Length;

                if (args.Length < argCount) args = args.Concat(new object[argCount - args.Length]).ToArray();
                if (args.Length > argCount) args = args.Take(argCount).ToArray();
                return d.DynamicInvoke(args);
            }
            else if (callback is int i)
            {
                var res = context.FireEventByRefCallback?.Call(i, args);
                (context.Engine as QuickJsEngine)?.Runtime?.ExecutePendingJob();
                return res;
            }
            else if (callback is Microsoft.ClearScript.ScriptObject so)
            {
                // TODO: because of an error in ClearScipt, arrays cannot be iterated (Mono bug?)
                so.Engine.Global.SetProperty("__temp__", so);
                so.Engine.Global.SetProperty("__args__", args?.ToList());
                var res = so.Engine?.Evaluate(null, true,
                    "var res = __temp__(...(__args__ || [])); delete __temp__; delete __args__; res;");
                return res;
            }
            else if (callback is QuickJS.ScriptFunction sf)
            {
                var res = sf.Invoke<object>(args);
                QuickJS.ScriptEngine.GetRuntime(sf.ctx)?.ExecutePendingJob();
                return res;
            }
            else if (callback is QuickJS.ScriptValue sv)
            {
                var sff = new QuickJS.ScriptFunction(QuickJS.ScriptEngine.GetContext(sv.ctx), sv);
                var res = sff.Invoke<object>(args);
                sff.Dispose();
                QuickJS.ScriptEngine.GetRuntime(sv.ctx)?.ExecutePendingJob();
                return res;
            }
            else if (callback is QuickJS.Native.JSValue qf)
            {
                var eg = (context?.Engine as QuickJsEngine);
                if (eg == null) return null;
                var sff = new QuickJS.ScriptFunction(eg.MainContext, qf);
                var res = sff.Invoke<object>(args);
                sff.Dispose();
                eg?.Runtime?.ExecutePendingJob();
                return res;
            }
            else
            {
                return null;
            }
        }
        
        public void Dispose()
        {
            Destroyed = true;
            callback = null;
            context = null;
        }
    }
}