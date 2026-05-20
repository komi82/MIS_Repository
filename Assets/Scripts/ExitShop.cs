using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitShop : MonoBehaviour
{
    public void Exitshop()
    {
        FadeManager.Instance.LoadSceneWithFade("free");
    }
}
