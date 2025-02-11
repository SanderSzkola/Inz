using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;


public class NodeButton : MonoBehaviour
{
    public MapNode Node;
    private Button Button;
    private static Color ColorBrown = new Color(0.15f, 0.1f, 0f);
    private static Color ColorRed = new Color(0.8f, 0.3f, 0.3f);
    private static Color ColorOrange = new Color(1f, 0.5f, 0f);
    private static DialogWindowManager DialogWindowManager;
    private static GameObject positionIndicatorPrefab;
    private GameObject playerIndicator;
    private List<MapNode> nodesToRefresh;

    private void Awake()
    {
        Button = GetComponent<Button>();
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() => TryMoveHere());
        if (positionIndicatorPrefab == null)
        {
            positionIndicatorPrefab = Resources.Load<GameObject>("PositionIndicator");
        }
    }

    public void Initialize(MapNode node, Sprite sprite)
    {
        Node = node;
        node.NodeButton = this;
        Button.image.sprite = sprite;
        RefreshColor();
        SetDialogWindow();
    }

    private void SetDialogWindow()
    {
        if (DialogWindowManager == null)
        {
            GameObject dialogCanvas = GameObject.Find("DialogCanvas");
            Transform dialogPanelTransform = dialogCanvas.transform.Find("DialogPanel");
            DialogWindowManager = dialogPanelTransform.GetComponent<DialogWindowManager>();
        }
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
            ShowPlayerIndicator();
        }
        else
        {
            Button.image.color = CanMoveHere() ? ColorOrange : ColorBrown;
            HidePlayerIndicator();
        }
    }

    public bool CanMoveHere()
    {
        bool canMove = Node.X == 0 && NodeMapGenerator.Instance.CurrentNode == null;
        if (Node?.PreviousNodes.Count != 0)
        {
            foreach (MapNode prevNode in Node.PreviousNodes)
            {
                if (prevNode.IsPlayerHere)
                {
                    canMove = true;
                    break;
                }
            }
        }
        return canMove;
    }

    void TryMoveHere()
    {
        if (CanMoveHere())
        {
            MapNode currentNode = NodeMapGenerator.Instance.CurrentNode;
            nodesToRefresh = new List<MapNode>();
            if (currentNode != null)
            {
                currentNode.IsPlayerHere = false;
                nodesToRefresh.Add(currentNode);
                nodesToRefresh.AddRange(currentNode.NextNodes);
            }
            Node.IsPlayerHere = true;
            NodeMapGenerator.Instance.CurrentNode = Node;
            nodesToRefresh.AddRange(Node.NextNodes);
            if (currentNode != null)
            {
                StartCoroutine(MoveHere(currentNode.NodeButton, MoveHerePostAnim));
            }
            else
            {
                MoveHerePostAnim();
            }
        }
    }

    private void MoveHerePostAnim()
    {
        foreach (MapNode node in nodesToRefresh)
        {
            node.NodeButton.RefreshColor();
        }

        UnitData[] unitData = FileOperationsManager.Instance.LoadSaveData().playerUnits;
        switch (Node.EncounterType)
        {
            case EncounterType.SKILL:
                foreach (UnitData unit in unitData)
                {
                    unit.SkillPoints += 1;
                }
                DialogWindowManager.SetMessage("The area feels serene and inspiring. Your team hones their skills. Everyone gains 1 skill point.\nGame saved.");
                DialogWindowManager.ShowWindow();
                FileOperationsManager.Instance.SaveGame();
                break;

            case EncounterType.REST:
                List<string> leveledUpUnits = new List<string>();

                foreach (UnitData unit in unitData)
                {
                    int originalExp = unit.Exp;
                    unit.ProcessRest();
                    if (unit.Exp != originalExp)
                    {
                        leveledUpUnits.Add(unit.Name);
                    }
                }

                string restMessage = "The team finds a quiet spot to rest. Everyone regains HP and MP.";
                if (leveledUpUnits.Count > 0)
                {
                    restMessage += "\nThe following units leveled up:\n" + string.Join(", ", leveledUpUnits) + ".";
                }
                restMessage += "\nGame saved.";
                DialogWindowManager.SetMessage(restMessage);
                DialogWindowManager.ShowWindow();
                FileOperationsManager.Instance.SaveGame();
                break;

            default:
                SceneManager.LoadScene("BattleScene");
                break;
        }
    }

    private void ShowPlayerIndicator()
    {
        if (positionIndicatorPrefab == null) return;

        if (playerIndicator == null)
        {
            playerIndicator = Instantiate(positionIndicatorPrefab, transform);
            playerIndicator.transform.localPosition = new Vector3(0, 6, 0);
            playerIndicator.GetComponent<Image>().color = Color.green;
            playerIndicator.transform.SetAsFirstSibling();
        }

        playerIndicator.SetActive(true);
    }

    private void HidePlayerIndicator()
    {
        if (playerIndicator != null)
        {
            playerIndicator.SetActive(false);
        }
    }

    public IEnumerator MoveHere(NodeButton nodeButton, Action onComplete = null)
    {
        GameObject playerIndicator = nodeButton.playerIndicator;

        Vector3 startPos = playerIndicator.transform.position;
        Vector3 targetPos = Button.transform.position + new Vector3(0, 6, 0);

        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            playerIndicator.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        playerIndicator.transform.position = targetPos;
        onComplete?.Invoke();
    }

}