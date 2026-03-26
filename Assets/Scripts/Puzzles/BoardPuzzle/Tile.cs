using UnityEngine;
using System.Collections;

public class Tile : MonoBehaviour
{
    public enum TileShape
    {
        Square,
        Circle,
        Triangle,
        Star,
        Diamond,
        Hexagon
    }

    public enum TileColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange
    }

    [SerializeField] GameObject mesh;
    [SerializeField] Renderer rend;

    public TileShape shape;
    public TileColor color;

    Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;
    }

    public void Setup(TileShape s, TileColor c)
    {
        shape = s;
        color = c;

        rend.material.color = GetColor(c);
    }

    Color GetColor(TileColor c)
    {
        switch (c)
        {
            case TileColor.Red: return Color.red;
            case TileColor.Blue: return Color.blue;
            case TileColor.Green: return Color.green;
            case TileColor.Yellow: return Color.yellow;
            case TileColor.Purple: return new Color(0.6f, 0f, 0.8f);
            case TileColor.Orange: return new Color(1f, 0.5f, 0f);
        }

        return Color.white;
    }

    public void Show()
    {
        mesh.SetActive(true);
        transform.position = startPos;

        rend.material.color = GetColor(color);
    }

    public void Hide()
    {
        mesh.SetActive(false);
    }

    public void Warning()
    {
        StartCoroutine(WarningRoutine());
    }

    IEnumerator WarningRoutine()
    {
        float t = 0f;

        while (t < 0.5f)
        {
            transform.position = startPos + Random.insideUnitSphere * 0.05f;
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;

        rend.material.color = Color.red;

        yield return new WaitForSeconds(1f);

        rend.material.color = GetColor(color);
    }
}