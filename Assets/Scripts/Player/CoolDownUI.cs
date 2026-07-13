using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CoolDownUI : MonoBehaviour
{

    [SerializeField] private PlayerRocketShooter shooter;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform cursorImageTransform;

    private void Start()
    {
        

    }
    private void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPos);

        cursorImageTransform.localPosition = localPos;

        cooldownImage.fillAmount = 1-(shooter.CooldownTimer / shooter.ShootInterval);
    }

}
