using UnityEngine;

/// <summary>
/// �Ə�UI�𐧌䂷��R���|�[�l���g
/// �R���g���[���[���쎞�̓X�e�B�b�N�����~��苗���ɏƏ����ړ�������
/// �}�E�X���쎞�̓}�E�X�̃��[���h���W�ɏƏ����ړ�������
/// </summary>
public class ShootTargetUI : MonoBehaviour
{  
    [SerializeField, Tooltip("�Ə��̃T�C�Y")]
    [Range(0.1f, 5.0f)]
    private float _reticleSize = 1.0f;

    [SerializeField, Tooltip("�R���g���[���[���쎞�̏Ə�����")]
    [Range(0.0f, 20.0f)]
    private float _controllerAimDist = 5.0f;

    [SerializeField] private PlayerShooter _playerShooter;

    private void Update()
    {
        UpdateReticlePosition();
    }
    private void Start()
    {
        // �V�[������PlayerShooter�������Ŏ擾����
        transform.localScale = Vector3.one * _reticleSize;
    }

    // �Ə��̈ʒu���X�V����
    private void UpdateReticlePosition()
    {
        if (!_playerShooter)
        {
            return;
        }
        
        if (_playerShooter.IsMouseAim)
        {
            // �}�E�X����F�}�E�X�̃��[���h���W�ɏƏ����ړ�
            transform.position = _playerShooter.MouseWorldPos;
        }
        else
        {
            // �R���g���[���[����F�v���C���[���W����X�e�B�b�N�����~�����ɏƏ����ړ�
            Vector2 playerPos = _playerShooter.transform.position;
            Vector2 aimDir = _playerShooter.ShootAimTarget.normalized;
            transform.position = playerPos + aimDir * _controllerAimDist;
        }
    }
}