using System;
using QuickJS.Experimental;
using QuickJS.Native;

namespace QuickJS
{
    public class QuickJSApiBridge : IJSApiBridge, IDisposable
    {
        public JSPayloadHeader GetPayloadHeader(ScriptContext context, JSValue val)
        {
            throw new NotImplementedException();
        }

        public JSValue NewBridgeObject(ScriptContext context, object o, JSValue proto)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}