using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class NodeButton : MonoBehaviour
{
    public MapNode Node;
    private Button Button;
    private bool playing = false;

    private void Awake()
    {
        Button = GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => ShowPosition());
        Button.image.color = new Color(0.5f, 0.3f, 0f);
    }

    public void SetSprite(Sprite sprite)
    {
        Button.image.sprite = sprite;
    }

    void ShowPosition()
    {
        Debug.Log($"{Node.X}, {Node.Y}");
        foreach (MapNode node in Node.NextNodes)
        {
            if (node != null && node.NodeButton != null)
            {
                node.NodeButton.StartCoroutine(node.NodeButton.FlashRed());
            }
        }
    }

    public IEnumerator FlashRed()
    {
        if (playing) yield break;
        playing = true;
        Color originalColor = Button.image.color;
        Button.image.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        Button.image.color = originalColor;
        playing = false;
        foreach (MapNode node in Node.NextNodes)
        {
            node.NodeButton.StartCoroutine(node.NodeButton.FlashRed());
        }
    }
}