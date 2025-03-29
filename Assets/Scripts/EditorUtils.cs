// https://discussions.unity.com/t/serialize-c-properties-how-to-with-code/683762
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[AttributeUsage(AttributeTargets.Field)]
public class SerializeProperty : PropertyAttribute
{
    public string PropertyName { get; private set; }

    public SerializeProperty(string propertyName)
    {
        PropertyName = propertyName;
    }
}

[CustomPropertyDrawer(typeof(SerializeProperty))]
public class SerializePropertyAttributeDrawer : PropertyDrawer
{
    private PropertyInfo propertyFieldInfo = null;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        UnityEngine.Object target = property.serializedObject.targetObject;

        // Find the property field using reflection, in order to get access to its getter/setter.
        if (propertyFieldInfo == null)
            propertyFieldInfo = target.GetType().GetProperty(((SerializeProperty)attribute).PropertyName,
                                                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyFieldInfo != null)
        {

            // Retrieve the value using the property getter:
            object value = propertyFieldInfo.GetValue(target, null);

            // Draw the property, checking for changes:
            EditorGUI.BeginChangeCheck();
            value = DrawProperty(position, property.propertyType, propertyFieldInfo.PropertyType, value, label);

            // If any changes were detected, call the property setter:
            if (EditorGUI.EndChangeCheck() && propertyFieldInfo != null)
            {

                // Record object state for undo:
                Undo.RecordObject(target, "Inspector");

                // Call property setter:
                propertyFieldInfo.SetValue(target, value, null);
            }

        }
        else
        {
            EditorGUI.LabelField(position, "Error: could not retrieve property.");
        }
    }

    private object DrawProperty(Rect position, SerializedPropertyType propertyType, Type type, object value, GUIContent label)
    {
        return propertyType switch
        {
            SerializedPropertyType.Integer => EditorGUI.IntField(position, label, (int)value),
            SerializedPropertyType.Boolean => EditorGUI.Toggle(position, label, (bool)value),
            SerializedPropertyType.Float => EditorGUI.FloatField(position, label, (float)value),
            SerializedPropertyType.String => EditorGUI.TextField(position, label, (string)value),
            SerializedPropertyType.Color => EditorGUI.ColorField(position, label, (Color)value),
            SerializedPropertyType.ObjectReference => EditorGUI.ObjectField(position, label, (UnityEngine.Object)value, type, true),
            SerializedPropertyType.ExposedReference => EditorGUI.ObjectField(position, label, (UnityEngine.Object)value, type, true),
            SerializedPropertyType.LayerMask => EditorGUI.LayerField(position, label, (int)value),
            SerializedPropertyType.Enum => EditorGUI.EnumPopup(position, label, (Enum)value),
            SerializedPropertyType.Vector2 => EditorGUI.Vector2Field(position, label, (Vector2)value),
            SerializedPropertyType.Vector3 => EditorGUI.Vector3Field(position, label, (Vector3)value),
            SerializedPropertyType.Vector4 => EditorGUI.Vector4Field(position, label, (Vector4)value),
            SerializedPropertyType.Rect => EditorGUI.RectField(position, label, (Rect)value),
            SerializedPropertyType.AnimationCurve => EditorGUI.CurveField(position, label, (AnimationCurve)value),
            SerializedPropertyType.Bounds => EditorGUI.BoundsField(position, label, (Bounds)value),
            _ => throw new NotImplementedException("Unimplemented propertyType " + propertyType + "."),
        };
    }
}