using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeMapInitializer : MonoBehaviour
{
    public Button ButtonPrefab;
    public Transform MapGameObject;

    void Start()
    {
        FillMap();
    }

    void FillMap()
    {
        RectTransform mapRect = MapGameObject.GetComponent<RectTransform>();
        Dictionary<MapNode, Button> nodeToButtonMap = new Dictionary<MapNode, Button>();
        Dictionary<EncounterType, Sprite> sprites = FileOperationsManager.Instance.LoadMapSprites();
        int maxRows = NodeMapGenerator.Instance.FloorWidth;
        int maxCols = NodeMapGenerator.Instance.NumFloors + 2; // space for boss
        float usableWidth = mapRect.rect.width * 0.9f;
        float usableHeight = mapRect.rect.height * 0.8f;
        float marginX = mapRect.rect.width * 0.1f;
        float marginY = mapRect.rect.height * 0.15f;


        // Generate buttons
        foreach (var floor in NodeMapGenerator.Instance.Map)
        {
            foreach (var node in floor)
            {
                // Calculate button position
                float xPosition = marginX + (node.X / (float)maxCols) * usableWidth - (mapRect.rect.width / 2);
                float yPosition = marginY + (node.Y / (float)maxRows) * usableHeight - (mapRect.rect.height / 2);

                // Instantiate button
                Button button = Instantiate(ButtonPrefab, MapGameObject);
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchoredPosition = new Vector2(xPosition, yPosition);

                nodeToButtonMap[node] = button;
                NodeButton nodeButton = button.GetComponent<NodeButton>();
                if (nodeButton != null)
                {
                    nodeButton.Initialize(node, EncounterHelper.GetSpriteFromType(node.EncounterType, sprites));
                }
            }
        }

        // Generate texture for connections
        Texture2D texture = GenerateTexture();
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = texture;
        material.mainTextureScale = new Vector2(1f, 1f);

        // Generate connections
        foreach (var node in nodeToButtonMap.Keys)
        {
            if (node.PreviousNodes == null) continue;

            foreach (var previousNode in node.PreviousNodes)
            {
                if (!nodeToButtonMap.ContainsKey(previousNode)) continue;

                // Get positions of the two buttons
                RectTransform startButtonRect = nodeToButtonMap[previousNode].GetComponent<RectTransform>();
                RectTransform endButtonRect = nodeToButtonMap[node].GetComponent<RectTransform>();

                Vector3 startCenter = startButtonRect.anchoredPosition;
                Vector3 endCenter = endButtonRect.anchoredPosition;

                // Calculate direction and offset
                Vector3 direction = (endCenter - startCenter).normalized;
                float startRadius = Mathf.Min(startButtonRect.rect.width, startButtonRect.rect.height) / 2.2f;
                float endRadius = Mathf.Min(endButtonRect.rect.width, endButtonRect.rect.height) / 2.2f;

                Vector3 adjustedStart = startCenter + direction * startRadius;
                Vector3 adjustedEnd = endCenter - direction * endRadius;

                Vector3 worldStart = MapGameObject.GetComponent<RectTransform>().TransformPoint(adjustedStart);
                Vector3 worldEnd = MapGameObject.GetComponent<RectTransform>().TransformPoint(adjustedEnd);

                // Create a new line
                GameObject lineObject = new GameObject("ConnectionLine");
                lineObject.transform.SetParent(MapGameObject, true);

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new Vector3[] { worldStart, worldEnd });
                lineRenderer.startWidth = 0.9f;
                lineRenderer.endWidth = 0.9f;

                // Apply dotted material
                lineRenderer.material = material;
                lineRenderer.startColor = Color.black;
                lineRenderer.endColor = Color.black;
            }
        }
    }

    Texture2D GenerateTexture()
    {
        // Something like [ # # # # ] 
        Texture2D texture = new Texture2D(23, 23);
        int w = texture.width;
        int h = texture.height;
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                if (i <= 0 || j <= 0 || i >= w || j >= w)
                {
                    texture.SetPixel(i, j, Color.clear);
                }
                else if (i % 4 == 1 || i % 4 == 2)
                {
                    texture.SetPixel(i, j, Color.clear);
                }
                else
                {
                    texture.SetPixel(j, i, Color.white);
                }
            }
        }
        texture.Apply();
        return texture;
    }
}