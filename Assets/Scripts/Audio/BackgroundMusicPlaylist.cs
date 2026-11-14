using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DIVIDED/Background Music Playlist", fileName = "BackgroundMusicPlaylist")]
public sealed class BackgroundMusicPlaylist : ScriptableObject
{
    [Serializable]
    public struct LevelMusic
    {
        [Min(0)] public int levelIndex;
        public AudioClip clip;
    }

    public AudioClip lobbyMusic;
    public AudioClip gameMusic;
    public List<LevelMusic> levelMusic = new List<LevelMusic>();

    public bool TryGetLevelClip(int index, out AudioClip clip)
    {
        foreach (var entry in levelMusic)
            if (entry.levelIndex == index)
            {
                clip = entry.clip;
                return true;
            }
        clip = null;
        return false;
    }
}
