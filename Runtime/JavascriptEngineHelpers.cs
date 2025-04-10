namespace QuickJS
{
    public class JavascriptEngineHelpers
    {

        public static bool HasEngine => true;
        
        public static IJavaScriptEngineFactory GetEngineFactory(JavascriptEngineType type)
        {
            switch (type)
            {
                case JavascriptEngineType.QuickJS:
                    return new QuickJsEngineFactory();
                
                case JavascriptEngineType.ClearScript:
                    return new ClearScriptEngineFactory();

                default:
                    throw new System.Exception("Could not find a valid scripting engine. ");
            }
        }
    }
}