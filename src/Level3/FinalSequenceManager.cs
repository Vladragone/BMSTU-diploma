using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalSequenceManager : MonoBehaviour
{
    public static FinalSequenceManager Instance;

    [Header("UI")]
    public Image fadeImage;
    public TextMeshProUGUI finalText;

    [Header("Settings")]
    public float fadeSpeed = 1f;
    public float textHoldTime = 2f;

    private bool alreadyPlaying;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayFinalSequence()
    {
        if (alreadyPlaying)
            return;

        alreadyPlaying = true;

        StartCoroutine(FinalCoroutine());
    }

    private IEnumerator FinalCoroutine()
    {
        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;

            SetFadeAlpha(alpha);

            yield return null;
        }

        alpha = 0f;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;

            SetTextAlpha(alpha);

            yield return null;
        }

        yield return new WaitForSeconds(textHoldTime);

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;

            SetTextAlpha(alpha);

            yield return null;
        }

        SetTextAlpha(0f);

        SetFadeAlpha(1f);

        Debug.Log("[FINAL] Игра завершена");

        Application.Quit();
    }

    private void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void SetTextAlpha(float alpha)
    {
        Color color = finalText.color;
        color.a = alpha;
        finalText.color = color;
    }
}
