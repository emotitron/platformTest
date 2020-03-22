//Copyright 2018, Davin Carten, All rights reserved


using UnityEngine;
using emotitron.Networking;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities
{


#if UNITY_EDITOR


	public class NetCoreHeaderEditor : HeaderEditorBase
	{

		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/NetCoreText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GrayBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; } // "Header/TeapotBW"; }
		}

		protected override string GridTexturePath
		{
			get { return "Header/HashRLoop"; }
		}
	}

	public class SystemHeaderEditor : HeaderEditorBase
	{
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/SystemText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GrayBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; }
		}
	}

	public class AccessoryHeaderEditor : HeaderEditorBase
	{
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/AccessoryText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GrayBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; }
		}
	}

	public class NetUtilityHeaderEditor : HeaderEditorBase
	{
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/NetUtilityText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GrayBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; }
		}
	}

	public class ReactorHeaderEditor : HeaderEditorBase
	{
		protected override string HelpURL { get { return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.r3vm0h2z0d5c"; } }

		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/ReactorText"; }
		}

		protected override string GridTexturePath
		{
			get { return "Header/ArrowLLoop"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/BlueBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; /* "Header/TeapotBW"*/; }
		}
	}

	public class AutomationHeaderEditor : HeaderEditorBase
	{
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/AutomationText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GreenBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; }
		}
	}

	public class TriggerHeaderEditor : HeaderEditorBase
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.twa1bl7i2gff";
			}
		}
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/TriggerText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/OrangeBack"; }
		}

		protected override string GridTexturePath
		{
			get { return "Header/ArrowRLoop"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; /* "Header/TeapotBW"*/; }
		}
	}

	public class SampleCodeHeaderEditor : HeaderEditorBase
	{
		protected override bool UseThinHeader { get { return true; } }

		protected override string TextTexturePath
		{
			get { return "Header/SampleCodeText"; }
		}

		protected override string BackTexturePath
		{
			get { return "Header/GrayBack"; }
		}

		protected override string TPotTexturePath
		{
			get { return null; /* "Header/TeapotBW"*/; }
		}
	}

	//public class VitalsReactorHeaderEditor : HeaderEditorBase
	//{
	//	protected override string TextTexturePath
	//	{
	//		get { return "Header/VitalsReactorText"; }
	//	}

	//	protected override string BackTexturePath
	//	{
	//		get { return "Header/GrayBack"; }
	//	}

	//	protected override string TPotTexturePath
	//	{
	//		get { return "Header/TeapotVitalsBW"; }
	//	}
	//}


	/// <summary>
	/// All of this just draws the pretty header graphic on components. Nothing to see here.
	/// </summary>
	[CustomEditor(typeof(Component))]
	[CanEditMultipleObjects]
	public class HeaderEditorBase : Editor
	{
		protected Texture2D textTexture;
		protected Texture2D gridTexture;
		protected Texture2D backTexture;
		protected Texture2D teapotTexture;

		protected static Texture2D blackDot, leftCap, leftCapThin;

		protected virtual string TextTexturePath { get { return "Header/EMOTITRON"; } }
		protected virtual string TextFallbackPath { get { return "Header/EMOTITRON"; } }
		protected virtual string GridTexturePath { get { return "Header/GridLoop"; } }
		protected virtual string TPotTexturePath { get { return "Header/Teapot"; } }
		protected virtual string TPotFallbackPath { get { return "Header/Teapot"; } }
		protected virtual string BackTexturePath { get { return "Header/GrayBack"; } }
		protected virtual string BackFallbackPath { get { return "Header/GrayBack"; } }
		protected string LeftCapPath = "Header/LeftCap";
		protected string LeftCapThinPath = "Header/LeftCap_thin";
		protected string BlackDotPath = "Header/BlackDot";


		protected bool showInstructions;
		protected static GUIContent showInstrGC = new GUIContent("Instructions");
		protected static GUIStyle richBox;
		protected static GUIStyle richLabel;
		protected static GUIStyle labelright;

		protected static GUIContent reusableGC = new GUIContent();
		/// <summary>
		/// Override this property. Any non-null value will show up as an instructions foldout.
		/// </summary>
		protected virtual string Instructions { get { return null; } }

		protected virtual string Overview { get { return null; } }

		protected virtual bool UseThinHeader { get { return false; } }

		/// <summary>
		/// Override this property. Any non-null value will turn the header graphic into a clicckable link to this url.
		/// </summary>
		protected virtual string HelpURL { get { return null; } }

		public virtual void OnEnable()
		{
			bool usethin = UseThinHeader || !SimpleSyncSettings.Single.showGUIHeaders;
			GetTextures(usethin);
		}

		private bool usedThin;
		private bool texturesInitialized;

		public virtual void GetTextures(bool usethin)
		{
			if (texturesInitialized && usethin == usedThin)
				return;

			texturesInitialized = true;
			usedThin = usethin;

			if (blackDot == null)
				blackDot = (Texture2D)Resources.Load<Texture2D>(BlackDotPath);

			if (leftCap == null)
				leftCap = (Texture2D)Resources.Load<Texture2D>(LeftCapPath);

			if (leftCapThin == null)
				leftCapThin = (Texture2D)Resources.Load<Texture2D>(LeftCapThinPath);

			string thintext = ((usethin) ? "_thin" : "");

			if (textTexture == null)
				textTexture = (Texture2D)Resources.Load<Texture2D>(TextTexturePath + thintext);
			if (textTexture == null)
				textTexture = (Texture2D)Resources.Load<Texture2D>(TextFallbackPath + thintext);

			if (gridTexture == null)
				gridTexture = (Texture2D)Resources.Load<Texture2D>(GridTexturePath);

			if (teapotTexture == null)
				teapotTexture = (Texture2D)Resources.Load<Texture2D>(TPotTexturePath + thintext);
			if (teapotTexture == null)
				teapotTexture = (Texture2D)Resources.Load<Texture2D>(TPotFallbackPath + thintext);

			if (backTexture == null)
				backTexture = (Texture2D)Resources.Load<Texture2D>(BackTexturePath + thintext);
			if (backTexture == null)
				backTexture = (Texture2D)Resources.Load<Texture2D>(BackFallbackPath + thintext);

		}

		protected virtual void EnsureStylesExist()
		{
			if (richBox == null)
				richBox = new GUIStyle("HelpBox")
				{
					richText = true,
					wordWrap = true,
					padding = new RectOffset(6, 6, 6, 6),
					stretchWidth = true,
					alignment = TextAnchor.UpperLeft,
					fontSize = 10
				};

			if (richLabel == null)
				richLabel = new GUIStyle() { richText = true };

			if (labelright == null)
				labelright = new GUIStyle("Label") { alignment = TextAnchor.UpperRight };
		}

		public void OnUndoRedo()
		{
			Repaint();
		}

		public override void OnInspectorGUI()
		{
			bool usethin = UseThinHeader || !SimpleSyncSettings.Single.showGUIHeaders;

			GetTextures(usethin);

			EnsureStylesExist();

			/// Draw headers
			if (usethin)
			{
				OverlayInstructions(ref showInstructions, Instructions, HelpURL, usethin);
				OverlayHeader(HelpURL, backTexture, gridTexture, teapotTexture, textTexture, usethin);
			}
			else
			{
				OverlayHeader(HelpURL, backTexture, gridTexture, teapotTexture, textTexture, usethin);
				OverlayInstructions(ref showInstructions, Instructions, HelpURL, usethin);
			}


			if (showInstructions)
				DrawInstructions(Instructions);

			OverlayOverview(Overview);

			OnInspectorGUIInjectMiddle();

			DrawSerializedObjectFields(serializedObject, false);

			OnInspectorGUIFooter();
		}

		protected virtual void OnInspectorGUIInjectMiddle()
		{
			//base.OnInspectorGUI();

		}
		protected virtual void OnInspectorGUIFooter()
		{

		}

		public static void OverlayHeader(string HelpURL,
			Texture2D backTexture, Texture2D gridTexture, Texture2D teapotTexture, Texture2D textTexture, bool thin = false)
		{
			int h = thin ? 16 : 32;
			Rect r = EditorGUILayout.GetControlRect(true, thin ? h : (h + 2));

			float left = r.xMin;

			if (!thin)
				r.yMin += 2;

			r.xMin = thin ? r.xMin + EditorGUIUtility.labelWidth /*+ 2*/ : 32;

			string url = HelpURL;
			if (url != null)
			{
				EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
				if (GUI.Button(r, GUIContent.none, GUIStyle.none))
					Application.OpenURL(url);
			}

			//GUI.DrawTexture(r, blackDot);

			var hr = new Rect(r) /*{ xMin = r.xMin + 8 }*/;

			if (backTexture != null)
				GUI.DrawTexture(hr, backTexture);


			/// Draw left endcap
			if (thin)
				GUI.DrawTexture(new Rect(hr) { xMin = hr.xMin - 2, width = 2 }, leftCapThin);
			else
				GUI.DrawTexture(new Rect(r) { xMin = r.xMin - 16, width = 16 }, leftCap);

			/// Draw repeating pattern
			if (gridTexture != null)
				GUI.DrawTexture(hr, gridTexture, ScaleMode.ScaleAndCrop);

			/// Draw Teapot layer
			if (teapotTexture != null)
				GUI.DrawTexture(new Rect(hr.xMax - 248, hr.yMin, 248, h), teapotTexture);

			/// Draw Text
			if (textTexture != null)
				GUI.DrawTexture(new Rect(hr) { x = thin ? hr.x + 4 : (hr.x - 8), width = 248 }, textTexture);

		}

		private GUIStyle instructionsStyle;
		private GUIContent nolabel = new GUIContent(" ");
		private GUIContent scirptlabel = new GUIContent("Script");

		public void OverlayInstructions(ref bool showInstructions, string instructions, string url, bool thin = false)
		{
			Rect r = thin ? EditorGUILayout.GetControlRect(false, 0) : EditorGUILayout.GetControlRect(true, 18);
			r.height = 18;

			if (instructions != null)
			{

				EditorGUI.BeginDisabledGroup(true);
				EditorGUI.LabelField(new Rect(r) { xMin = r.xMin + 14, yMin = r.yMin + 2 }, showInstrGC);
				EditorGUI.EndDisabledGroup();
				showInstructions = EditorGUI.Toggle(new Rect(r) { yMin = r.yMin + 2, width = EditorGUIUtility.labelWidth }, GUIContent.none, showInstructions, (GUIStyle)"Foldout");

				DrawScriptField(serializedObject, new Rect(r) { yMin = r.yMin + 2 }, nolabel);

			}
			else
			{
				DrawScriptField(serializedObject, new Rect(r) { yMin = r.yMin + 2 }, scirptlabel);
			}


			/// Draw Docs Link Ico
			if (/*thin && */HelpURL != null)
			{
				//var helpIcoRect = new Rect(r) { xMin = r.xMin - 8 - 16 - 2, width = 16 };
				EditorUtils.DrawDocsIcon(r.xMin + EditorGUIUtility.labelWidth - 16 - 4, r.yMin + 2, url);
			}
		}

		protected void DrawInstructions(string instructions)
		{
			if (instructionsStyle == null)
			{
				instructionsStyle = new GUIStyle("HelpBox");
				instructionsStyle.richText = true;
				instructionsStyle.padding = new RectOffset(6, 6, 6, 6);
			}
			EditorGUILayout.LabelField(instructions, instructionsStyle);
		}

		protected static GUIStyle overviewStyle;

		public void OverlayOverview(string text)
		{
			if (text == null)
				return;

			if (overviewStyle == null)
			{
				overviewStyle = new GUIStyle("HelpBox"); //GUI.skin.GetStyle("HelpBox");
				overviewStyle.richText = true;
				overviewStyle.padding = new RectOffset(6, 6, 6, 6);
			}

			EditorGUILayout.LabelField(text, overviewStyle);
		}

		public static void DrawScriptField(SerializedObject so)
		{
			SerializedProperty sp = so.GetIterator();
			sp.Next(true);

			sp.NextVisible(false);
			EditorGUI.BeginDisabledGroup(true);
			Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.GetPropertyHeight(sp));
			EditorGUI.PropertyField(r, sp);
			EditorGUI.EndDisabledGroup();
		}

		public static void DrawScriptField(SerializedObject so, Rect r, GUIContent label)
		{
			SerializedProperty sp = so.GetIterator();
			sp.Next(true);

			sp.NextVisible(false);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUI.PropertyField(r, sp, label);
			EditorGUI.EndDisabledGroup();
		}

		public static void DrawSerializedObjectFields(SerializedObject so, bool includeScriptField)
		{
			SerializedProperty sp = so.GetIterator();
			sp.Next(true);

			// Skip drawing the script reference?
			if (!includeScriptField)
				sp.NextVisible(false);

			EditorGUI.BeginChangeCheck();

			int skipNextX = 0;
			int wrapNextX = 0;

			while (sp.NextVisible(false))
			{
				/// Skip entries if we have triggered a HideNextX
				if (skipNextX > 0)
				{
					skipNextX--;
					continue;
				}

				EditorGUILayout.PropertyField(sp);

				if (wrapNextX > 0)
				{
					wrapNextX--;
					if (wrapNextX == 0)
						EditorGUILayout.EndVertical();
				}

				/// Handling for HideNextXAttribute
				var obj = sp.serializedObject.targetObject.GetType();
				var fld = obj.GetField(sp.name);
				if (fld != null)
				{
					var attrs = fld.GetCustomAttributes(false);
					foreach (var a in attrs)
					{
						var hnx = a as HideNextXAttribute;
						if (hnx != null)
							if (sp.propertyType == SerializedPropertyType.Boolean)
								if (sp.boolValue == hnx.hideIf)
								{
									skipNextX = (a as HideNextXAttribute).hideCount;
								}
								else
								{
									wrapNextX = (a as HideNextXAttribute).hideCount;
									if (hnx.guiStyle != null || hnx.guiStyle == "")
										EditorGUILayout.BeginVertical((GUIStyle)hnx.guiStyle);
									else
										EditorGUILayout.BeginVertical();
								}
					}
				}
				
			}

			if (EditorGUI.EndChangeCheck())
			{
				so.ApplyModifiedProperties();
			}
		}

		//public static void InitalizeStaticTextures()
		//{
		//	//defaultBackTexture = (Texture2D)Resources.Load<Texture2D>("EditorHeaderBack");
		//	//defaultTeapotTexture = (Texture2D)Resources.Load<Texture2D>("EditorHeaderTeapot");
		//	//defaultTextTexture = (Texture2D)Resources.Load<Texture2D>("EditorHeaderText");

		//	//settingsBackTexture = (Texture2D)Resources.Load<Texture2D>("EditorHeaderGray");
		//	//settingsTextTexture = (Texture2D)Resources.Load<Texture2D>("EditorSettingsText");
			

		//}
		//public static void DrawDefaultHeader(string HelpURL, ref bool showInstructions, string instructions)
		//{
		//	if (defaultBackTexture == null)
		//		InitalizeStaticTextures();

		//	OverlayHeader(HelpURL, defaultBackTexture, null, defaultTeapotTexture, defaultTextTexture);
		//}

		//public static void DrawSettingsHeader(string HelpURL, ref bool showInstructions, string instructions)
		//{
		//	if (settingsBackTexture == null)
		//		InitalizeStaticTextures();

		//	OverlayHeader(HelpURL, settingsBackTexture, null, defaultTeapotTexture, settingsTextTexture);
		//}

		public static GUIStyle defaultVertBoxStyle;
		const int vertpad = 6;
		static int holdIndent;

		public static Rect BeginVerticalBox(GUIStyle gstyle = null)
		{
			if (defaultVertBoxStyle == null)
				defaultVertBoxStyle = new GUIStyle((GUIStyle)"HelpBox")
				{
					margin = new RectOffset(),
					padding = new RectOffset(vertpad, vertpad, vertpad, vertpad)
				};

			Rect r = EditorGUILayout.BeginVertical(defaultVertBoxStyle);

			return r;
		}

		public static void EndVerticalBox()
		{
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}

		protected static void Divider()
		{
			EditorGUILayout.Space();
			Rect r = EditorGUILayout.GetControlRect(false, 2);
			EditorGUI.DrawRect(r, Color.black);
			EditorGUILayout.Space();
		}

		protected bool IndentedFoldout(GUIContent gc, bool folded, int indent)
		{
			var holdindent = EditorGUI.indentLevel;
			EditorGUI.indentLevel += indent;
			var r = EditorGUILayout.GetControlRect();
			EditorGUI.LabelField(r, gc);
			bool val = EditorGUI.Toggle(new Rect(r) { x = r.x - 12 }, GUIContent.none, folded, (GUIStyle)"Foldout");
			EditorGUI.indentLevel = holdindent;
			return val;
		}

		protected static void CustomGUIRender(SerializedObject so)
		{
			var property = so.GetIterator();
			property.Next(true);
			property.NextVisible(true);

			do
			{
				EditorGUILayout.PropertyField(property);
			}
			while (property.NextVisible(false));
		}

	}



#endif

}

