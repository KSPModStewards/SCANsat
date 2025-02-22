#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANmechStarter - A simple monobehaviour to check if everything is loaded correctly before launching the MechJeb watcher
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using UnityEngine;

namespace SCANmechjeb
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	class SCANmechStarter : MonoBehaviour
	{
		private MonoBehaviour SCANmechjebInt;

		private void Start()
		{
			if (SCANmechjebMainMenu.Loaded)
			{
				print("[SCANsatMechJeb] Starting SCANsat - MechJeb Interface...");
				SCANmechjebInt = gameObject.AddComponent<SCANmechjeb>();
			}
			else
			{
				Destroy(gameObject);
			}
		}
	}
}
