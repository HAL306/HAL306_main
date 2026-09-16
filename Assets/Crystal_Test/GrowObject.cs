using System.Collections;
using UnityEngine;

public class GrowObject : MonoBehaviour
{
    [Header("飛び出し設定")]
    [SerializeField]
    private float growDistance = 2.0f;

    [SerializeField]
    private float growSpeed = 2.0f;


    [Header("AoE設定")]
    [SerializeField]
    private GameObject aoeObject;
    [SerializeField]
    private GameObject growObject1;
    [SerializeField]
    private GameObject growObject2;
    [SerializeField]
    private float aoeDisplayTime = 2.0f;


    [Header("デバッグ")]
    [SerializeField]
    private bool autoStart = false;


    private MeshRenderer[] meshRenderers;

    private MaterialPropertyBlock propertyBlock;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private bool isGrowing;

    private static readonly int CutEnabledID =
        Shader.PropertyToID("_CutEnabled");


    private void Awake()
    {
        propertyBlock =
            new MaterialPropertyBlock();

        meshRenderers =
            GetComponentsInChildren<MeshRenderer>(true);

        // AoE
        if (aoeObject != null)
        {
            aoeObject.SetActive(false);
        }
        if (growObject1 != null)
        {
            growObject1.SetActive(false);
        }
        if (growObject2 != null)
        {
            growObject2.SetActive(false);
        }
    }


    private void Start()
    {
        if (autoStart)
        {
            StartGrowSequence();
        }
    }


    public void SetCutPlane(
        Vector2 spawnPosition,
        Vector2 groundNormal)
    {
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetVector(
                "_SpawnPosition",
                new Vector4(
                    spawnPosition.x,
                    spawnPosition.y,
                    0.0f,
                    0.0f
                )
            );

            propertyBlock.SetVector(
                "_GroundNormal",
                new Vector4(
                    groundNormal.x,
                    groundNormal.y,
                    0.0f,
                    0.0f
                )
            );

            renderer.SetPropertyBlock(propertyBlock);
        }
    }


    private void SetCutEnabled(bool enabled)
    {
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(
                CutEnabledID,
                enabled ? 1.0f : 0.0f
            );

            renderer.SetPropertyBlock(propertyBlock);
        }
    }


    public void SetAoeDirection(Vector2 direction)
    {
        if (aoeObject == null)
            return;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        aoeObject.transform.rotation =
            Quaternion.Euler(
                0.0f,
                0.0f,
                angle
            );
    }


    public void StartGrowSequence()
    {
        StartCoroutine(GrowSequence());
    }


    private IEnumerator GrowSequence()
    {
        // AoE
        if (aoeObject != null)
        {
            aoeObject.SetActive(true);
            Debug.Log("SetActiveAoEtrue");
        }

        
        yield return new WaitForSeconds(
            aoeDisplayTime
        );

        
        if (aoeObject != null)
        {
            aoeObject.SetActive(false);
            Debug.Log("SetActiveAoEfalse");
        }

        if (growObject1 != null)
        {
            growObject1.SetActive(true);

        }
        if (growObject2 != null) {
            growObject2.SetActive(true);

        }
        StartGrow();
    }


    public void StartGrow()
    {
        Vector3 centerPosition =
            transform.position;

        Vector3 growDirection =
            transform.up;

        startPosition =
            centerPosition -
            growDirection * growDistance;

        targetPosition =
            centerPosition +
            growDirection * growDistance;

        transform.position =
            startPosition;

      
        SetCutEnabled(true);

        isGrowing = true;
    }


    private void Update()
    {
        if (!isGrowing)
            return;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                growSpeed * Time.deltaTime
            );

        if (transform.position ==
            targetPosition)
        {
            isGrowing = false;

            OnGrowFinished();
        }
    }


    private void OnGrowFinished()
    {
        SetCutEnabled(false);
    }
}