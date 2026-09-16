using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the animated gameplay panorama used by NewTitleScene.
/// The panorama is deliberately created at runtime so the supplied 4K layers
/// remain easy to replace without editing the scene hierarchy.
/// </summary>
public sealed class TitleGameplayBackdrop : MonoBehaviour
{
    private Camera titleCamera;
    private ScrollingLayer sky;
    private ScrollingLayer farTerrain;
    private ScrollingLayer nearTerrain;
    private ScrollingLayer foreground;
    private GameObject rocketPrefab;
    private AudioClip launchSound;
    private float rocketTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (SceneManager.GetActiveScene().name != "NewTitleScene" || FindFirstObjectByType<TitleGameplayBackdrop>() != null)
            return;

        new GameObject("TitleGameplayBackdrop").AddComponent<TitleGameplayBackdrop>();
    }

    private void Awake()
    {
        titleCamera = Camera.main;
        if (titleCamera == null)
        {
            enabled = false;
            return;
        }

        // Replace the old two-plane mock-up with the same layered view used by gameplay.
        SetLegacyLayerActive("Sky", false);
        SetLegacyLayerActive("FarSilhouette", false);

        sky = CreateLayer("Sky", "TitlePanorama/IMG_3362", -30, 0.18f, 0f);
        farTerrain = CreateLayer("FarTerrain", "TitlePanorama/IMG_3364", -20, 0.32f, 0f);
        nearTerrain = CreateLayer("NearTerrain", "TitlePanorama/IMG_3363", -10, 0.56f, 0f);

        // AirShip is sorting order 3.  Order 5 therefore puts crystals/moon in
        // front of the balloon while the overlay canvas keeps all lettering above it.
        foreground = CreateLayer("CrystalMoonForeground", "TitlePanorama/IMG_3365", 5, 0.56f, 0f);
        rocketPrefab = Resources.Load<GameObject>("TitlePanorama/PlayerRocket");
        launchSound = Resources.Load<AudioClip>("TitlePanorama/RocketLaunch");
        rocketTimer = Random.Range(1.2f, 2.6f);
    }

    private void Update()
    {
        if (titleCamera == null)
            return;

        float delta = Time.unscaledDeltaTime;
        sky.Tick(delta, titleCamera);
        farTerrain.Tick(delta, titleCamera);
        nearTerrain.Tick(delta, titleCamera);
        foreground.Tick(delta, titleCamera); // Exactly the same flow rate as its matching background.

        rocketTimer -= delta;
        if (rocketTimer <= 0f)
        {
            rocketTimer = Random.Range(1.8f, 4.4f);
            StartCoroutine(FireRocket());
        }
    }

    private ScrollingLayer CreateLayer(string objectName, string resourcePath, int order, float speed, float yOffset)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Debug.LogWarning($"Title panorama sprite was not found: {resourcePath}");
            return new ScrollingLayer();
        }

        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform, false);
        return new ScrollingLayer(root.transform, sprite, order, speed, yOffset, titleCamera);
    }

    private IEnumerator FireRocket()
    {
        if (rocketPrefab == null)
            yield break;

        float halfHeight = titleCamera.orthographicSize;
        float halfWidth = halfHeight * titleCamera.aspect;
        Vector3 start = new Vector3(-halfWidth - 0.8f, Random.Range(-halfHeight * 0.15f, halfHeight * 0.42f), 0f);
        Vector3 end = new Vector3(Random.Range(-halfWidth * 0.2f, halfWidth * 0.82f), Random.Range(-halfHeight * 0.42f, -halfHeight * 0.08f), 0f);

        Vector2 direction = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        GameObject rocketObject = Instantiate(rocketPrefab, start, Quaternion.identity);
        rocketObject.name = "TitlePlayerRocket";
        SpriteRenderer renderer = rocketObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = 7;

        PlayerRocket rocket = rocketObject.GetComponent<PlayerRocket>();
        rocket.Init(direction, 0f, 0, distance + 1f, 0, null, null, 0f);
        if (launchSound != null)
            AudioSource.PlayClipAtPoint(launchSound, start);

        float flightTime = distance / 30f;
        yield return new WaitForSeconds(flightTime);
        if (rocket != null)
            rocket.DetonateVisualOnly();
    }

    private static void SetLegacyLayerActive(string objectName, bool active)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null) target.SetActive(active);
    }

    private sealed class ScrollingLayer
    {
        private readonly Transform root;
        private readonly Transform[] tiles;
        private readonly float speed;
        private readonly float yOffset;
        private float tileWidth;

        public ScrollingLayer() { tiles = new Transform[0]; }

        public ScrollingLayer(Transform root, Sprite sprite, int order, float speed, float yOffset, Camera camera)
        {
            this.root = root;
            this.speed = speed;
            this.yOffset = yOffset;
            tiles = new Transform[2];
            for (int i = 0; i < tiles.Length; i++)
            {
                GameObject tile = new GameObject($"Tile{i}", typeof(SpriteRenderer));
                tile.transform.SetParent(root, false);
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = order;
                tiles[i] = tile.transform;
            }
            Fit(camera);
        }

        public void Tick(float delta, Camera camera)
        {
            if (root == null || tiles.Length == 0) return;
            Fit(camera);
            root.position += Vector3.left * speed * delta;
            if (root.position.x <= -tileWidth)
                root.position += Vector3.right * tileWidth;
        }

        private void Fit(Camera camera)
        {
            float worldHeight = camera.orthographicSize * 2f;
            Sprite sprite = tiles[0].GetComponent<SpriteRenderer>().sprite;
            float scale = worldHeight / sprite.bounds.size.y;
            tileWidth = sprite.bounds.size.x * scale;
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].localScale = Vector3.one * scale;
                tiles[i].localPosition = new Vector3(tileWidth * i, yOffset, 0f);
            }
        }
    }
}
