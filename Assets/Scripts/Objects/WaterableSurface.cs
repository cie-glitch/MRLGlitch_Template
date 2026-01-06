using UnityEngine;
using System.Collections.Generic;

public class WaterableSurface : MonoBehaviour
{
    [Header("Paint Settings")]
    [SerializeField] private Texture2D paintTexture;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 512;
    [SerializeField] private float brushSize = 10f;
    [SerializeField] private Color paintColor = Color.blue;
    [SerializeField] private string materialPropertyName = "_MainTex";

    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    private MeshCollider meshCollider;
    private Renderer meshRenderer;
    private Material materialInstance;

    void Start()
    {
        // Get components
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<Renderer>();

       
            paintTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            // Initialize with transparent or white
            Color[] fillPixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < fillPixels.Length; i++)
            {
                fillPixels[i] = Color.black;
            }
            paintTexture.SetPixels(fillPixels);
            paintTexture.Apply();


        // Create material instance and assign texture
        if (meshRenderer != null)
        {
            materialInstance = meshRenderer.material;
            materialInstance.SetTexture(materialPropertyName, paintTexture);
        }
    }

    void OnParticleCollision(GameObject other)
    {

        ParticleSystem ps = other.GetComponent<ParticleSystem>();
        if (ps == null || meshCollider == null)
            return;

        int numCollisionEvents = ps.GetCollisionEvents(gameObject, collisionEvents);

        // Process each collision event

        //Pick a rnaomd PèarticleCollisionEvent to paint
        int randomIndex = Random.Range(0, numCollisionEvents);

        ParticleCollisionEvent collisionEvent = collisionEvents[randomIndex];

        // Get the collision point in world space
        Vector3 worldPoint = collisionEvent.intersection;

        // Raycast to get UV coordinates
        RaycastHit hit;
        Vector3 rayDirection = (worldPoint - collisionEvent.intersection + collisionEvent.normal * 0.01f).normalized;

        if (Physics.Raycast(worldPoint - collisionEvent.normal * 0.1f, collisionEvent.normal, out hit, 0.2f))
        {
            if (hit.collider == meshCollider)
            {
                // Get UV2 coordinates
                Vector2 uv = hit.textureCoord2;

                // Convert UV to texture pixel coordinates
                int x = Mathf.FloorToInt(uv.x * paintTexture.width);
                int y = Mathf.FloorToInt(uv.y * paintTexture.height);

                // Paint a circle at this position
                PaintCircle(x, y, brushSize, paintColor);
            }
        } else
        {
            Debug.Log("Raycast did not hit the mesh collider.");
        }
    }

    void PaintCircle(int centerX, int centerY, float radius, Color color)
    {
        int radiusInt = Mathf.CeilToInt(radius);

        Debug.Log($"Painting circle at ({centerX}, {centerY}) with radius {radius}");

        for (int x = -radiusInt; x <= radiusInt; x++)
        {
            for (int y = -radiusInt; y <= radiusInt; y++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);

                if (distance <= radius)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    // Check bounds
                    if (pixelX >= 0 && pixelX < paintTexture.width && pixelY >= 0 && pixelY < paintTexture.height)
                    {
                        // Optional: Add soft edge with alpha blend
                        float alpha = 1f - (distance / radius);
                        Color currentColor = paintTexture.GetPixel(pixelX, pixelY);
                        Color blendedColor = Color.Lerp(currentColor, color, alpha * color.a);

                        paintTexture.SetPixel(pixelX, pixelY, blendedColor);
                    }
                }
            }
        }

        // Apply changes to texture
        paintTexture.Apply();
    }

    Texture2D CloneTexture(Texture2D source)
    {
        Texture2D clone = new Texture2D(source.width, source.height, source.format, false);
        clone.SetPixels(source.GetPixels());
        clone.Apply();
        return clone;
    }
}
