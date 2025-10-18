using UnityEngine;
using UnityEngine.EventSystems;

public class ScaleOnEnter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private float scaleSpeed = 5f;
    [SerializeField] private float enterScale = 1.25f;
    private Vector3 desiredScale;
    private Vector3 defaultScale;

    public void OnPointerClick(PointerEventData eventData)
    {
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        desiredScale = enterScale * Vector3.one;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        desiredScale = defaultScale;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultScale = transform.localScale;

        if (defaultScale == Vector3.zero)
            defaultScale = Vector3.one;

        desiredScale = defaultScale;

    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = Vector3.MoveTowards(transform.localScale, desiredScale, scaleSpeed * Time.deltaTime);
    }

    void OnDisable()
    {
        desiredScale = defaultScale;
        transform.localScale = defaultScale;
    }
}
