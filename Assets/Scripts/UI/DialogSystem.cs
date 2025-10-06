using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ReplicaSettings
{
    [TextArea(3, 5)]
    public string text;
    public float displayDuration = 3f;
}

// ИЗМЕНИТЕ ИМЯ КЛАССА НА DialogSystem
public class DialogSystem : MonoBehaviour
{
    [Header("Настройки реплик")]
    [SerializeField] private List<ReplicaSettings> replicaSettings = new List<ReplicaSettings>
    {
        new ReplicaSettings { text = "Хей, ты! Стой! не отправляй это в брак!", displayDuration = 3f },
        new ReplicaSettings { text = "Это мне очень нужно, очень очень", displayDuration = 2f },
        new ReplicaSettings { text = "Я искал это уже несколько лет тут", displayDuration = 3f },
        new ReplicaSettings { text = "Тебе наверно интересно, кто я?", displayDuration = 2f },
        new ReplicaSettings { text = "... Я..я.я беглец", displayDuration = 2f },
        new ReplicaSettings { text = "Вот уже очень много времени я ищу этот куб", displayDuration = 3f },
        new ReplicaSettings { text = "Поэтому прошу, умоляю! Отдай мне его", displayDuration = 3f },
        new ReplicaSettings { text = "Только только, по-жа-луй-ста сначала найди его сердце, шарик такой маленький, он часто пропадает, но рано или поздно возвращается", displayDuration = 5f }
    };

    [Header("Настройки отображения")]
    [SerializeField] private Text textPrefab;
    [SerializeField] private Transform textContainer;
    [SerializeField] private float charDelay = 0.05f;
    [SerializeField] private int maxVisibleReplicas = 3;
    [SerializeField] private float verticalSpacing = 30f;

    [Header("Анимация")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Управление клавишей")]
    [SerializeField] private KeyCode triggerKey = KeyCode.E;
    [SerializeField] private bool showAllAtOnce = false;

    private Queue<ReplicaSettings> replicaQueue = new Queue<ReplicaSettings>();
    private List<Text> activeTexts = new List<Text>();
    private List<Text> textPool = new List<Text>();
    private bool isShowing = false;
    private bool dialogStarted = false;

    void Start()
    {
        InitializeTextPool();
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !isShowing)
        {
            if (!dialogStarted)
            {
                StartDialog();
            }
            else
            {
                ShowNextReplica();
            }
        }
    }

    void InitializeTextPool()
    {
        for (int i = 0; i < maxVisibleReplicas; i++)
        {
            CreateNewTextObject();
        }
    }

    void CreateNewTextObject()
    {
        Text newText = Instantiate(textPrefab, textContainer);
        newText.gameObject.SetActive(false);
        textPool.Add(newText);
    }

    void StartDialog()
    {
        dialogStarted = true;
        
        replicaQueue.Clear();
        foreach (ReplicaSettings settings in replicaSettings)
        {
            replicaQueue.Enqueue(settings);
        }

        ShowNextReplica();
    }

    public void ShowNextReplica()
    {
        if (replicaQueue.Count > 0 && !isShowing)
        {
            StartCoroutine(ShowReplicaCoroutine(replicaQueue.Dequeue()));
        }
        else if (replicaQueue.Count == 0)
        {
            dialogStarted = false;
        }
    }

    IEnumerator ShowReplicaCoroutine(ReplicaSettings settings)
    {
        isShowing = true;

        if (textPool.Count == 0)
            CreateNewTextObject();

        Text currentText = textPool[0];
        textPool.RemoveAt(0);
        
        currentText.gameObject.SetActive(true);
        currentText.text = "";
        currentText.rectTransform.anchoredPosition = GetStartPosition();

        yield return StartCoroutine(FadeText(currentText, 0, 1, fadeInDuration));

        foreach (char c in settings.text)
        {
            currentText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        ShiftTextsUp();

        activeTexts.Add(currentText);

        yield return new WaitForSeconds(settings.displayDuration);

        yield return StartCoroutine(FadeText(currentText, 1, 0, fadeOutDuration));

        currentText.gameObject.SetActive(false);
        textPool.Add(currentText);
        activeTexts.Remove(currentText);
        isShowing = false;

        if (showAllAtOnce && replicaQueue.Count > 0)
        {
            ShowNextReplica();
        }
    }

    IEnumerator FadeText(Text text, float from, float to, float duration)
    {
        float elapsed = 0;
        Color color = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, fadeCurve.Evaluate(elapsed / duration));
            text.color = color;
            yield return null;
        }

        color.a = to;
        text.color = color;
    }

    Vector2 GetStartPosition()
    {
        return new Vector2(0, 0);
    }

    void ShiftTextsUp()
    {
        foreach (Text text in activeTexts)
        {
            if (text.gameObject.activeInHierarchy)
            {
                Vector2 currentPos = text.rectTransform.anchoredPosition;
                text.rectTransform.anchoredPosition = new Vector2(currentPos.x, currentPos.y + verticalSpacing);
            }
        }
    }

    [ContextMenu("Показать следующую реплику")]
    void ShowNextReplicaEditor()
    {
        if (!isShowing)
        {
            if (replicaQueue.Count == 0)
            {
                StartDialog();
            }
            else
            {
                ShowNextReplica();
            }
        }
    }

    public void StartDialogExternally()
    {
        if (!isShowing && !dialogStarted)
        {
            StartDialog();
        }
    }

    public void ResetDialog()
    {
        dialogStarted = false;
        replicaQueue.Clear();
        
        foreach (Text text in activeTexts)
        {
            text.gameObject.SetActive(false);
            textPool.Add(text);
        }
        activeTexts.Clear();
        
        isShowing = false;
    }
}