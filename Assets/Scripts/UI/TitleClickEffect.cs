using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class TitleClickEffect : MonoBehaviour
{
    private const int ImpactEffectSortingOrder = 1000;

    [SerializeField] private TitleInteraction titleInteraction;
    [SerializeField] private RectTransform titleLogo;
    [SerializeField] private RectTransform logoRunner;
    [SerializeField, Min(0f)] private float transitionDelay = 0.4f;
    [SerializeField, Tooltip("プレイヤー画像の中心を基準にした銃口位置（UIピクセル）")]
    private Vector2 muzzleLocalOffset = new Vector2(40f, 8f);
    [SerializeField, Tooltip("プレイヤー画像の中心を基準にした着弾位置（UIピクセル）")]
    private Vector2 impactLocalOffset = new Vector2(300f, 100f);
    [SerializeField, Min(0.05f), Tooltip("着弾判定の大きさ")]
    private float impactColliderSize = 0.45f;
    [SerializeField, Min(0.05f), Tooltip("通常弾の発射間隔（秒）")]
    private float normalShotInterval = 0.55f;
    private bool clicked;
    private float runnerTime;
    private float shotTimer;
    private float runnerBaseY;
    private GameObject gameplayBulletPrefab;
    private GameObject gameplayImpactPrefab;
    private AudioClip gameplayShotSound;
    private Canvas titleCanvas;
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

        if (logoRunner != null)
            runnerBaseY = logoRunner.anchoredPosition.y;

        gameplayBulletPrefab = Resources.Load<GameObject>("TitlePanorama/PlayerBullet");
        gameplayImpactPrefab = Resources.Load<GameObject>("TitlePanorama/TerrainPixelImpact");
        gameplayShotSound = Resources.Load<AudioClip>("TitlePanorama/PlayerShot");

        // Screen Space Overlay always renders after world particles. Moving the
        // title canvas to the camera allows the impact (order 1000) to appear in
        // front while the title itself (order 500) stays above the scenery.
        titleCanvas = titleLogo != null ? titleLogo.GetComponentInParent<Canvas>() : null;
        if (titleCanvas != null && Camera.main != null)
        {
            titleCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            titleCanvas.worldCamera = Camera.main;
            titleCanvas.planeDistance = 50f;
            titleCanvas.overrideSorting = true;
            titleCanvas.sortingOrder = 500;
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
        Vector2 position = logoRunner.anchoredPosition;
        // A compact run cycle for the logo character: lift, lean and squash are
        // kept independent from frame rate and remain active while the player
        // stays fixed beside the logo.
        float stride = Mathf.Sin(runnerTime * 13f);
        position.y = runnerBaseY + Mathf.Abs(stride) * 12f;
        float aimAngle = Mathf.Atan2(impactLocalOffset.y, impactLocalOffset.x) * Mathf.Rad2Deg;
        logoRunner.localRotation = Quaternion.Euler(0f, 0f, aimAngle + stride * -3f);
        logoRunner.localScale = new Vector3(1f + Mathf.Abs(stride) * 0.05f, 1f - Mathf.Abs(stride) * 0.08f, 1f);
        logoRunner.anchoredPosition = position;

        shotTimer -= Time.unscaledDeltaTime;
        if (shotTimer <= 0f)
        {
            shotTimer = normalShotInterval;
            PlayLogoShot();
        }
    }

    private void PlayLogoShot()
    {
        if (logoRunner == null || gameplayBulletPrefab == null || Camera.main == null)
            return;

        Camera camera = Camera.main;
        Camera canvasCamera = titleCanvas != null && titleCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? titleCanvas.worldCamera : null;
        Vector2 runnerScreen = RectTransformUtility.WorldToScreenPoint(canvasCamera, logoRunner.position);
        float canvasScale = titleCanvas != null ? titleCanvas.scaleFactor : 1f;
        float angle = Mathf.Atan2(impactLocalOffset.y, impactLocalOffset.x);
        Vector2 aimDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 aimNormal = new Vector2(-aimDirection.y, aimDirection.x);
        Vector2 muzzleScreen = runnerScreen
            + (aimDirection * muzzleLocalOffset.x + aimNormal * muzzleLocalOffset.y) * canvasScale;
        Vector2 impactScreen = runnerScreen + impactLocalOffset * canvasScale;
        float cameraDistance = Mathf.Abs(camera.transform.position.z);
        Vector3 origin = camera.ScreenToWorldPoint(new Vector3(muzzleScreen.x, muzzleScreen.y, cameraDistance));
        origin.z = 0f;
        Vector3 impactPoint = camera.ScreenToWorldPoint(new Vector3(impactScreen.x, impactScreen.y, cameraDistance));
        impactPoint.z = 0f;
        Vector2 shotDirection = ((Vector2)impactPoint - (Vector2)origin).normalized;

        GameObject bulletObject = Instantiate(gameplayBulletPrefab, origin, Quaternion.identity);
        bulletObject.name = "TitlePlayerBullet";
        SpriteRenderer renderer = bulletObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 7;

        PlayerBullet bullet = bulletObject.GetComponent<PlayerBullet>();
        if (bullet != null)
        {
            GameObject target = new GameObject("TitleBulletImpactCollider", typeof(BoxCollider2D));
            target.transform.position = impactPoint;
            target.layer = 31;
            target.GetComponent<BoxCollider2D>().size = Vector2.one * impactColliderSize;

            float range = Vector2.Distance(origin, impactPoint) + 1f;
            bullet.Init(shotDirection, 0f, 1 << 31, range, 0, null, 0f);
            StartCoroutine(WaitForBulletImpact(bulletObject, target, impactPoint));
        }
        if (gameplayShotSound != null)
            AudioSource.PlayClipAtPoint(gameplayShotSound, origin);
    }

    private IEnumerator WaitForBulletImpact(GameObject bulletObject, GameObject target, Vector3 impactPoint)
    {
        float timeout = 2f;
        while (bulletObject != null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (target != null)
            Destroy(target);
        if (gameplayImpactPrefab == null)
            yield break;

        GameObject impact = Instantiate(gameplayImpactPrefab, impactPoint, Quaternion.identity);
        impact.name = "TitleTerrainPixelImpact";
        impact.transform.localScale *= 2.25f;
        ParticleSystemRenderer[] renderers = impact.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = "Default";
            renderers[i].sortingOrder = ImpactEffectSortingOrder;
            renderers[i].rendererPriority = ImpactEffectSortingOrder;
        }

        ParticleSystem[] particles = impact.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
            particles[i].Play(true);
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
