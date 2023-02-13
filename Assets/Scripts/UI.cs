//Sanchay Ravindiran 2020

/*
    This class handles the user interface of the
    game server. The interface is simple and resembles
    a colorful console, and this class is used to
    display game server logs on the console to keep
    the user informed on what is going on within the game.
*/

using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI Text;
    [SerializeField] private TextMeshProUGUI LineNumber;
    private float Lines;

    public void Display(string text, bool important)
    {
        if (important)
        {
            Text.text += "<color=#FFFFFF>" + text + "</color>\n";
        }
        else
        {
            Text.text += text + "\n";
        }

        Lines++;
        LineNumber.text = "logs: " + Lines;
    }

    private void Update()
    {
        Vector2 tempVector = transform.localPosition;
        tempVector.y += Input.GetAxis("Vertical") * 1500 * Time.deltaTime;
        transform.localPosition = tempVector;
    }
}
