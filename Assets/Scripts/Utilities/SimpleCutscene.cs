using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class StartCutscene : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneSlide
    {
        public Sprite slide;
        public float duration = 3f;
    }

    [Header("CUTSCENE SETTINGS")]
    public CutsceneSlide[] slides;
    
    [Header("AUDIO SETTINGS")]
    public AudioClip firstAudioClip;
    public AudioClip secondAudioClip;
    public int firstSoundStartSlide = 0;
    public int secondSoundStartSlide = 0;
    
    public float fadeDuration = 1f;

    [Header("REFERENCES")]
    public Image displayImage;
    public AudioSource audioSource;

    [Header("ON CUTSCENE END")]
    public UnityEvent onCutsceneEnd;
    
    private int currentSlide = 0;
    private bool firstSoundPlayed = false;
    private bool secondSoundPlayed = false;

    void Start()
    {
            audioSource = gameObject.AddComponent<AudioSource>();
            StartCoroutine(PlayCutscene());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SkipToNext();
        }
    }

    private IEnumerator PlayCutscene()
    {
        // Начинаем с черного экрана
        displayImage.color = Color.black;
        displayImage.sprite = null;

        // Показываем все слайды по порядку
        for (int i = 0; i < slides.Length; i++)
        {
            // Плавный переход от черного к слайду
            yield return StartCoroutine(FadeFromBlackToSlide(slides[i].slide));
            
            // Проверяем нужно ли начать проигрывать первый звук
            if (!firstSoundPlayed && i >= firstSoundStartSlide)
            {
                PlayFirstSound();
                firstSoundPlayed = true;
            }
            
            // Проверяем нужно ли начать проигрывать второй звук
            if (!secondSoundPlayed && i >= secondSoundStartSlide)
            {
                PlaySecondSound();
                secondSoundPlayed = true;
            }
            
            // Ждем указанное для этого слайда время
            yield return new WaitForSeconds(slides[i].duration);
            
            // Плавный переход к черному (кроме последнего слайда)
            if (i < slides.Length - 1)
            {
                yield return StartCoroutine(FadeToBlack());
            }
        }

        // Финальный переход к черному в конце
        yield return StartCoroutine(FadeToBlack());
        yield return new WaitForSeconds(0.5f);
        
        // Разтемнение к прозрачному
        yield return StartCoroutine(FadeFromBlackToClear());

        // Вызываем действие после катсцены
        onCutsceneEnd?.Invoke();
    }

    // Плавный переход от черного к слайду
    private IEnumerator FadeFromBlackToSlide(Sprite slide)
    {
        // Устанавливаем новое изображение (пока черное)
        displayImage.sprite = slide;
        displayImage.color = Color.black;

        // Плавное появление слайда из черного
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            // Плавно меняем от черного к белому (полная видимость слайда)
            displayImage.color = Color.Lerp(Color.black, Color.white, progress);
            yield return null;
        }

        displayImage.color = Color.white;
    }

    // Плавный переход от слайда к черному
    private IEnumerator FadeToBlack()
    {
        float timer = 0f;
        Color startColor = displayImage.color;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            // Плавно меняем к черному
            displayImage.color = Color.Lerp(startColor, Color.black, progress);
            yield return null;
        }

        displayImage.color = Color.black;
    }

    // Плавный переход от черного к прозрачному (в самом конце)
    private IEnumerator FadeFromBlackToClear()
    {
        displayImage.color = Color.black;
        displayImage.sprite = null;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            // Плавно меняем от черного к прозрачному
            displayImage.color = new Color(0, 0, 0, 1 - progress);
            yield return null;
        }

        displayImage.color = new Color(0, 0, 0, 0);
    }

    private void PlayFirstSound()
    {
        if (firstAudioClip != null && audioSource != null)
        {
                audioSource.PlayOneShot(firstAudioClip);
        }
    }

    private void PlaySecondSound()
    {
        if (secondAudioClip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(secondAudioClip);
        }
    }
    

    private void SkipToNext()
    {
        StopAllCoroutines();

        currentSlide++;

        if (currentSlide < slides.Length)
        {
            // Показываем следующий слайд с черным переходом
            StartCoroutine(FadeFromBlackToSlide(slides[currentSlide].slide));

            // Проверяем нужно ли начать звуки
            CheckAndPlaySounds();

            StartCoroutine(WaitAndContinue());
        }
        else
        {
            // Запускаем финальное затемнение
            StartCoroutine(FinishCutscene());
        }
    }

    private void CheckAndPlaySounds()
    {
        // Проверяем первый звук
        if (!firstSoundPlayed && currentSlide >= firstSoundStartSlide)
        {
            PlayFirstSound();
            firstSoundPlayed = true;
        }
        
        // Проверяем второй звук
        if (!secondSoundPlayed && currentSlide >= secondSoundStartSlide)
        {
            PlaySecondSound();
            secondSoundPlayed = true;
        }
    }

    private IEnumerator WaitAndContinue()
    {
        // Ждем указанное для текущего слайда время
        yield return new WaitForSeconds(slides[currentSlide].duration);
        
        currentSlide++;
        
        if (currentSlide < slides.Length)
        {
            // Переход к черному и затем к следующему слайду
            yield return StartCoroutine(FadeToBlack());
            yield return StartCoroutine(FadeFromBlackToSlide(slides[currentSlide].slide));
            
            // Проверяем нужно ли начать звуки
            CheckAndPlaySounds();
            
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
        
        // Разтемнение к прозрачному
        yield return StartCoroutine(FadeFromBlackToClear());

        // Вызываем действие после катсцены
        onCutsceneEnd?.Invoke();
    }

    public void StopCutscene()
    {
        StopAllCoroutines();
        
        // Останавливаем все AudioSource
        AudioSource[] allAudioSources = GetComponents<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }
        
        onCutsceneEnd?.Invoke();
    }
}