using UnityEngine;
using System;
using UnityEngine.Events;

namespace SCANsat.Unity.HSVPicker.Events
{
	[Serializable]
	public class ColorChangedEvent : UnityEvent<Color>
	{
		
	}
}
