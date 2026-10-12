using UnityEditor;
using UnityEngine;


// Unity Editor Script - Not Attached To A GameObject
// Gives Unit Weapon Slots readable labels based on the mounted Weapon and capacity.
[CustomPropertyDrawer(
    typeof(UnitWeaponSlot)
)]
public class UnitWeaponSlotEditor :
    PropertyDrawer
{
    private const float Spacing =
        2f;


    public override float GetPropertyHeight(
        SerializedProperty Property,
        GUIContent Label
    )
    {
        if (!Property.isExpanded)
        {
            return
                EditorGUIUtility.singleLineHeight;
        }


        float Height =
            EditorGUIUtility.singleLineHeight;


        SerializedProperty Child =
            Property.Copy();

        SerializedProperty End =
            Child.GetEndProperty();


        bool EnterChildren =
            true;


        while (Child.NextVisible(
            EnterChildren) &&
            !SerializedProperty.EqualContents(
                Child,
                End))
        {
            EnterChildren =
                false;


            if (Child.depth !=
                Property.depth + 1)
            {
                continue;
            }


            Height +=
                Spacing +
                EditorGUI.GetPropertyHeight(
                    Child,
                    true
                );
        }


        return
            Height;
    }


    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label
    )
    {
        EditorGUI.BeginProperty(
            Position,
            Label,
            Property
        );


        float LineHeight =
            EditorGUIUtility.singleLineHeight;


        Rect HeaderRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width,
                LineHeight
            );


        Property.isExpanded =
            EditorGUI.Foldout(
                HeaderRect,
                Property.isExpanded,
                BuildWeaponLabel(
                    Property
                ),
                true
            );


        if (!Property.isExpanded)
        {
            EditorGUI.EndProperty();

            return;
        }


        DrawChildren(
            Position,
            Property,
            HeaderRect.yMax +
            Spacing
        );


        EditorGUI.EndProperty();
    }


    // Uses the mounted Weapon's authored Name and the Slot's maximum Charges.
    private GUIContent BuildWeaponLabel(
        SerializedProperty Property
    )
    {
        SerializedProperty WeaponProperty =
            Property.FindPropertyRelative(
                "_Weapon"
            );

        SerializedProperty ChargesProperty =
            Property.FindPropertyRelative(
                "_MaxCharges"
            );


        string WeaponName =
            "Empty Weapon Slot";


        if (WeaponProperty != null &&
            WeaponProperty.objectReferenceValue
                is Definition_Weapon Weapon)
        {
            WeaponName =
                string.IsNullOrWhiteSpace(
                    Weapon.Name)
                    ? Weapon.name
                    : Weapon.Name;
        }


        int Charges =
            ChargesProperty != null
                ? ChargesProperty.intValue
                : 0;


        return
            new GUIContent(
                $"{WeaponName} [{Charges} Charges]"
            );
    }


    // Draws the actual serialized Slot fields below the custom header.
    private void DrawChildren(
        Rect Position,
        SerializedProperty Property,
        float CurrentY
    )
    {
        EditorGUI.indentLevel++;


        SerializedProperty Child =
            Property.Copy();

        SerializedProperty End =
            Child.GetEndProperty();


        bool EnterChildren =
            true;


        while (Child.NextVisible(
            EnterChildren) &&
            !SerializedProperty.EqualContents(
                Child,
                End))
        {
            EnterChildren =
                false;


            if (Child.depth !=
                Property.depth + 1)
            {
                continue;
            }


            float ChildHeight =
                EditorGUI.GetPropertyHeight(
                    Child,
                    true
                );


            Rect ChildRect =
                new Rect(
                    Position.x,
                    CurrentY,
                    Position.width,
                    ChildHeight
                );


            EditorGUI.PropertyField(
                ChildRect,
                Child,
                true
            );


            CurrentY +=
                ChildHeight +
                Spacing;
        }


        EditorGUI.indentLevel--;
    }
}


// Unity Editor Script - Not Attached To A GameObject
// Gives Unit System Slots readable labels based on the mounted System and capacity.
[CustomPropertyDrawer(
    typeof(UnitSystemSlot)
)]
public class UnitSystemSlotEditor :
    PropertyDrawer
{
    private const float Spacing =
        2f;


    public override float GetPropertyHeight(
        SerializedProperty Property,
        GUIContent Label
    )
    {
        if (!Property.isExpanded)
        {
            return
                EditorGUIUtility.singleLineHeight;
        }


        float Height =
            EditorGUIUtility.singleLineHeight;


        SerializedProperty Child =
            Property.Copy();

        SerializedProperty End =
            Child.GetEndProperty();


        bool EnterChildren =
            true;


        while (Child.NextVisible(
            EnterChildren) &&
            !SerializedProperty.EqualContents(
                Child,
                End))
        {
            EnterChildren =
                false;


            if (Child.depth !=
                Property.depth + 1)
            {
                continue;
            }


            Height +=
                Spacing +
                EditorGUI.GetPropertyHeight(
                    Child,
                    true
                );
        }


        return
            Height;
    }


    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label
    )
    {
        EditorGUI.BeginProperty(
            Position,
            Label,
            Property
        );


        float LineHeight =
            EditorGUIUtility.singleLineHeight;


        Rect HeaderRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width,
                LineHeight
            );


        Property.isExpanded =
            EditorGUI.Foldout(
                HeaderRect,
                Property.isExpanded,
                BuildSystemLabel(
                    Property
                ),
                true
            );


        if (!Property.isExpanded)
        {
            EditorGUI.EndProperty();

            return;
        }


        DrawChildren(
            Position,
            Property,
            HeaderRect.yMax +
            Spacing
        );


        EditorGUI.EndProperty();
    }


    // Uses the mounted System's authored Name and the Slot's maximum Charges.
    private GUIContent BuildSystemLabel(
        SerializedProperty Property
    )
    {
        SerializedProperty SystemProperty =
            Property.FindPropertyRelative(
                "_System"
            );

        SerializedProperty ChargesProperty =
            Property.FindPropertyRelative(
                "_MaxCharges"
            );


        string SystemName =
            "Empty System Slot";


        if (SystemProperty != null &&
            SystemProperty.objectReferenceValue
                is Definition_System System)
        {
            SystemName =
                string.IsNullOrWhiteSpace(
                    System.Name)
                    ? System.name
                    : System.Name;
        }


        int Charges =
            ChargesProperty != null
                ? ChargesProperty.intValue
                : 0;


        return
            new GUIContent(
                $"{SystemName} [{Charges} Charges]"
            );
    }


    // Draws the actual serialized Slot fields below the custom header.
    private void DrawChildren(
        Rect Position,
        SerializedProperty Property,
        float CurrentY
    )
    {
        EditorGUI.indentLevel++;


        SerializedProperty Child =
            Property.Copy();

        SerializedProperty End =
            Child.GetEndProperty();


        bool EnterChildren =
            true;


        while (Child.NextVisible(
            EnterChildren) &&
            !SerializedProperty.EqualContents(
                Child,
                End))
        {
            EnterChildren =
                false;


            if (Child.depth !=
                Property.depth + 1)
            {
                continue;
            }


            float ChildHeight =
                EditorGUI.GetPropertyHeight(
                    Child,
                    true
                );


            Rect ChildRect =
                new Rect(
                    Position.x,
                    CurrentY,
                    Position.width,
                    ChildHeight
                );


            EditorGUI.PropertyField(
                ChildRect,
                Child,
                true
            );


            CurrentY +=
                ChildHeight +
                Spacing;
        }


        EditorGUI.indentLevel--;
    }
}