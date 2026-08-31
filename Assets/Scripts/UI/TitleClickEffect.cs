using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class TitleClickEffect : MonoBehaviour
{
    [SerializeField] private TitleInteraction titleInteraction;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private RectTransform logoRunner;
    [SerializeField, Min(0f)] private float transitionDelay = 0.4f;
    [SerializeField, Min(1f)] private float runnerSpeed = 170f;

    private bool clicked;
    private float runnerTime;
    private float shotTimer;
    private static Sprite dustSprite;

    private void Awake()
    {
        if (titleInteraction == null)
            titleInteraction = GetComponent<TitleInteraction>();

        if (titleLogo == null)
        {
            GameObject logo = GameObject.Find("TitleLogo");
            if (logo != null)
                titleLogo = logo.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        UpdateRunner();

        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        if (!clicked && (mouseClicked || touched))
            StartCoroutine(PlayAndTransition());
    }

    private void UpdateRunner()
    {
        if (clicked || logoRunner == null)
            return;

        runnerTime += Time.unscaledDeltaTime;
        if (runnerTime < 1.8f)
            return;

        Vector2 position = logoRunner.anchoredPosition;
        position.x += runnerSpeed * Time.unscaledDeltaTime;
        position.y = -122f + Mathf.Abs(Mathf.Sin(runnerTime * 12f)) * 12f;
        if (position.x > 500f)
            position.x = -500f;
        logoRunner.anchoredPosition = position;

        shotTimer -= Time.unscaledDeltaTime;
        if (shotTimer <= 0f)
        {
            shotTimer = 0.55f;
            PlayLogoShot();
        }
    }

    private void PlayLogoShot()
    {
        if (titleLogo == null || logoRunner == null || !(titleLogo.parent is RectTransform parent))
            return;

        Vector2 origin = parent.InverseTransformPoint(logoRunner.position) + new Vector3(48f, 5f);
        Color[] colors =
        {
            new Color(0.18f, 0.04f, 0.30f),
            new Color(0.43f, 0.08f, 0.72f),
            new Color(0.62f, 0.24f, 0.92f),
            new Color(0.12f, 0.43f, 0.73f)
        };

        for (int i = 0; i < 7; i++)
        {
            Spawn(parent, origin + new Vector2(Random.Range(25f, 75f), Random.Range(-18f, 18f)),
                new Vector2(Random.Range(70f, 190f), Random.Range(-90f, 130f)),
                Random.Range(7f, 18f), colors[Random.Range(0, colors.Length)],
                Random.Range(0.35f, 0.65f), false);
        }
    }

    private IEnumerator PlayAndTransition()
    {
        clicked = true;
        PlayEffect();
        yield return new WaitForSecondsRealtime(transitionDelay);

        if (titleInteraction != null)
            titleInteraction.GameStart();
        else
            clicked = false;
    }

    private void PlayEffect()
    {
        if (titleLogo == null || !(titleLogo.parent is RectTransform parent))
            return;

        Vector2 origin = titleLogo.anchoredPosition + new Vector2(0f, -titleLogo.rect.height * 0.34f);

        for (int i = 0; i < 12; i++)
        {
            Spawn(parent, origin + new Vector2(Random.Range(-360f, 360f), Random.Range(-18f, 22f)),
                new Vector2(Random.Range(-80f, 80f), Random.Range(45f, 125f)),
                Random.Range(55f, 125f),
                new Color(0.62f, 0.58f, 0.64f, Random.Range(0.22f, 0.45f)),
                Random.Range(0.45f, 0.75f), true);
        }

        Color[] colors =
        {
            new Color(0.18f, 0.04f, 0.30f),
            new Color(0.43f, 0.08f, 0.72f),
            new Color(0.62f, 0.24f, 0.92f),
            new Color(0.12f, 0.43f, 0.73f)
        };

        for (int i = 0; i < 28; i++)
        {
            Spawn(parent, origin + new Vector2(Random.Range(-430f, 430f), Random.Range(-16f, 16f)),
                new Vector2(Random.Range(-150f, 150f), Random.Range(-230f, 90f)),
                Random.Range(9f, 28f), colors[Random.Range(0, colors.Length)],
                Random.Range(0.45f, 0.9f), false);
        }
    }

    private void Spawn(RectTransform parent, Vector2 position, Vector2 velocity,
        float size, Color color, float lifetime, bool isDust)
    {
        GameObject particle = new GameObject(isDust ? "TitleDust" : "TitlePixel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = particle.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.SetAsLastSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = isDust ? new Vector2(size * 1.7f, size) : Vector2.one * size;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, 35f));

        Image image = particle.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        if (isDust)
            image.sprite = GetDustSprite();

        StartCoroutine(Animate(rect, image, velocity, lifetime, isDust));
    }

    private IEnumerator Animate(RectTransform rect, Image image, Vector2 velocity,
        float lifetime, bool isDust)
    {
        float elapsed = 0f;
        Color startColor = image.color;
        float spin = Random.Range(-220f, 220f);

        while (elapsed < lifetime && rect != null)
        {
            float delta = Time.unscaledDeltaTime;
            elapsed += delta;
            float t = Mathf.Clamp01(elapsed / lifetime);
            velocity.y -= (isDust ? 20f : 420f) * delta;
            rect.anchoredPosition += velocity * delta;
            rect.Rotate(0f, 0f, spin * delta);
            float scale = isDust ? Mathf.Lerp(0.55f, 1.45f, t) : Mathf.Lerp(1f, 0.45f, t);
            rect.localScale = Vector3.one * scale;
            image.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
    }

    private static Sprite GetDustSprite()
    {
        if (dustSprite != null)
            return dustSprite;

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeTitleDust";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        Vector2 center = Vector2.one * (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
            float alpha = 1f - Mathf.SmoothStep(0.55f, 1f, distance);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }

        texture.SetPixels(pixels);
        texture.Apply();
        dustSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        dustSprite.name = "RuntimeTitleDustSprite";
        return dustSprite;
    }
}
