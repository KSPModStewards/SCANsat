
using UnityEngine;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_UI.UI_Framework
{
	public class SCANEdgeDetect : MonoBehaviour
	{
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

			if (_edgeDetectMaterial != null)
			{
				Destroy(_edgeDetectMaterial);
				_edgeDetectMaterial = null;
			}
		}

		private void SetMaterial()
		{
			_edgeDetectMaterial = new Material(SCAN_UI_Loader.EdgeDetectShader);

			// OnRenderImage calls back in here whenever the material has gone, so this can run more
			// than once on the same component: the ramp of the previous material has to go with it.
			if (_rampTexture != null)
			{
				Destroy(_rampTexture);
			}

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

			// _Sensitivity and _SampleDistance are commented out in EdgeDetectColors, which hardcodes
			// 0.75 for both: setting them did nothing. _RampTex is the shader's only input.
			_edgeDetectMaterial.SetTexture("_RampTex", _rampTexture);
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
