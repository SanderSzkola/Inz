using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class NodeButton : MonoBehaviour
{
    public MapNode Node;
    private Button Button;
    private static Color ColorBrown = new Color(0.15f, 0.1f, 0f);
    private static Color ColorRed = new Color(1f, 0.4f, 0.4f);

    private void Awake()
    {
        Button = GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => TryMoveHere());
    }

    public void Initialize(MapNode node, Sprite sprite)
    {
        Node = node;
        node.NodeButton = this;
        Button.image.sprite = sprite;
        RefreshColor();
    }

    public void SetSprite(Dictionary<EncounterType, Sprite> dictionary)
    {
        Button.image.sprite = EncounterHelper.GetSpriteFromType(Node.EncounterType, dictionary);
    }

    public void RefreshColor()
    {
        if (Node != null && Node.IsPlayerHere)
        {
            Button.image.color = ColorRed;
        }
        else
        {
            Button.image.color = ColorBrown;
        }
    }

    void TryMoveHere()
    {
        bool canMove = Node.X == 0 && NodeMapGenerator.Instance.CurrentFloor == -1;

        if (!canMove && Node.PreviousNodes != null)
        {
            foreach (MapNode prevNode in Node.PreviousNodes)
            {
                if (prevNode.IsPlayerHere)
                {
                    canMove = true;
                    prevNode.IsPlayerHere = false;
                    prevNode.NodeButton.RefreshColor();
                    break;
                }
            }
        }

        if (canMove)
        {
            Node.IsPlayerHere = true;
            NodeMapGenerator.Instance.CurrentFloor = Node.X;
            RefreshColor();
            MoveHere();
            SceneManager.LoadScene("BattleScene");
        }

    }

    // TODO: some graphic pointer or circle or whatever, to show the movement
    public IEnumerator MoveHere()
    {
        yield return null;
    }
}