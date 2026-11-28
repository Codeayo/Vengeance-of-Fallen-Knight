using UnityEngine;

/// <summary>Sprites and materials used by runtime-built cinematic effects (ghost, dust). Resources/CinematicAssets.</summary>
[CreateAssetMenu(menuName = "Vengeance/Cinematic Assets")]
public class CinematicAssets : ScriptableObject
{
    public Sprite ghostFigure;
    public Sprite softGlow;
    public Sprite mist;
    public Material particleMaterial;

    private static CinematicAssets instance;
    public static CinematicAssets Instance => instance != null ? instance : (instance = Resources.Load<CinematicAssets>("CinematicAssets"));
}
