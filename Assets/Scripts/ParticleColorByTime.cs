using UnityEngine;

/// <summary>
/// �p�[�e�B�N���J�n�F�����Ԍo�߂ŕ�Ԃ��鉉�o�X�N���v�g�B
/// ���V�X�e���Ƃ͓Ɨ����āA�Ώ� `ParticleSystem` �̌����ڂ݂̂𐧌䂷��B
/// </summary>
public class ParticleStartColorLerp : MonoBehaviour
{
    public ParticleSystem ps;

    public Color startColor = Color.white;   // �J�n���̐F
    public Color endColor = Color.red;       // 2�b��̐F

    public float delay = 1f;                 // �F�ω��J�n�܂ł̒x������
    public float duration = 2f;              // �F���ς��܂ł̎���

    private float startTime;

    void Start()
    {
        startTime = Time.time;               // �Q�[���J�n�������L�^
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        // �x�����͊J�n�F���ێ�
        if (elapsed < delay)
        {
            var main = ps.main;
            main.startColor = startColor;
            return;
        }

        // �x����̌o�ߎ���
        float t = Mathf.Clamp01((elapsed - delay) / duration);

        // �F����
        Color current = Color.Lerp(startColor, endColor, t);
        current.a = 1f; // �������h�~

        var mainModule = ps.main;
        mainModule.startColor = current;
    }
}