using UnityEngine;


[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public sealed class UnitView :
    MonoBehaviour
{
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


    [SerializeField]
    private UnitFacing _Facing =
        UnitFacing.East;


    private SpriteRenderer _SpriteRenderer;

    private Animator _Animator;


    public UnitFacing Facing =>
        _Facing;


    private void Awake()
    {
        CacheComponents();
    }


    private void OnEnable()
    {
        ApplyFacing();
    }


    public void SetFacing(
        UnitFacing Facing
    )
    {
        _Facing =
            Facing;


        ApplyFacing();
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