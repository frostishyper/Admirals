using UnityEditor;
using UnityEngine;


// Unity Editor Script - Not Attached To A GameObject
// Gives D20 outcome entries readable names and enforces legal D20 values.
[CustomPropertyDrawer(
    typeof(D20RollRange),
    true
)]
public class D20OutcomeEditor :
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
                BuildLabel(
                    Property
                ),
                true
            );


        if (!Property.isExpanded)
        {
            EditorGUI.EndProperty();

            return;
        }


        EditorGUI.indentLevel++;


        float CurrentY =
            HeaderRect.yMax +
            Spacing;


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


            if (Child.name ==
                "_MinimumRoll")
            {
                DrawD20Value(
                    ChildRect,
                    Child,
                    "Minimum Roll"
                );
            }
            else if (Child.name ==
                "_MaximumRoll")
            {
                DrawD20Value(
                    ChildRect,
                    Child,
                    "Maximum Roll"
                );
            }
            else
            {
                EditorGUI.PropertyField(
                    ChildRect,
                    Child,
                    true
                );
            }


            CurrentY +=
                ChildHeight +
                Spacing;
        }


        EditorGUI.indentLevel--;


        EditorGUI.EndProperty();
    }


    // D20 values are always restricted to the legal range of 1 through 20.
    private void DrawD20Value(
        Rect Position,
        SerializedProperty Property,
        string Label
    )
    {
        int CurrentValue =
            Mathf.Clamp(
                Property.intValue,
                1,
                20
            );


        int NewValue =
            EditorGUI.IntSlider(
                Position,
                Label,
                CurrentValue,
                1,
                20
            );


        Property.intValue =
            Mathf.Clamp(
                NewValue,
                1,
                20
            );
    }


    // Uses the authored Outcome Name and D20 range
    // instead of Unity's generic "Element X" label.
    private GUIContent BuildLabel(
        SerializedProperty Property
    )
    {
        SerializedProperty MinimumRoll =
            Property.FindPropertyRelative(
                "_MinimumRoll"
            );

        SerializedProperty MaximumRoll =
            Property.FindPropertyRelative(
                "_MaximumRoll"
            );

        SerializedProperty Outcome =
            Property.FindPropertyRelative(
                "_Outcome"
            );


        string OutcomeName =
            GetOutcomeName(
                Outcome
            );


        int Minimum =
            MinimumRoll != null
                ? Mathf.Clamp(
                    MinimumRoll.intValue,
                    1,
                    20
                )
                : 1;


        int Maximum =
            MaximumRoll != null
                ? Mathf.Clamp(
                    MaximumRoll.intValue,
                    1,
                    20
                )
                : 20;


        string RangeText;


        if (Minimum ==
            Maximum)
        {
            RangeText =
                Minimum.ToString();
        }
        else
        {
            RangeText =
                $"{Minimum}-{Maximum}";
        }


        if (string.IsNullOrWhiteSpace(
                OutcomeName))
        {
            OutcomeName =
                "Unnamed Outcome";
        }


        return
            new GUIContent(
                $"{OutcomeName} [{RangeText}]"
            );
    }


    // Every current D20 consumer stores its payload in "_Outcome",
    // and every authored outcome uses "_Name" for its readable label.
    private string GetOutcomeName(
        SerializedProperty Outcome
    )
    {
        if (Outcome == null)
        {
            return
                string.Empty;
        }


        SerializedProperty Name =
            Outcome.FindPropertyRelative(
                "_Name"
            );


        if (Name == null)
        {
            return
                string.Empty;
        }


        return
            Name.stringValue;
    }
}