using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowKeyboard : MonoBehaviour
{
    private TouchScreenKeyboard keyboard;

    public void EnableKeyboard()
    {
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
    }

}
