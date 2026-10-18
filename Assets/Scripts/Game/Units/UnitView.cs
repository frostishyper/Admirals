using System;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class UnitView :
    MonoBehaviour
{
    [Serializable]
    private sealed class AnimatorTagBoolBinding
    {
        [SerializeField]
        [ScriptableObjectPicker]
        private Definition_UnitTag _Tag;

        [AnimatorBoolParameter]
        [SerializeField]
        private string _AnimatorBoolParameter;


        public Definition_UnitTag Tag =>
            _Tag;

        public string AnimatorBoolParameter =>
            _AnimatorBoolParameter;
    }


    private const float FacingNorth =
        0f;

    private const float FacingEast =
        1f;

    private const float FacingSouth =
        2f;


    private static readonly int FacingParameter =
        Animator.StringToHash(
            "Facing"
        );

    private static readonly int EmbarkedParameter =
        Animator.StringToHash(
            "Embarked"
        );


    [SerializeField]
    private UnitFacing _Facing =
        UnitFacing.East;


    [Header("Runtime Tag Presentation")]
    [SerializeField]
    private AnimatorTagBoolBinding[] _AnimatorTagBoolBindings =
        Array.Empty<AnimatorTagBoolBinding>();


    private SpriteRenderer _SpriteRenderer;

    private Animator _Animator;

    private ParticipantUnitTagSnapshot[] _RuntimeTags =
        Array.Empty<ParticipantUnitTagSnapshot>();


    public UnitFacing Facing =>
        _Facing;


    public void SetColor(
        Color Color
    )
    {
        if (_SpriteRenderer == null)
        {
            CacheComponents();
        }


        if (_SpriteRenderer != null)
        {
            _SpriteRenderer.color =
                Color;
        }
    }


    private void Awake()
    {
        CacheComponents();
    }


    private void OnEnable()
    {
        ApplyFacing();
        ApplyTags();
    }


    public void SetFacing(
        UnitFacing Facing
    )
    {
        _Facing =
            Facing;


        ApplyFacing();
    }


    public void SetTags(
        IReadOnlyList<ParticipantUnitTagSnapshot> Tags
    )
    {
        if (Tags == null ||
            Tags.Count == 0)
        {
            _RuntimeTags =
                Array.Empty<ParticipantUnitTagSnapshot>();
        }
        else
        {
            _RuntimeTags =
                new ParticipantUnitTagSnapshot[Tags.Count];


            for (int i = 0;
                i < Tags.Count;
                i++)
            {
                _RuntimeTags[i] =
                    Tags[i];
            }
        }


        ApplyTags();
    }


    public void SetEmbarkedPresentationLevel(int Level)
    {
        if (_Animator == null)
            CacheComponents();

        if (_Animator == null ||
            !HasAnimatorParameter(EmbarkedParameter, AnimatorControllerParameterType.Int))
        {
            return;
        }

        _Animator.SetInteger(
            EmbarkedParameter,
            Mathf.Clamp(Level, 0, 3)
        );
    }


    private bool HasAnimatorParameter(
        int NameHash,
        AnimatorControllerParameterType Type)
    {
        AnimatorControllerParameter[] Parameters = _Animator.parameters;

        for (int i = 0; i < Parameters.Length; i++)
        {
            if (Parameters[i].nameHash == NameHash &&
                Parameters[i].type == Type)
            {
                return true;
            }
        }

        return false;
    }


    private void ApplyTags()
    {
        if (_Animator == null)
        {
            CacheComponents();
        }


        if (_Animator == null ||
            _AnimatorTagBoolBindings == null)
        {
            return;
        }


        for (int BindingIndex = 0;
            BindingIndex < _AnimatorTagBoolBindings.Length;
            BindingIndex++)
        {
            AnimatorTagBoolBinding Binding =
                _AnimatorTagBoolBindings[BindingIndex];


            if (Binding == null ||
                Binding.Tag == null ||
                string.IsNullOrWhiteSpace(
                    Binding.AnimatorBoolParameter))
            {
                continue;
            }


            bool IsPresent =
                false;


            if (_RuntimeTags != null)
            {
                for (int TagIndex = 0;
                    TagIndex < _RuntimeTags.Length;
                    TagIndex++)
                {
                    if (string.Equals(
                            _RuntimeTags[TagIndex]?.DefinitionID,
                            Binding.Tag.ID,
                            StringComparison.Ordinal))
                    {
                        IsPresent =
                            true;

                        break;
                    }
                }
            }


            _Animator.SetBool(
                Animator.StringToHash(
                    Binding.AnimatorBoolParameter
                ),
                IsPresent
            );
        }
    }


    private void ApplyFacing()
    {
        if (_SpriteRenderer == null ||
            _Animator == null)
        {
            CacheComponents();
        }


        if (_SpriteRenderer == null ||
            _Animator == null)
        {
            return;
        }


        _SpriteRenderer.flipX =
            _Facing ==
            UnitFacing.West;


        float AnimatorFacing =
            _Facing switch
            {
                UnitFacing.North =>
                    FacingNorth,

                UnitFacing.South =>
                    FacingSouth,

                UnitFacing.East =>
                    FacingEast,

                UnitFacing.West =>
                    FacingEast,

                _ =>
                    FacingEast
            };


        _Animator.SetFloat(
            FacingParameter,
            AnimatorFacing
        );
    }


    private void CacheComponents()
    {
        _SpriteRenderer =
            GetComponent<SpriteRenderer>();


        _Animator =
            GetComponent<Animator>();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        CacheComponents();


        if (_SpriteRenderer != null)
        {
            _SpriteRenderer.flipX =
                _Facing ==
                UnitFacing.West;
        }


        if (Application.isPlaying)
        {
            ApplyFacing();
        }
    }

#endif
}
