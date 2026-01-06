using UnityEngine;
using UnityEngine.Video;

public class TunnelParticleVideo : MonoBehaviour
{
    [Header("Mesh Settings")]
    [SerializeField] private MeshFilter planeMeshFilter;
    
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool loopVideo = true;
    
    [Header("Additional Texture")]
    [SerializeField] private Texture2D additionalTexture;
    [SerializeField] private float additionalYMultiplier = 1.0f;
    [SerializeField] private float additionalYOffset = 0.0f;
    
    [Header("Particle System")]
    [SerializeField] private ParticleSystem particleSystem;
    
    [Header("Spawn Settings")]
    [SerializeField] private int particlesPerFrame = 5;
    [SerializeField] private float yAxisMultiplier = 1.0f;
    [SerializeField] private float yAxisOffset = -0.5f; // Offset so alpha 0.5 = no displacement
    
    [Header("Particle Properties")]
    [SerializeField] private bool useTextureColor = true;
    [SerializeField] private Vector3 initialVelocity = Vector3.zero;
    [SerializeField] private float velocityRandomness = 0.1f;
    
    private Mesh planeMesh;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;
    private ParticleSystem.EmitParams emitParams;
    private RenderTexture renderTexture;
    private Texture2D readableTexture;

    void Start()
    {
        InitializeMesh();
        InitializeParticleSystem();
        InitializeVideoPlayer();
    }

    void OnDestroy()
    {
        CleanupTextures();
    }

    void Update()
    {
        UpdateReadableTexture();
        SpawnParticlesThisFrame();
    }

    private void InitializeMesh()
    {
        if (planeMeshFilter == null)
        {
            planeMeshFilter = GetComponent<MeshFilter>();
        }

        if (planeMeshFilter == null || planeMeshFilter.mesh == null)
        {
            Debug.LogError("No MeshFilter or Mesh found!");
            enabled = false;
            return;
        }

        planeMesh = planeMeshFilter.mesh;
        vertices = planeMesh.vertices;
        uvs = planeMesh.uv;
        triangles = planeMesh.triangles;

        if (uvs.Length == 0)
        {
            Debug.LogError("Mesh has no UV coordinates!");
            enabled = false;
        }
    }

    private void InitializeParticleSystem()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }

        if (particleSystem == null)
        {
            Debug.LogError("No ParticleSystem found! Please attach a ParticleSystem component.");
            enabled = false;
            return;
        }

        // Configure particle system to not emit automatically
        var emission = particleSystem.emission;
        emission.enabled = false;

        // Initialize emit params
        emitParams = new ParticleSystem.EmitParams();
    }

    private void InitializeVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }

        if (videoClip == null)
        {
            Debug.LogError("No video clip assigned!");
            enabled = false;
            return;
        }

        // Configure video player
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.playOnAwake = false;
        
        // Create render texture matching video dimensions
        renderTexture = new RenderTexture((int)videoClip.width, (int)videoClip.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        videoPlayer.targetTexture = renderTexture;

        // Create readable texture for sampling
        readableTexture = new Texture2D((int)videoClip.width, (int)videoClip.height, TextureFormat.RGBA32, false);

        // Start playing the video
        videoPlayer.Play();
    }

    private void CleanupTextures()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (readableTexture != null)
        {
            Destroy(readableTexture);
        }
    }

    private void UpdateReadableTexture()
    {
        if (videoPlayer == null || !videoPlayer.isPlaying || renderTexture == null)
            return;

        // Copy render texture to readable texture
        RenderTexture.active = renderTexture;
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = null;
    }

    private void SpawnParticlesThisFrame()
    {
        for (int i = 0; i < particlesPerFrame; i++)
        {
            EmitSingleParticle();
        }
    }

    private void EmitSingleParticle()
    {
        // Select a random triangle
        int triangleIndex = Random.Range(0, triangles.Length / 3) * 3;
        
        // Get the three vertices of the triangle
        int idx0 = triangles[triangleIndex];
        int idx1 = triangles[triangleIndex + 1];
        int idx2 = triangles[triangleIndex + 2];

        Vector3 v0 = vertices[idx0];
        Vector3 v1 = vertices[idx1];
        Vector3 v2 = vertices[idx2];

        Vector2 uv0 = uvs[idx0];
        Vector2 uv1 = uvs[idx1];
        Vector2 uv2 = uvs[idx2];

        // Generate random barycentric coordinates
        float r1 = Random.value;
        float r2 = Random.value;
        
        if (r1 + r2 > 1)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }
        float r3 = 1 - r1 - r2;

        // Interpolate position and UV
        Vector3 localPosition = r1 * v0 + r2 * v1 + r3 * v2;
        Vector2 uv = r1 * uv0 + r2 * uv1 + r3 * uv2;

        // Sample texture color at UV coordinate
        Color textureColor = SampleTexture(uv);
        
        // Sample additional texture using normalized UV
        Color additionalColor = SampleAdditionalTexture(uv);

        // Use alpha channel to offset along local Y axis
        float yOffset = (textureColor.a + yAxisOffset) * yAxisMultiplier;
        
        // Add additional Y offset from R channel of additional texture
        yOffset += (additionalColor.r + additionalYOffset) * additionalYMultiplier;
        
        localPosition += Vector3.up * yOffset;

        // Convert to world position
        Vector3 worldPosition = transform.TransformPoint(localPosition);

        // Set up particle emission parameters
        emitParams.position = worldPosition;
        
        if (useTextureColor)
        {
            // Use R channel from additional texture as alpha
            emitParams.startColor = new Color(textureColor.r, textureColor.g, textureColor.b, additionalColor.r);
        }

        // Optional: Add velocity with randomness
        Vector3 velocity = initialVelocity;
        if (velocityRandomness > 0)
        {
            velocity += new Vector3(
                Random.Range(-velocityRandomness, velocityRandomness),
                Random.Range(-velocityRandomness, velocityRandomness),
                Random.Range(-velocityRandomness, velocityRandomness)
            );
        }
        emitParams.velocity = transform.TransformDirection(velocity);

        // Emit the particle
        particleSystem.Emit(emitParams, 1);
    }

    private Color SampleTexture(Vector2 uv)
    {
        if (readableTexture == null)
            return Color.white;

        // Clamp UV coordinates to [0,1]
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        // Sample RGB from top half (0.5-1.0 in texture space)
        int x = Mathf.FloorToInt(uv.x * (readableTexture.width - 1));
        int yRGB = Mathf.FloorToInt((uv.y * 0.5f + 0.5f) * (readableTexture.height - 1));
        Color rgbColor = readableTexture.GetPixel(x, yRGB);

        // Sample alpha from bottom half (0.0-0.5 in texture space)
        int yAlpha = Mathf.FloorToInt(uv.y * 0.5f * (readableTexture.height - 1));
        Color alphaColor = readableTexture.GetPixel(x, yAlpha);

        // Return RGB with alpha from bottom half (using grayscale value)
        return new Color(rgbColor.r, rgbColor.g, rgbColor.b, alphaColor.grayscale);
    }
    
    private Color SampleAdditionalTexture(Vector2 uv)
    {
        if (additionalTexture == null)
            return Color.white;

        // Clamp UV coordinates to [0,1]
        uv.x = Mathf.Clamp01(uv.x);
        uv.y = Mathf.Clamp01(uv.y);

        // Convert UV to pixel coordinates
        int x = Mathf.FloorToInt(uv.x * (additionalTexture.width - 1));
        int y = Mathf.FloorToInt(uv.y * (additionalTexture.height - 1));

        return additionalTexture.GetPixel(x, y);
    }
}