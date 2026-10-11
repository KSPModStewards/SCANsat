using System.Text;
using SCANsat.Unity;
using SCANsat.Unity.Unity;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Editor tooling for the general settings page: the map generation budget and scanline speed rows.
// Both are four-stop sliders whose value is an index into a table in SCAN_SettingsGeneral, so the
// prefab only carries the stops as tick labels.
public static class SCANsatSettingsRows
{
	const string generalPath = "Assets/Prefabs/SCAN_General.prefab";

	const float trackHalfWidth = 170f;   // how far the slider track reaches either side of the row centre
	const float labelY = -34f;

	static readonly string[] budgetTicks = new string[] { "2", "4", "8", "16" };
	static readonly string[] scanlineTicks = new string[] { "0.5x", "1x", "2x", "Instant" };

	[MenuItem("SCANsat/Author Settings Rows")]
	public static void AuthorRows()
	{
		GameObject root = PrefabUtility.LoadPrefabContents(generalPath);

		if (root == null)
		{
			Debug.LogError("[rows] could not load " + generalPath);
			return;
		}

		try
		{
			SCAN_SettingsGeneral page = root.GetComponent<SCAN_SettingsGeneral>();
			Transform section = root.transform.Find("General_Section");
			Transform budgetRow = section == null ? null : section.Find("Map_Speed");

			if (page == null || budgetRow == null)
			{
				Debug.LogError("[rows] the page or the map speed row is missing; nothing changed");
				return;
			}

			// The scanline row is the budget row duplicated, so it inherits the styling, the layout
			// element and the tooltip handler; only the texts, the stops and the wiring differ.
			GameObject scanlineRow = Object.Instantiate(budgetRow.gameObject, section);
			scanlineRow.transform.SetSiblingIndex(budgetRow.GetSiblingIndex() + 1);

			Text budgetTitle = MakeRow(budgetRow.gameObject, "Map_Speed", "Map_Budget", "Map Generation Budget: 4 ms/frame", budgetTicks, 1);
			Text scanlineTitle = MakeRow(scanlineRow, "Map_Speed", "Scanline_Speed", "Scanline Speed: 1x (1.0 s)", scanlineTicks, 1);

			Slider budgetSlider = budgetRow.Find("Slider").GetComponent<Slider>();
			Slider scanlineSlider = scanlineRow.transform.Find("Slider").GetComponent<Slider>();

			Rewire(budgetSlider, page.MapBudgetSlider);
			Rewire(scanlineSlider, page.ScanlineSlider);

			SetTooltip(scanlineRow.GetComponent<TooltipHandler>(), "settingsHelpScanlineSpeed");

			SerializedObject so = new SerializedObject(page);
			Assign(so, "m_MapBudget", budgetTitle.GetComponent<TextHandler>());
			Assign(so, "m_MapBudgetSlider", budgetSlider);
			Assign(so, "m_Scanline", scanlineTitle.GetComponent<TextHandler>());
			Assign(so, "m_ScanlineSlider", scanlineSlider);
			so.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, generalPath);
			Debug.Log("[rows] saved " + generalPath);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}

		AssetDatabase.SaveAssets();
		DumpGeneral();
	}

	// One four-stop row: rename it after what it now sets, retitle it, and space its tick labels along
	// the track. The prefab ships three ticks, so the fourth is cloned from the third.
	static Text MakeRow(GameObject row, string oldName, string name, string title, string[] ticks, int value)
	{
		row.name = name;

		Transform titleObject = row.transform.Find(oldName);
		titleObject.name = name;

		Text titleText = titleObject.GetComponent<Text>();
		titleText.text = title;

		Transform third = row.transform.Find("Slider_Label3");

		if (row.transform.Find("Slider_Label4") == null)
		{
			GameObject fourth = Object.Instantiate(third.gameObject, row.transform);
			fourth.name = "Slider_Label4";
			fourth.transform.SetSiblingIndex(third.GetSiblingIndex() + 1);
		}

		for (int i = 0; i < ticks.Length; i++)
		{
			RectTransform label = (RectTransform)row.transform.Find("Slider_Label" + (i + 1));
			float x = -trackHalfWidth + (2 * trackHalfWidth * i) / (ticks.Length - 1);

			label.anchoredPosition = new Vector2(x, labelY);
			label.GetComponent<Text>().text = "|" + (char)10 + ticks[i];

			// Only the widest stop needs more room than the stock tick box; a wider box still keeps
			// the tick itself centred on the handle position.
			if (ticks[i].Length > 3)
			{
				label.sizeDelta = new Vector2(56, label.sizeDelta.y);
			}
		}

		Slider slider = row.transform.Find("Slider").GetComponent<Slider>();
		slider.minValue = 0;
		slider.maxValue = ticks.Length - 1;
		slider.wholeNumbers = true;
		slider.value = value;

		return titleText;
	}

	static void Rewire(Slider slider, UnityEngine.Events.UnityAction<float> call)
	{
		while (slider.onValueChanged.GetPersistentEventCount() > 0)
		{
			UnityEventTools.RemovePersistentListener(slider.onValueChanged, 0);
		}

		UnityEventTools.AddPersistentListener(slider.onValueChanged, call);
	}

	static void SetTooltip(TooltipHandler handler, string name)
	{
		if (handler == null)
		{
			Debug.LogError("[rows] no tooltip handler on the new row");
			return;
		}

		SerializedObject so = new SerializedObject(handler);
		so.FindProperty("m_TooltipName").stringValue = name;
		so.FindProperty("m_HelpTip").boolValue = true;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void Assign(SerializedObject so, string field, Object value)
	{
		SerializedProperty property = so.FindProperty(field);

		if (property == null)
		{
			Debug.LogError("[rows] the page has no field " + field + "; is Assets/Plugins/SCANsat.Unity.dll current?");
			return;
		}

		property.objectReferenceValue = value;
	}

	// What actually ships: load the built bundle from GameData and dump the page out of it, so the rows
	// are verified in the artifact the game reads rather than in the project asset they came from.
	[MenuItem("SCANsat/Verify Shipped Bundle")]
	public static void VerifyShippedBundle()
	{
		string path = "C:/git/ksp/SCANsat/GameData/SCANsat/Resources/scan_prefabs.scan";
		AssetBundle bundle = AssetBundle.LoadFromFile(path);

		if (bundle == null)
		{
			Debug.LogError("[rows] could not load the bundle at " + path);
			return;
		}

		GameObject page = bundle.LoadAsset<GameObject>("SCAN_General");

		if (page == null)
		{
			Debug.LogError("[rows] the bundle has no SCAN_General");
			bundle.Unload(true);
			return;
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("[rows] dump of the shipped bundle");
		Walk(page.transform, 0, sb);
		Debug.Log(sb.ToString());

		bundle.Unload(true);
	}

	[MenuItem("SCANsat/Dump General Settings Page")]
	public static void DumpGeneral()
	{
		GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(generalPath);

		if (root == null)
		{
			Debug.LogError("[rows] could not load " + generalPath);
			return;
		}

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("[rows] dump of " + generalPath);
		Walk(root.transform, 0, sb);
		Debug.Log(sb.ToString());
	}

	// A serialized reference as its path in the page, so two objects both called Slider can be told apart.
	static string Describe(SerializedProperty p)
	{
		if (p == null)
		{
			return "MISSING";
		}

		Component c = p.objectReferenceValue as Component;

		if (c == null)
		{
			return p.objectReferenceValue == null ? "null" : p.objectReferenceValue.name;
		}

		string path = c.name + ":" + c.GetType().Name;

		for (Transform t = c.transform.parent; t != null; t = t.parent)
		{
			path = t.name + "/" + path;
		}

		return path;
	}

	static void Walk(Transform t, int depth, StringBuilder sb)
	{
		string pad = new string(' ', depth * 2);
		sb.Append(pad).Append(t.name);

		RectTransform rt = t as RectTransform;

		if (rt != null)
		{
			sb.Append("  [pos=").Append(rt.anchoredPosition).Append(" size=").Append(rt.sizeDelta).Append("]");
		}

		foreach (Component c in t.GetComponents<Component>())
		{
			if (c == null || c is RectTransform)
			{
				continue;
			}

			sb.Append("  {").Append(c.GetType().Name);

			Text text = c as Text;
			if (text != null)
			{
				sb.Append(" text=").Append(text.text.Replace(((char)10).ToString(), " / ")).Append(" size=").Append(text.fontSize);
			}

			Slider slider = c as Slider;
			if (slider != null)
			{
				sb.Append(" min=").Append(slider.minValue).Append(" max=").Append(slider.maxValue)
					.Append(" whole=").Append(slider.wholeNumbers).Append(" value=").Append(slider.value)
					.Append(" listeners=").Append(slider.onValueChanged.GetPersistentEventCount());

				for (int i = 0; i < slider.onValueChanged.GetPersistentEventCount(); i++)
				{
					sb.Append(" -> ").Append(slider.onValueChanged.GetPersistentMethodName(i));
				}
			}

			if (c is TooltipHandler)
			{
				SerializedObject so = new SerializedObject(c);
				sb.Append(" tooltip=").Append(so.FindProperty("m_TooltipName").stringValue)
					.Append(" help=").Append(so.FindProperty("m_HelpTip").boolValue);
			}

			if (c is SCAN_SettingsGeneral)
			{
				SerializedObject so = new SerializedObject(c);
				string[] fields = new string[] { "m_MapBudget", "m_MapBudgetSlider", "m_Scanline", "m_ScanlineSlider", "m_UIScaleSlider" };

				foreach (string f in fields)
				{
					sb.Append(" ").Append(f).Append("=").Append(Describe(so.FindProperty(f)));
				}
			}

			sb.Append("}");
		}

		sb.AppendLine();

		for (int i = 0; i < t.childCount; i++)
		{
			Walk(t.GetChild(i), depth + 1, sb);
		}
	}
}
