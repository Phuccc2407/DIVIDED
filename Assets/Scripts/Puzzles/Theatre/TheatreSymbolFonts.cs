using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class TheatreSymbolFonts : MonoBehaviour
{
    public Font[] sourceFonts = System.Array.Empty<Font>();
    public TheatreRoom[] rooms = System.Array.Empty<TheatreRoom>();
    private readonly List<TMP_FontAsset> generated = new List<TMP_FontAsset>();

    private void Awake()
    {
        foreach (var source in sourceFonts)
        {
            if (source == null) continue;
            var font = TMP_FontAsset.CreateFontAsset(source);
            if (font != null) generated.Add(font);
        }
        if (generated.Count == 0) return;
        generated[0].fallbackFontAssetTable = generated.GetRange(1, generated.Count - 1);
        const string glyphs = "?\u2600\u263D\u2605\u2604\u263F\u2640\u2641\u2642\u2643\u2644\u2645\u2646\u2647\u2660\u2663\u2665\u2666\u269C\u262F\u2625\u2693\u26A1\u2694\u2696\u2699\u231B\u221E\u2295\u2297\u25C8\u25B3\u25BD\u25A1\u25CE\u16A0\u16B1\u16A8\u16C9\u16DF\u16D7";
        foreach (var font in generated) font.TryAddCharacters(glyphs, out string missing);
        if (rooms.Length == 0) ApplyFonts(GetComponentsInChildren<TMP_Text>(true));
        else foreach (var room in rooms)
            if (room != null) ApplyFonts(room.GetComponentsInChildren<TMP_Text>(true));
    }

    private void ApplyFonts(TMP_Text[] texts)
    {
        foreach (var text in texts)
        {
            var room = text.GetComponentInParent<TheatreRoom>();
            if (room != null && room.statusDisplay == text) continue;
            text.font = generated[0];
            text.fontSharedMaterial = generated[0].material;
            text.ForceMeshUpdate();
        }
    }

    private void OnDestroy()
    {
        foreach (var font in generated)
        {
            if (font == null) continue;
            if (font.material != null) Destroy(font.material);
            foreach (var texture in font.atlasTextures) if (texture != null) Destroy(texture);
            Destroy(font);
        }
    }
}
