#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Style - Script for applying UI style elements
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Style : MonoBehaviour
	{
		public enum StyleTypes
		{
			None,
			Window,
			Box,
			HiddenBox,
			Button,
			HiddenButton,
			ToggleButton,
			Toggle,
			HorizontalSlider,
			TextInput,
			KSPToggle,
			VerticalScrollbar,
			KSPWindow,
			AppButton,
			Tooltip,
			VerticalSlider,
			Popup,
		}

		[SerializeField]
		private StyleTypes m_StyleType = StyleTypes.None;

		public StyleTypes StlyeType
		{
			get { return m_StyleType; }
		}

		private void setSelectable(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
		{
			Selectable select = GetComponent<Selectable>();

			if (select == null)
				return;

			select.image.sprite = normal;
			select.image.type = Image.Type.Sliced;
			select.transition = Selectable.Transition.SpriteSwap;

			SpriteState spriteState = select.spriteState;
			spriteState.highlightedSprite = highlight;
			spriteState.pressedSprite = active;
			spriteState.disabledSprite = inactive;
			select.spriteState = spriteState;
		}

		public void setImage(Sprite sprite)
		{
			Image image = GetComponent<Image>();

			if (image == null)
				return;

			image.sprite = sprite;
		}

		public void setButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive)
		{
			setSelectable(normal, highlight, active, inactive);
		}

		public void setToggle(Sprite normal, Sprite highlight, Sprite active, Sprite inactive, Sprite checkmark, Sprite checkmarkHover)
		{
			setSelectable(normal, highlight, active, inactive);

			Toggle toggle = GetComponent<Toggle>();

			if (toggle == null)
				return;

			Image toggleImage = toggle.graphic as Image;

			if (toggleImage == null)
				return;

			toggleImage.sprite = checkmark;
			toggleImage.type = Image.Type.Sliced;

			SCAN_Toggle scan_toggle = GetComponent<SCAN_Toggle>();

			if (scan_toggle != null)
				scan_toggle.HoverCheckmark = checkmarkHover;
		}

		public void setToggleButton(Sprite normal, Sprite highlight, Sprite active, Sprite inactive, Sprite checkmark)
		{
			setSelectable(normal, highlight, active, inactive);

			Toggle toggle = GetComponent<Toggle>();

			if (toggle == null)
				return;

			Image toggleImage = toggle.graphic as Image;

			if (toggleImage == null)
				return;

			toggleImage.sprite = checkmark;
			toggleImage.type = Image.Type.Sliced;
		}

		public void setSlider(Sprite background, Sprite thumb, Sprite thumbHighlight, Sprite thumbActive, Sprite thumbInactive)
		{
			setSelectable(thumb, thumbHighlight, thumbActive, thumbInactive);

			if (background == null)
				return;

			Slider slider = GetComponent<Slider>();

			if (slider == null)
				return;

			Image back = slider.GetComponentInChildren<Image>();

			if (back == null)
				return;

			back.sprite = background;
			back.type = Image.Type.Sliced;
		}

		public void setScrollbar(Sprite background, Sprite thumb)
		{
			Image back = GetComponent<Image>();

			if (back == null)
				return;

			back.sprite = background;

			Scrollbar scroll = GetComponent<Scrollbar>();

			if (scroll == null)
				return;

			if (scroll.targetGraphic == null)
				return;

			Image scrollThumb = scroll.targetGraphic.GetComponent<Image>();

			if (scrollThumb == null)
				return;

			scrollThumb.sprite = thumb;
		}
	}
}
