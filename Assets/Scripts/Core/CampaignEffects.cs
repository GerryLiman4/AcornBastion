using UnityEngine;

// Small native sprite effects keep the jam spells independent of new art packages.
public static class CampaignEffects
{
    private static Sprite square;
    private static Sprite ring;

    public static Sprite Square
    {
        get
        {
            if (square == null) square = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            return square;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ring != null) return ring;
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            texture.name = "CampaignRing";
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    texture.SetPixel(x, y, distance >= 26 && distance <= 31 ? Color.white : Color.clear);
                }
            texture.Apply();
            ring = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
            return ring;
        }
    }

    public static SpriteRenderer Sprite(string name, Transform parent, Vector3 position, Vector2 size, Color color, bool circle = false)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1);
        var sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sprite = circle ? Ring : Square;
        sprite.sortingLayerName = "Default";
        sprite.sortingOrder = 100;
        sprite.color = color;
        return sprite;
    }
}
