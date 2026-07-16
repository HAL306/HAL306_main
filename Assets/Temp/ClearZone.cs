using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ClearZone : MonoBehaviour
{
    [SerializeField]
    BOSScharge _boss;

    [SerializeField] private PlayCutsceneEventChannel cutsceneEventCh;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            _boss.gameObject.SetActive(false);


            // rendererを念のためデフォルトにする
            Camera camera = Camera.main;
            if (camera == null) return;

            // カメラのURP追加データを取得
            var cameraData = camera.GetUniversalAdditionalCameraData();

            // 指定したインデックスのRendererへ切り替え
            // フィーバー用のインデックスは1
            cameraData.SetRenderer(0);

            cutsceneEventCh.PlayCutscene("StageClear");
            this.enabled = false;
        }
    }
}
