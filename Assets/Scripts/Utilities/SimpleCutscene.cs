using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SimpleCutscene : MonoBehaviour
{
    [Header("CUTSCENE SETTINGS")]
    public Sprite[] slides;
    public float slideDuration = 3f;
    public float fadeDuration = 1f;

    [Header("REFERENCES")]
    public Image displayImage;

    [Header("ON CUTSCENE END")]
    public UnityEvent onCutsceneEnd;
    private int currentSlide = 0;

    void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        // Пропуск по клику мыши
        if (Input.GetMouseButtonDown(0))
        {
            SkipToNext();
        }
    }

    private IEnumerator PlayCutscene()
    {
        // Показываем все слайды по порядку
        for (int i = 0; i < slides.Length; i++)
        {
            yield return StartCoroutine(ShowSlide(slides[i]));
            yield return new WaitForSeconds(slideDuration);
            
            // Плавно скрываем слайд (кроме последнего)
            if (i < slides.Length - 1)
            {
                yield return StartCoroutine(FadeOut());
            }
        }

        // Затемнение в конце
        yield return StartCoroutine(FadeToBlack());
        yield return new WaitForSeconds(0.5f);
        
        // Разтемнение
        yield return StartCoroutine(FadeFromBlack());

        // Вызываем действие после катсцены
        onCutsceneEnd?.Invoke();
    }

    private IEnumerator ShowSlide(Sprite slide)
    {
        // Устанавливаем новое изображение
        displayImage.sprite = slide;
        displayImage.color = new Color(1, 1, 1, 0);

        // Плавное появление
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeDuration;
            displayImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        displayImage.color = Color.white;
    }

    private IEnumerator FadeOut()
    {
        // Плавное исчезновение
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1 - (timer / fadeDuration);
            displayImage.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }

    private IEnumerator FadeToBlack()
    {
        // Плавное затемнение к черному
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeDuration;
            displayImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        displayImage.color = Color.black;
    }

    private IEnumerator FadeFromBlack()
    {
        // Плавное разтемнение от черного
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1 - (timer / fadeDuration);
            displayImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        displayImage.color = new Color(0, 0, 0, 0);
    }

    private void SkipToNext()
    {
        StopAllCoroutines();
        
        currentSlide++;
        
        if (currentSlide < slides.Length)
        {
            // Показываем следующий слайд
            StartCoroutine(ShowSlide(slides[currentSlide]));
            StartCoroutine(WaitAndContinue());
        }
        else
        {
            // Запускаем финальное затемнение
            StartCoroutine(FinishCutscene());
        }
    }

    private IEnumerator WaitAndContinue()
    {
        yield return new WaitForSeconds(slideDuration);
        
        currentSlide++;
        
        if (currentSlide < slides.Length)
        {
            yield return StartCoroutine(FadeOut());
            yield return StartCoroutine(ShowSlide(slides[currentSlide]));
            StartCoroutine(WaitAndContinue());
        }
        else
        {
            StartCoroutine(FinishCutscene());
        }
    }

    private IEnumerator FinishCutscene()
    {
        // Затемнение в конце
        yield return StartCoroutine(FadeToBlack());
        yield return new WaitForSeconds(0.5f);
        
        // Разтемнение
        yield return StartCoroutine(FadeFromBlack());

        // Вызываем действие после катсцены
        onCutsceneEnd?.Invoke();
    }
}