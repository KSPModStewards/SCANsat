using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EdgeDetectTex : MonoBehaviour
{
    public RawImage m_Texture;

    public Material m_EdgeDetectMaterial;

    public Camera m_Cam;

    private RenderTexture rt;
    private RenderTexture destrt;
    private Texture2D tex;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTex();
    }

    private void UpdateTex()
    {
        if (rt == null)
        {
            rt = new RenderTexture(500, 700, 32, RenderTextureFormat.Default);
            rt.Create();

            destrt = new RenderTexture(500, 700, 32, RenderTextureFormat.Default);
            destrt.Create();

            m_Cam.targetTexture = rt;

            tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        }

        RenderTexture old = RenderTexture.active;
        RenderTexture.active = rt;
        float shadows = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance = 0;
        m_Cam.Render();
        QualitySettings.shadowDistance = shadows;
        Graphics.Blit(rt, null, m_EdgeDetectMaterial);

        //m_EdgeDetectMaterial.SetTexture("_MainTex", rt);

        //tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        //tex.Apply();

        RenderTexture.active = old;

        m_Texture.texture = rt;

    }
}
