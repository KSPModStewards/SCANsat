
using UnityEngine;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_UI.UI_Framework
{
	public class SCANEdgeDetect : MonoBehaviour
	{
		private float _sensitivityDepth = 0.8f;
		private float _sensitivityNormals = 0.6f;
		private float _sampleDist = 0.8f;

		private Material _edgeDetectMaterial = null;

		private Texture2D _rampTexture;

		private void Start()
		{
			SetMaterial();
		}

		private void OnDestroy()
		{
			if (_rampTexture != null)
			{
				Destroy(_rampTexture);
				_rampTexture = null;
			}
		}

		private void SetMaterial()
		{
			_edgeDetectMaterial = new Material(SCAN_UI_Loader.EdgeDetectShader);

			_rampTexture = new Texture2D(256, 1, TextureFormat.RGB24, false);

			// ramp texture to render everything in dark shades of Amber,
			// except originally dark lines, which become bright Amber
			for (int i = 0; i < 256; ++i)
			{
				_rampTexture.SetPixel(i, 0, palette.lerp(palette.black, palette.xkcd_Amber, i / 1024f));
			}

			for (int i = 0; i < 10; ++i)
			{
				_rampTexture.SetPixel(i, 0, palette.xkcd_Amber);
			}

			_rampTexture.Apply();

			_edgeDetectMaterial.SetTexture("_RampTex", _rampTexture);
			Vector2 sensitivity = new Vector2(_sensitivityDepth, _sensitivityNormals);
			_edgeDetectMaterial.SetVector("_Sensitivity", new Vector4(sensitivity.x, sensitivity.y, 1.0f, sensitivity.y));
			_edgeDetectMaterial.SetFloat("_SampleDistance", _sampleDist);
		}

		private void OnEnable()
		{
			SetCameraFlag();
		}

		private void SetCameraFlag()
		{
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals;
		}

		//Camera RenderTexture will be applied here
		[ImageEffectOpaque]
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_edgeDetectMaterial == null)
			{
				SetMaterial();
			}

			Graphics.Blit(source, destination, _edgeDetectMaterial);
		}
	}
}
