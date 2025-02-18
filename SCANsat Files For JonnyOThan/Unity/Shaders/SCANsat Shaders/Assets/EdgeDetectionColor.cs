using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[RequireComponent (typeof (Camera))]
	[AddComponentMenu ("Image Effects/Edge Detection/Edge Detection Color")]
	public class EdgeDetectionColor : PostEffectsBase
    {
        public RawImage m_Texture;

		public float sensitivityDepth = 1.0f;
		public float sensitivityNormals = 1.0f;
		public float sampleDist = 1.0f;
		public float edgesOnly = 0.0f;
		public Color edgesOnlyBgColor = Color.black;
		public Color edgesColor = Color.red;
		
		public Shader edgeDetectShader;
		public Material edgeDetectMaterial = null;		
		
		public override bool CheckResources ()
		{
			CheckSupport (true);
			
			edgeDetectMaterial = CheckShaderAndCreateMaterial (edgeDetectShader,edgeDetectMaterial);

            Texture2D t = new Texture2D(256, 1, TextureFormat.RGB24, false);

            // ramp texture to render everything in dark shades of Amber,
            // except originally dark lines, which become bright Amber
            for (int i = 0; i < 256; ++i)
                t.SetPixel(i, 0, Color.Lerp(Color.black, Color.yellow, i / 1024f));
            for (int i = 0; i < 10; ++i)
                t.SetPixel(i, 0, Color.yellow);
            t.Apply();
            edgeDetectMaterial.SetTexture("_RampTex", t);

            return isSupported;
		}
		
		void SetCameraFlag ()
		{
				GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
		}
		
		void OnEnable ()
		{
			SetCameraFlag();
		}
		
		[ImageEffectOpaque]
		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{
			if (CheckResources () == false)
			{
				Graphics.Blit (source, destination);
				return;
			}
		    if (edgeDetectMaterial == null)
		    {
                edgeDetectShader = Shader.Find("Hidden/EdgeDetectColors");
		        edgeDetectMaterial = CheckShaderAndCreateMaterial(edgeDetectShader, edgeDetectMaterial);
                Texture2D t = new Texture2D(256, 1, TextureFormat.RGB24, false);

                // ramp texture to render everything in dark shades of Amber,
                // except originally dark lines, which become bright Amber
                for (int i = 0; i < 256; ++i)
                    t.SetPixel(i, 0, Color.Lerp(Color.black, Color.yellow, i / 1024f));
                for (int i = 0; i < 10; ++i)
                    t.SetPixel(i, 0, Color.yellow);
                t.Apply();
                edgeDetectMaterial.SetTexture("_RampTex", t);
            }
			Vector2 sensitivity = new Vector2 (sensitivityDepth, sensitivityNormals);
			edgeDetectMaterial.SetVector ("_Sensitivity", new Vector4 (sensitivity.x, sensitivity.y, 1.0f, sensitivity.y));
			//edgeDetectMaterial.SetFloat ("_BgFade", edgesOnly);
			edgeDetectMaterial.SetFloat ("_SampleDistance", sampleDist);
            //edgeDetectMaterial.SetVector("_BgColor", edgesOnlyBgColor);
            //edgeDetectMaterial.SetVector("_Color", edgesColor);
			
			Graphics.Blit (source, destination, edgeDetectMaterial);

            m_Texture.texture = destination;

        }
	}
}
