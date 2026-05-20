using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// テスト用の単純移動スクリプト。
/// Spaceキー押下中に対象オブジェクトをY方向へ移動させる。
/// </summary>
public class fpsmovie : MonoBehaviour
{
    public float speed = 5f; // 上昇速度



    void Update()
    {


        // スペースキーが押されている間
        if (Input.GetKey(KeyCode.Space))
        {
            // 現在位置を取得
            Vector3 pos = transform.position;

            // Y座標を加算
            pos.y += speed * Time.deltaTime;

            // 新しい位置を反映
            transform.position = pos;
        }
    }
}

