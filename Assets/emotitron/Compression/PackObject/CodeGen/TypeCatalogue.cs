#if UNITY_EDITOR

using emotitron.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;


namespace emotitron.Compression.Internal
{
	[CreateAssetMenu()]
	[System.Serializable]
	public class TypeCatalogue : ScriptableObject
	{
		public const string PACKABLE_PATH = "Assets/emotitron/Compression/PackObject/";
		public const string CODEGEN_PATH = "Assets/PackCodeGen/";
		public const string CODEGEN_EDITOR_RESOURCE_PATH = PACKABLE_PATH + "CodeGen/Editor/Resources/";

		public static TypeCatalogue single;

		[UnityEditor.InitializeOnLoadMethod]
		public static void Initialize()
		{
			EnsureExists();

			//EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
			//EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;

			CompilationPipeline.assemblyCompilationFinished -= CompileFinished;
			CompilationPipeline.assemblyCompilationFinished += CompileFinished;

			EditorApplication.update -= EditorUpdate;
			EditorApplication.update += EditorUpdate;
		}

		//private static void HandleOnPlayModeChanged(PlayModeStateChange obj)
		//{
		//	switch (obj)
		//	{
		//		case PlayModeStateChange.EnteredEditMode:
		//			break;

		//		case PlayModeStateChange.ExitingEditMode:
		//			break;

		//		case PlayModeStateChange.EnteredPlayMode:
		//			break;

		//		case PlayModeStateChange.ExitingPlayMode:
		//			break;
		//	}
		//}

		private static bool rebuilding;
		/// Delete any generated extensions that are throwing up errors.
		private static void CompileFinished(string arg1, CompilerMessage[] arg2)
		{

			if (!SimpleSyncSettings.Single.deleteBadCode)
				return;

			/// Check if any errors have popped up related to one of the generated files
			foreach (var arg in arg2)
			{
				
				if (arg.type == CompilerMessageType.Error && arg.file.Contains(CODEGEN_PATH))
				{
					Debug.ClearDeveloperConsole();
					Debug.LogWarning("Script errors found in SimpleSync PackObject codegen. Deleting outdated codegen...");
					deleteAllPending = true;
					//DeleteAllPackCodeGen();
					//rebuilding = true;
					//EditorUtility.SetDirty(single);
					return;
				}
			}
		}


		public static bool rescanPending;
		private static bool deleteAllPending;

		private static void EditorUpdate()
		{
			if (deleteAllPending)
			{
				deleteAllPending = false;
				DeleteAllPackCodeGen();
				rebuilding = true;
				EditorUtility.SetDirty(single);
			}
			if (rescanPending)
			{
				rescanPending = false;
				RescanAssembly();
			}
		}


		/// <summary>
		/// Trigger the rebuild at a later timing segement than the end of CompileFinished to avoid relentless looping.
		/// </summary>
		[UnityEditor.Callbacks.DidReloadScripts]
		public static void PostCompile()
		{
			if (!SimpleSyncSettings.Single.enableCodegen)
			{
				Debug.Log("Codegen disabled in " + typeof(SimpleSyncSettings).Name + ". Not checking for new or changed PackObjects (SyncVars). Enable Codegen if you are using PackObjects.");
				return;
			}

			if (rebuilding)
			{
				Debug.Log("Skipping a post compile rebuild.");
				rebuilding = false;
				return;
			}

			Debug.Log("Rescanning assembly for [PackObject] changes.");
			rescanPending = true;

		}

		/// <summary>
		/// Finds or creates the singleton database.
		/// </summary>
		/// <returns></returns>
		public static TypeCatalogue EnsureExists()
		{
			if (single == null)
				single = Resources.Load<TypeCatalogue>("TypeCatalogue");

			if (single == null)
			{
				single = ScriptableObject.CreateInstance<TypeCatalogue>();
				AssetDatabase.CreateAsset(single, CODEGEN_EDITOR_RESOURCE_PATH + "TypeCatalogue.asset");
				AssetDatabase.Refresh();
			}

			return single;
		}

		public TypeInfoDict catalogue = new TypeInfoDict();

		[MenuItem(SimpleSyncSettings.MENU_PATH + "Delete All PackObj Codegen")]
		public static void DeleteAllPackCodeGen()
		{
			/// Get collection of current CodeGen files
			DirectoryInfo d = new DirectoryInfo(CODEGEN_PATH);//Assuming Test is your Folder
			FileInfo[] files = d.GetFiles("*.cs"); //Getting Text files

			if (files.Length == 0)
				return;

			foreach (var f in files)
			{
				File.Delete(f.FullName);
			}

			single.catalogue.Clear();
			EditorUtility.SetDirty(single);

			AssetDatabase.Refresh();
		}

		[MenuItem(SimpleSyncSettings.MENU_PATH + "Rebuild PackObj Codegen")]
		public static void RebuildSNSCodegen()
		{
			Compression.Internal.TypeCatalogue.DeleteAllPackCodeGen();
			Compression.Internal.TypeCatalogue.rescanPending = true;
		}


		private static List<string> reusableFilePaths = new List<string>();
		private static HashSet<Type> tempProcessedTypes = new HashSet<Type>();

		//[MenuItem("Window/Rescan ASM")]
		public static void RescanAssembly()
		{
			EnsureExists();

			var watch0 = System.Diagnostics.Stopwatch.StartNew();

			AssetDatabase.Refresh();
			bool haschanged = false;

			/// Get collection of current CodeGen files
			DirectoryInfo d = new DirectoryInfo(CODEGEN_PATH);//Assuming Test is your Folder

			/// Create directory if its gone missing.
			if (!d.Exists)
				d.Create();

			FileInfo[] files = d.GetFiles("*.cs"); //Getting Text files

			tempProcessedTypes.Clear();

			/// Make record of all codegen files, so we can clean up any unassociated ones.
			reusableFilePaths.Clear();
			foreach (var f in files)
				reusableFilePaths.Add(CODEGEN_PATH + f.Name);

			/// Check every type in the ASM for PackObj, and Catalogue them
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var t in a.GetTypes())
				{
					var typeinfo = single.MakeRecordCurrent(t, ref haschanged);
					/// Remove from our deletion filepaths list
					if (typeinfo != null)
					{
						reusableFilePaths.Remove(typeinfo.filepath);
					}
				}

			/// Delete any files that don't have an associated typeinfo
			foreach (var f in reusableFilePaths)
			{
				Debug.Log("<b>Deleting outdated file: </b>" + f);
				File.Delete(f);
				haschanged = true;
			}

			if (haschanged)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			watch0.Stop();
			Debug.Log("Rebuilding PackObject Codegen - took <i>" + watch0.ElapsedMilliseconds + "ms</i>. "
				+ (haschanged ? "Changes where found." : "No changes found.")
				+ "\nNOTE: Automatic PackObject codegen can be disabled in SimpleSyncSettings.");
		}


		public TypeInfo MakeRecordCurrent(Type type, ref bool haschanged)
		{
			tempProcessedTypes.Add(type);

			TypeInfo currTypeInfo;
			int index = catalogue.TryGetValue(type.FullName, out currTypeInfo);

			var attrs = type.GetCustomAttributes(typeof(PackObjectAttribute), false);

			/// Has no attributes, not even worth checking.
			if (attrs.Length == 0)
			{
				return null;
			}

#if UNITY_2018_1_OR_NEWER
			if (!PlayerSettings.allowUnsafeCode && type.IsValueType)
			{
				Debug.LogWarning(typeof(PackObjectAttribute).Name + " on structs will be ignored unless PlayerSettings.allowUnsafeCode == true.");
				return null;
			}
#endif

			/// Is a packobject, create or amend as needed
			var packObjAttr = (attrs[0] as PackObjectAttribute);

			/// Attribute is not PackObject
			if (packObjAttr == null)
			{
				/// not a packObj, but a record exists. Delete it.
				if (currTypeInfo != null)
				{
					catalogue.Remove(type.FullName);
					//File.Delete(filepath);
					Debug.Log("Deleting recordless codegen " + type.Name);
					haschanged = true;
				}
				return null;
			}

			/// Is a packobject, but is a struct that is managed (needs to be unmanaged - no refs in it)
			if (type.IsValueType && !type.IsUnManaged())
			{
				Debug.LogWarning(type.Name + " is a PackObject, but cannot be packed because it is a managed type. Structs cannot contain references or any managed types to be packable. ");

				/// not a packObj, but a record exists. Delete it.
				if (currTypeInfo != null)
				{
					catalogue.Remove(type.FullName);
					haschanged = true;
				}
				return null;
			}

			/// This is a new record, so create our type for filling
			if (index == -1)
			{
				currTypeInfo = new TypeInfo(type);
				haschanged = true;
			}

			GetPackableFields(type, currTypeInfo, packObjAttr);

			/// Type is a packObj, but no record exists yet.
			/// Get a list of fields and associated pack attr for each field
			if (currTypeInfo == null)
			{
				haschanged = true;
				Debug.Log("No record yet for " + type.Name);
				return GenerateAndRecord(type, currTypeInfo, packObjAttr);
			}

			currTypeInfo.filepath = GetExtFilepath(type);

			/// If generated file time has changed - it can't be trusted. Delete and regen.
			if (currTypeInfo.codegenFileWriteTime != File.GetLastWriteTime(currTypeInfo.filepath).Ticks)
			{
				haschanged = true;
				Debug.Log("Codegen file time out of date, regenerating. " + currTypeInfo.filepath);
				return GenerateAndRecord(type, currTypeInfo, packObjAttr);
			}

			long hash = type.TypeToHash64();
			/// If field/attrs don't match, regenerate
			if (currTypeInfo.hashcode != hash)
			{
				haschanged = true;
				//Debug.Log(currTypeInfo.hashcode + " Type Compare mismatch " + hash);
				currTypeInfo.hashcode = hash;
				return GenerateAndRecord(type, currTypeInfo, packObjAttr);
			}

			//Debug.Log(type.Name + " has not changed " + currTypeInfo.hashcode);
			haschanged = false;
			return currTypeInfo;
		}

		public TypeInfo GenerateAndRecord(Type type, TypeInfo typeInfo, PackObjectAttribute packObjAttr)
		{
			string filepath = typeInfo.filepath;

			StringBuilder sb = type.GeneratePackCode(typeInfo, packObjAttr);
			Debug.Log("Generating codegen for PackObj <b>" + type.FullName + "</b>");
			File.WriteAllText(filepath, sb.ToString());

			typeInfo.codegenFileWriteTime = File.GetLastWriteTime(filepath).Ticks;
			catalogue.Add(type, typeInfo);

			EditorUtility.SetDirty(this);
			//AssetDatabase.SaveAssets();

			return typeInfo;
		}


		public void GetPackableFields(Type type, TypeInfo currTypeInfo, PackObjectAttribute packObjAttr)
		{
			int nestedFieldCount = 0;
			int localFieldCount = 0;

			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

			var defaultInclusion = packObjAttr.defaultInclusion;

			int cnt = fields.Length;
			for (int i = 0; i < cnt; ++i)
			{
				var fieldInfo = fields[i];

				///// Don't include any nested packObjs that will produce a nested loop
				//if (type.CheckForNestedLoop())
				//{
				//	Debug.LogWarning("<b>" + type.Name + "</b> contains a nested loop with field <b>" + fieldInfo.FieldType + " " + fieldInfo.Name + "</b>. Will not be included in serialization.");
				//	continue;
				//}

				var attrs = fieldInfo.GetCustomAttributes(typeof(PackBaseAttribute), false);

				/// Only pack if marked with a Pack, or if we are set to capture all public
				if (defaultInclusion == DefaultPackInclusion.Explicit && attrs.Length == 0)
					continue;

				/// Count up fields in nested
				var nestedAttrs = fieldInfo.FieldType.GetCustomAttributes(typeof(PackObjectAttribute), false);
				if (nestedAttrs.Length != 0)
				{
					bool haschanged = false;
					bool alreadyCurrent = (tempProcessedTypes.Contains(fieldInfo.FieldType));
					var nestedTypeInfo = (alreadyCurrent) ? catalogue.GetTypeInfo(fieldInfo.FieldType) : MakeRecordCurrent(fieldInfo.FieldType, ref haschanged);

					if (nestedTypeInfo != null)
						nestedFieldCount += nestedTypeInfo.totalFieldCount;
					else
						continue;
				}
				else
					localFieldCount++;
			}

			currTypeInfo.localFieldCount = localFieldCount;
			currTypeInfo.totalFieldCount = localFieldCount + nestedFieldCount;
		}

		public static string GetExtFilepath(Type type)
		{
			string filename = "Pack_" + type.Name + ".cs";
			return CODEGEN_PATH + filename;
		}
	}
}

#endif