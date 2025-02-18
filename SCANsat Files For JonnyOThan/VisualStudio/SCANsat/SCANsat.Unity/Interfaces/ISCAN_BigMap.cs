#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_BigMap - Interface for transfer of big map information
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SCANsat.Unity.Interfaces
{
	public interface ISCAN_BigMap
	{
		string Version { get; }

		string CurrentProjection { get; set; }

		string CurrentMapType { get; set; }

		string CurrentResource { get; set; }

		string CurrentCelestialBody { get; set; }

		string RandomWaypoint { get; }

		bool IsVisible { get; set; }

		bool ColorToggle { get; set; }

		bool TerminatorToggle { get; set; }

		bool GridToggle { get; set; }

		bool OrbitToggle { get; set; }

		bool WaypointToggle { get; set; }

		bool AnomalyToggle { get; set; }

		bool FlagToggle { get; set; }

		bool LegendToggle { get; set; }

		bool LegendAvailable { get; }

		bool ResourceToggle { get; set; }

		bool OrbitAvailable { get; }

		bool ShowOrbit { get; }

		bool ShowWaypoint { get; }

		bool ShowResource { get; }

		bool MechJebAvailable { get; }

		bool TooltipsOn { get; }

		bool LegendTooltips { get; }

		bool LockInput { get; set; }

		int OrbitSteps { get; }

		int CurrentScene { get; }

		float Scale { get; }

		Sprite WaypointSprite { get; }

		Canvas MainCanvas { get; }

		Canvas TooltipCanvas { get; }

		Vector2 Position { get; set; }

		Vector2 Size { get; set; }

		Texture2D LegendImage { get; }

		IList<string> Projections { get; }

		IList<string> MapTypes { get; }

		IList<string> Resources { get; }

		IList<string> CelestialBodies { get; }

		IList<string> LegendLabels { get; }

		Dictionary<string, MapLabelInfo> OrbitLabelList { get; }

		Dictionary<Guid, MapLabelInfo> FlagInfoList { get; }

		Dictionary<string, MapLabelInfo> AnomalyInfoList { get; }

		Dictionary<int, MapLabelInfo> WaypointInfoList { get; }

		KeyValuePair<Guid, MapLabelInfo> VesselInfo { get; }

		string MapInfo(Vector2 rectPosition);

		string TooltipText(float xPos);

		void RefreshMap();

		void OpenMainMap();

		void OpenInstruments();

		void OpenZoomMap();

		void OpenSettings();

		void OpenOverlay();

		void ExportMap();

		void Update();

		void SetWaypoint(string id, Vector2 pos);

		void SetMJWaypoint(Vector2 pos);

		void ClickMap(Vector2 pos);

		SimpleLabelInfo OrbitInfo(int index);

		MapLabelInfo OrbitIconInfo(string id);

		Vector2 VesselPosition();
	}
}
