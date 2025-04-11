using UnityEngine;

namespace QuickJS.Unity
{
    public class JSToggleHintAttribute: PropertyAttribute
    {
        public string text { get; set; }

        public JSToggleHintAttribute(string text)
        {
            this.text = text;
        }
    }
}