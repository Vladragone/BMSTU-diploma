using System.Collections;
using UnityEngine;

public class ErrorSignal : MonoBehaviour
{
    [Header("Error Visual")]
    public Renderer signalRenderer;
    public Material normalMaterial;
    public Material errorMaterial;

    [Header("Error Audio")]
    public AudioSource audioSource;

    public void PlayError()
    {
        StopAllCoroutines();
        StartCoroutine(ErrorCoroutine());
    }

    private IEnumerator ErrorCoroutine()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }

        for (int i = 0; i < 6; i++)
        {
            if (signalRenderer != null)
            {
                signalRenderer.material = errorMaterial;
            }

            yield return new WaitForSeconds(0.15f);

            if (signalRenderer != null)
            {
                signalRenderer.material = normalMaterial;
            }

            yield return new WaitForSeconds(0.15f);
        }
    }
}