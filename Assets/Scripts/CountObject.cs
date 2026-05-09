using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 特定タグオブジェクト数を監視し、条件達成時にUIを切り替える簡易監視スクリプト。
/// 主に演出・デバッグ用途で、ゲーム進行の補助判定に使う。
/// </summary>
public class CountObject : MonoBehaviour
{
    public GameObject[] objects;
    [SerializeField] private GameObject image;
    // public TextMeshProUGUI text;
    int count;

    void Update()
    {
        objects = GameObject.FindGameObjectsWithTag("Button");
        count = objects.Length;
        Debug.Log(count);
        if (count == 0)
        {
            //  text.text = "Success!!";
                image.SetActive(false);
            
        }
    }
}