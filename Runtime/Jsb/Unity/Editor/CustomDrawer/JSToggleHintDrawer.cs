using UnityEditor;
using UnityEngine;

namespace QuickJS.Unity
{
    [CustomPropertyDrawer((typeof(JSToggleHintAttribute)))]
    public class JSToggleHintDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            var ta = attribute as JSToggleHintAttribute;
            property.boolValue = EditorGUI.Toggle(pos, ta.text, property.boolValue);
        }
    }
}