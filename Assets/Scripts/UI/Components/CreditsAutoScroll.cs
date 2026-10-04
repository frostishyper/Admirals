using UnityEngine;

// Attach To Scroll -> Content
[RequireComponent(typeof(RectTransform))]
public class CreditsAutoScroll : MonoBehaviour
{
    [SerializeField] private float ScrollSpeed = 30f;
    private RectTransform Content;
    private float LoopHeight;
    private void Awake()
    {
        Content = GetComponent<RectTransform>();
    }

    private void Start()
    {
        // Total Content Height Is Two Identical Copies Stacked, So Half 
        // (Copies Are Delimited By The "Loop" EGame Object In The Heirarchy)
        LoopHeight = Content.rect.height * 0.5f;
    }

    private void Update()
    {
        Vector2 Pos = Content.anchoredPosition;
        Pos.y += ScrollSpeed * Time.deltaTime;

        if (Pos.y >= LoopHeight)
        {
            Pos.y -= LoopHeight;
        }
        Content.anchoredPosition = Pos;
    }
}
