using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Settings_Old : MonoBehaviour
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private GameObject m_BodyInfoPrefab = null;
		[SerializeField]
		private Transform m_BodyInfoTransform = null;
		[SerializeField]
		private TextHandler m_ScanActiveToggle = null;
		[SerializeField]
		private Slider m_TimeWarpResolution = null;
		[SerializeField]
		private TextHandler m_SensorInfo = null;
		[SerializeField]
		private Toggle m_GroundTrackToggle = null;
		[SerializeField]
		private Toggle m_GroundTrackActiveToggle = null;
		[SerializeField]
		private Toggle m_OverlayTooltipToggle = null;
		[SerializeField]
		private Toggle m_WindowTooltipToggle = null;
		[SerializeField]
		private Toggle m_StockToolbarToggle = null;
		[SerializeField]
		private TextHandler m_UIScale = null;
		[SerializeField]
		private Slider m_UIScaleSlider = null;
		[SerializeField]
		private Toggle m_BiomeLockToggle = null;
		[SerializeField]
		private Toggle m_NarrowBandToggle = null;
		[SerializeField]
		private Toggle m_InstantScanToggle = null;
		[SerializeField]
		private Toggle m_DisableStockToggle = null;
		[SerializeField]
		private Toggle m_StockThresholdToggle = null;
		[SerializeField]
		private InputField m_ThresholdInput = null;
		[SerializeField]
		private Button m_ThresholdSet = null;
		[SerializeField]
		private TextHandler m_MapInterpolation = null;
		[SerializeField]
		private TextHandler m_MapHeight = null;
		[SerializeField]
		private TextHandler m_CoverageTransparency = null;
		[SerializeField]
		private TextHandler m_BiomeMapHeight = null;
		[SerializeField]
		private Toggle m_GreyScaleToggle = null;
		[SerializeField]
		private Toggle m_CSVExportToggle = null;
		[SerializeField]
		private TextHandler m_MapWidth = null;
		[SerializeField]
		private InputField m_MapWidthInput = null;
		[SerializeField]
		private Transform m_ResetSCANResource = null;
		[SerializeField]
		private Transform m_ResetStockResource = null;
		[SerializeField]
		private Transform m_MapFill = null;
	}
}
