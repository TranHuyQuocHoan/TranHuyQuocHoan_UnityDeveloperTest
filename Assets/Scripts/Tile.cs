using UnityEngine;

public enum ColorType
{
    Wall,
    Path,
    NPC,
    Goal,
    FoundPath
}

public class Tile : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void ChooseColor(ColorType colorType)
    {
        switch (colorType)
        {
            case ColorType.Wall:
                sr.color = Color.gray;
                break;

            case ColorType.Path:
                sr.color = Color.white;
                break;

            case ColorType.NPC:
                sr.color = Color.green;
                break;

            case ColorType.Goal:
                sr.color = Color.red;
                break;

            case ColorType.FoundPath:
                sr.color = Color.yellow;
                break;
        }
    }
}
