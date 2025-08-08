using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PsdLayerData
{
    public string id;
    public GameObject layerPrefab;
    public Sprite sprite;
    public Vector3 position;
    public int sortingOrder;
}

[System.Serializable]
public class StickerComposite
{
    public PsdLayerData baseSticker; // STK_* Base
    public List<PsdLayerData> nestedZones = new List<PsdLayerData>(); // Place_Zone children
    public List<PsdLayerData> nestedOverlays = new List<PsdLayerData>(); // Overlay children
}

[System.Serializable]
public class PsdStickerData
{
    public string id;
    public GameObject layerPrefab; // the PSD-imported GameObject
    public Sprite baseSprite;
    public Vector3 position;
    public int sortingOrder;
    public List<PsdLayerData> nestedZoneLayers = new List<PsdLayerData>();
    public List<PsdLayerData> nestedOverlayLayers = new List<PsdLayerData>();
}

[CreateAssetMenu(menuName = "MyStickerRoom/PsdMetadata", fileName = "PsdMetadata")]
public class PsdMetadata : ScriptableObject
{
    public string psdName;
    public GameObject psdPrefab;
    public List<PsdLayerData> backgroundLayers = new List<PsdLayerData>();
    public List<PsdLayerData> zoneLayers = new List<PsdLayerData>(); // global placeholders
    public List<PsdLayerData> overlayLayers = new List<PsdLayerData>(); // global overlays
    
    // per-sticker data including nested placeholders and overlays
    public List<PsdStickerData> stickerDatas = new List<PsdStickerData>();
    
    [HideInInspector] public List<PsdLayerData> stickerLayers = new List<PsdLayerData>(); // base sprites only (deprecated)
}