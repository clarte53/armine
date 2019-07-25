using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Armine.Shaders;
using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Armine.Editor.Tools
{
	[InitializeOnLoad]
	public class ShadersDatabase
	{
		private struct ShaderName
		{
			public string name;
			public bool builtin;

			public ShaderName(string n, bool b)
			{
				name = n;
				builtin = b;
			}
		}

		#region Members
		private const string resourceFileName = Constants.shaderDatabase + ".txt";
		private const string resourceFilePath = "Modules/Armine/Armine/Resources/";
		private const string downloadURL = "https://unity3d.com/get-unity/download/archive";
		private const string regexURL = @"(https?:\/\/[\w\/.-]+\/[0-9a-f]{12}\/)builtin_shaders-(\d+\.\d+\.\d+\w\d+)[\w\/.-]+";
		private const string regexShader = @"^\s*Shader\s+""([\w+\/.-]+)""";
		private const string shaderExtension = "shader";

		private static CLARTE.Serialization.Binary serializer = new CLARTE.Serialization.Binary();
		private static IEnumerator it = null;
		#endregion

		#region Constructors
		static ShadersDatabase()
		{
			// Rebuild database when editor start, at play and every rebuilds
			if(it == null)
			{
				it = GetAllshaders();

				EditorApplication.update += EditorCoroutine;
			}
		}
		#endregion

		#region Menu shortcuts
		[MenuItem("Armine/Tools/Build shaders database", false, 9)]
		public static void MenuBuildDatabase()
		{
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

			watch.Start();

			IEnumerator it = GetAllshaders();

			while(it.MoveNext())
			{ }

			watch.Stop();

			Debug.LogFormat("Created shaders database. Elapsed time: {0}", watch.Elapsed);
		}
		#endregion

		#region Shaders analysis coroutines
		private static void EditorCoroutine()
		{
			if(it != null)
			{
				if(!it.MoveNext())
				{
					EditorApplication.update -= EditorCoroutine;

					it = null;
				}
			}
		}

		private static IEnumerator GetAllshaders()
		{
			HashSet<ShaderName> shaders = new HashSet<ShaderName>();

			IEnumerator it = DownloadBuiltinShaders(shaders);

			while(it.MoveNext())
			{
				yield return it.Current;
			}

			it = GetCustomShadersNames(shaders);

			while(it.MoveNext())
			{
				yield return it.Current;
			}

			Dictionary<int, string[]> properties = new Dictionary<int, string[]>();

			it = GetShadersProperties(shaders, properties);

			while(it.MoveNext())
			{
				yield return it.Current;
			}

			string serialized_file = string.Format("{0}{1}{2}{3}", Application.dataPath, Path.AltDirectorySeparatorChar, resourceFilePath, resourceFileName);

			it = serializer.Serialize(properties, serialized_file);

			while(it.MoveNext())
			{
				yield return it.Current;
			}
		}

		private static IEnumerator DownloadBuiltinShaders(HashSet<ShaderName> shaders)
		{
			using(UnityWebRequest web_page = UnityWebRequest.Get(downloadURL))
			{
#if UNITY_2017_2_OR_NEWER
				yield return web_page.SendWebRequest();
#else
                yield return web_page.Send();
#endif

                while(!web_page.isDone)
                {
                    yield return web_page;
                }

				if(string.IsNullOrEmpty(web_page.error))
				{
					Regex regex = new Regex(regexURL);

					MatchCollection matches = regex.Matches(web_page.downloadHandler.text);

					foreach(Match match in matches)
					{
						if(match.Groups[2].Value == Application.unityVersion)
						{
							IEnumerator it = GetUnityBuiltinShadersNames(shaders, new Uri(match.Value, UriKind.Absolute));

							while(it.MoveNext())
							{
								yield return it.Current;
							}

							break; // Because we have multiple versions of the url (i.e. for windows and mac, but both are the same)
						}
					}
				}
				else
				{
					Debug.LogErrorFormat("Failed to fetch unity builtin shaders download URL: {0}", web_page.error);
				}
			}
		}

		private static IEnumerator GetUnityBuiltinShadersNames(HashSet<ShaderName> shaders, Uri uri)
		{
			string filename = Path.GetFileName(uri.LocalPath);

			string file = string.Format("{0}{1}{2}", Application.temporaryCachePath, Path.AltDirectorySeparatorChar, filename);

			if(!File.Exists(file))
			{
				using(UnityWebRequest zip_resource = UnityWebRequest.Get(uri.ToString()))
				{
#if UNITY_2017_2_OR_NEWER
					yield return zip_resource.SendWebRequest();
#else
                    yield return zip_resource.Send();
#endif

                    while(!zip_resource.isDone)
                    {
                        yield return zip_resource;
                    }

                    if(string.IsNullOrEmpty(zip_resource.error))
					{
						File.WriteAllBytes(file, zip_resource.downloadHandler.data);
					}
					else
					{
						Debug.LogErrorFormat("Failed to download unity builtin shaders: {0}", zip_resource.error);
					}
				}
			}

			if(File.Exists(file))
			{
				CLARTE.Threads.Result result = CLARTE.Threads.Tasks.Add(() =>
				{
					try
					{
						Regex regex = new Regex(regexShader);

						using(FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							using(ZipFile zf = new ZipFile(fs))
							{
								if(zf.TestArchive(true))
								{
									foreach(ZipEntry zip_entry in zf)
									{
										// Ignore directories
										if(!zip_entry.IsFile)
											continue;

										if(Path.GetExtension(zip_entry.Name).ToLower().TrimStart('.') == shaderExtension)
										{
											using(Stream shader = zf.GetInputStream(zip_entry))
											{
												using(StreamReader reader = new StreamReader(shader))
												{
													while(!reader.EndOfStream)
													{
														Match match = regex.Match(reader.ReadLine());

														if(match.Success)
														{
															shaders.Add(new ShaderName(match.Groups[1].Value, true));
														}
													}
												}
											}
										}
									}
								}
								else
								{
									Debug.LogErrorFormat("Zip file '{0}' failed integrity check!", file);
								}
							}
						}
					}
					catch(Exception e)
					{
						Debug.LogErrorFormat("Error while getting buitlin shaders names: {0}\n{1}", e.Message, e.StackTrace);
					}
				});

				while(!result.Done)
				{
					yield return null;
				}
			}
		}

		private static IEnumerator GetCustomShadersNames(HashSet<ShaderName> shaders)
		{
			string[] paths = AssetDatabase.GetAllAssetPaths();

			foreach(string s in paths)
			{
				if(!Path.IsPathRooted(s))
				{
					Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(s);

					if(shader != null)
					{
						shaders.Add(new ShaderName(shader.name, false));

						yield return null;
					}
				}
			}
		}

		private static IEnumerator GetShadersProperties(HashSet<ShaderName> shaders, Dictionary<int, string[]> properties)
		{
			Dictionary<PropertyType, HashSet<string>> shaders_properties = new Dictionary<PropertyType, HashSet<string>>();

			foreach(object value in Enum.GetValues(typeof(PropertyType)))
			{
				shaders_properties[(PropertyType) value] = new HashSet<string>();
			}

			foreach(ShaderName shader_name in shaders)
			{
				Shader shader = Shader.Find(shader_name.name);

				if(shader != null)
				{
					int property_count = ShaderUtil.GetPropertyCount(shader);

					ShaderUtil.ShaderPropertyType shader_property_type;
					PropertyType type;

					bool property_ok;

					for(int i = 0; i < property_count; i++)
					{
						property_ok = true;

						shader_property_type = ShaderUtil.GetPropertyType(shader, i);

						switch(shader_property_type)
						{
							case ShaderUtil.ShaderPropertyType.Float:
								type = PropertyType.FLOAT;
								break;
							case ShaderUtil.ShaderPropertyType.Range:
								type = PropertyType.RANGE;
								break;
							case ShaderUtil.ShaderPropertyType.Vector:
								type = PropertyType.VECTOR;
								break;
							case ShaderUtil.ShaderPropertyType.Color:
								type = PropertyType.COLOR;
								break;
							case ShaderUtil.ShaderPropertyType.TexEnv:
								type = PropertyType.TEXTURE;
								break;
							default:
								Debug.LogErrorFormat("Unsupported property type '{0}'", shader_property_type);
								type = PropertyType.FLOAT;
								property_ok = false;
								break;
						}

						if(property_ok)
						{
							string property_name = ShaderUtil.GetPropertyName(shader, i);

							if(!string.IsNullOrEmpty(property_name))
							{
								shaders_properties[type].Add(property_name);
							}
						}
					}
				}
				else if(!shader_name.builtin)
				{
					Debug.LogWarningFormat("Can not analyse shader '{0}' properties.", shader_name.name);
				}

				yield return null;
			}

			foreach(object value in Enum.GetValues(typeof(PropertyType)))
			{
				HashSet<string> properties_names = shaders_properties[(PropertyType) value];

				string[] values = new string[properties_names.Count];

				int i = 0;

				foreach(string shader_name in properties_names)
				{
					values[i] = shader_name;

					i++;
				}

				properties[(int) value] = values;
			}
		}
		#endregion
	}
}
