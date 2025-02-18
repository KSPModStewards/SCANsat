#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANsat - Raster Prop Monitor persistent storage
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using SCANsat.SCAN_UI;
using SCANsat.SCAN_Platform.Extensions.ConfigNodes;

namespace SCANsat.SCAN_PartModules
{
	public class SCANRPMStorage : PartModule
	{
		internal List<RPMPersistence> RPMList = new List<RPMPersistence>();

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("SCANsatRPM"))
			{
				ConfigNode RPMPersistence = node.GetNode("SCANsatRPM");
				foreach (ConfigNode RPMNode in RPMPersistence.GetNodes("Prop"))
				{
					string id = RPMNode.parse("Prop ID", "");

					if (string.IsNullOrEmpty(id))
						continue;

					int m = RPMNode.parse("Mode", (int)0);
					int z = RPMNode.parse("Zoom", (int)0);
					int r = RPMNode.parse("Resource", (int)0);
					bool c = RPMNode.parse("Color", true);
					bool lines = RPMNode.parse("Lines", true);
					bool anom = RPMNode.parse("Anomalies", true);
					bool resource = RPMNode.parse("DrawResource", true);


					RPMList.Add(new RPMPersistence(id, m, c, z, lines, anom, resource, r));
				}
			}
		}

		public override void OnSave(ConfigNode node)
		{
			if (RPMList.Count > 0)
			{
				ConfigNode RPMPersistence = new ConfigNode("SCANsatRPM");
				foreach (RPMPersistence RPMMFD in RPMList)
				{
					ConfigNode RPMProp = new ConfigNode("Prop");
					RPMProp.AddValue("Prop ID", RPMMFD.RPMID);
					RPMProp.AddValue("Mode", RPMMFD.RPMMode);
					RPMProp.AddValue("Zoom", RPMMFD.RPMZoom);
					RPMProp.AddValue("Resource", RPMMFD.RPMResource);
					RPMProp.AddValue("Color", RPMMFD.RPMColor);
					RPMProp.AddValue("Lines", RPMMFD.RPMLines);
					RPMProp.AddValue("Anomalies", RPMMFD.RPMAnomaly);
					RPMProp.AddValue("DrawResource", RPMMFD.RPMDrawResource);
					RPMPersistence.AddNode(RPMProp);
				}
				node.AddNode(RPMPersistence);
			}
		}

	}
}
