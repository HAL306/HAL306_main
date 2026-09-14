using UnityEngine;

public class Crystal_MaskObject : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void SetCutY(float cutY)
    {
        meshRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat("_CutY", cutY);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }
}
