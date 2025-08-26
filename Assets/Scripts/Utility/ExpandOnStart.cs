using System.Collections;
using UnityEngine;

public class ExpandOnStart : Expand
{
    public float ExpandDelay { get => expandDelay; set => expandDelay = value; }
    [SerializeField] private float expandDelay = 0f;
    [Tooltip("Set to negative value if you don't want this object to automatically contract.")]
    [SerializeField] private float contractDelay = -1f;
    private bool resetExpand = false;

    void OnEnable()
    {
        if (resetExpand)
        {
            Reset();
        }
        StartCoroutine(ExpandCoroutine());
    }

    private IEnumerator ExpandCoroutine()
    {
        resetExpand = contractDelay < 0; // Only reset if contractDelay is negative
        yield return new WaitForSeconds(expandDelay);
        Display(true, () =>
        {
            if (contractDelay > 0)
            {
                StartCoroutine(ContractCoroutine());
            }
        });
    }

    private IEnumerator ContractCoroutine()
    {
        yield return new WaitForSeconds(contractDelay);
        Display(false, () =>
        {
            gameObject.SetActive(false);
        });
    }
}
