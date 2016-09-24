﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FairyGUI.Utils;
using System.Text;

namespace FairyGUI
{
	/// <summary>
	/// A UI Package contains a description file and some texture,sound assets.
	/// </summary>
	public class UIPackage
	{
		/// <summary>
		/// Package id. It is generated by the Editor, or set by customId.
		/// </summary>
		public string id { get; private set; }

		/// <summary>
		/// Package name.
		/// </summary>
		public string name { get; private set; }

		/// <summary>
		/// The path relative to the resources folder.
		/// </summary>
		public string assetPath { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="extension"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public delegate UnityEngine.Object LoadResource(string name, string extension, System.Type type);

		List<PackageItem> _items;
		Dictionary<string, PackageItem> _itemsById;
		Dictionary<string, PackageItem> _itemsByName;
		Dictionary<string, string> _descPack;
		Dictionary<string, PixelHitTestData> _hitTestDatas;
		AssetBundle _resBundle;
		string _assetNamePrefix;
		string _customId;
		bool _fromBundle;

		class AtlasSprite
		{
			public string atlas;
			public Rect rect;
			public bool rotated;
		}
		Dictionary<string, AtlasSprite> _sprites;

		LoadResource _loadFunc;

		static Dictionary<string, UIPackage> _packageInstById = new Dictionary<string, UIPackage>();
		static Dictionary<string, UIPackage> _packageInstByName = new Dictionary<string, UIPackage>();
		static List<UIPackage> _packageList = new List<UIPackage>();
		static Dictionary<string, Dictionary<string, string>> _stringsSource;

		internal static int _constructing;
		internal static string URL_PREFIX = "ui://";

		public UIPackage()
		{
			_items = new List<PackageItem>();
			_sprites = new Dictionary<string, AtlasSprite>();
			_hitTestDatas = new Dictionary<string, PixelHitTestData>();
		}

		/// <summary>
		/// Return a UIPackage with a certain id.
		/// </summary>
		/// <param name="id">ID of the package.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage GetById(string id)
		{
			UIPackage pkg;
			if (_packageInstById.TryGetValue(id, out pkg))
				return pkg;
			else
				return null;
		}

		/// <summary>
		/// Return a UIPackage with a certain name.
		/// </summary>
		/// <param name="name">Name of the package.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage GetByName(string name)
		{
			UIPackage pkg;
			if (_packageInstByName.TryGetValue(name, out pkg))
				return pkg;
			else
				return null;
		}

		/// <summary>
		/// Add a UI package from assetbundle.
		/// </summary>
		/// <param name="bundle">A assetbundle.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(AssetBundle bundle)
		{
			return AddPackage(bundle, bundle, null);
		}

		/// <summary>
		/// Add a UI package from two assetbundles. desc and res can be same.
		/// </summary>
		/// <param name="desc">A assetbunble contains description file.</param>
		/// <param name="res">A assetbundle contains resources.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(AssetBundle desc, AssetBundle res)
		{
			return AddPackage(desc, res, null);
		}

		/// <summary>
		/// Add a UI package from two assetbundles with a optional main asset name.
		/// </summary>
		/// <param name="desc">A assetbunble contains description file.</param>
		/// <param name="res">A assetbundle contains resources.</param>
		/// <param name="mainAssetName">Main asset name.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(AssetBundle desc, AssetBundle res, string mainAssetName)
		{
			string source = null;
#if UNITY_5
			if (mainAssetName != null)
			{
				TextAsset ta = desc.LoadAsset<TextAsset>(mainAssetName);
				if (ta != null)
					source = ta.text;
			}
			else
			{
				string[] names = desc.GetAllAssetNames();
				foreach (string n in names)
				{
					if (n.IndexOf("@") == -1)
					{
						TextAsset ta = desc.LoadAsset<TextAsset>(n);
						if (ta != null)
						{
							source = ta.text;
							if (mainAssetName == null)
								mainAssetName = Path.GetFileNameWithoutExtension(n);
							break;
						}
					}
				}
			}
#else
			if (mainAssetName != null)
			{
				TextAsset ta = (TextAsset)desc.Load(mainAssetName, typeof(TextAsset));
				if (ta != null)
					source = ta.text;
			}
			else
			{
				source = ((TextAsset)desc.mainAsset).text;
				mainAssetName = desc.mainAsset.name;
			}
#endif
			if (source == null)
				throw new Exception("FairyGUI: invalid package.");
			if (desc != res)
				desc.Unload(true);
			return AddPackage(source, res, mainAssetName);
		}

		/// <summary>
		/// Add a UI package from a description text and a assetbundle.
		/// </summary>
		/// <param name="desc">Description text.</param>
		/// <param name="res">A assetbundle contains resources.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(string desc, AssetBundle res)
		{
			return AddPackage(desc, res, null);
		}

		/// <summary>
		/// Add a UI package from a description text and a assetbundle, with a optional main asset name.
		/// </summary>
		/// <param name="desc">Description text.</param>
		/// <param name="res">A assetbundle contains resources.</param>
		/// <param name="mainAssetName">Main asset name.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(string desc, AssetBundle res, string mainAssetName)
		{
			UIPackage pkg = new UIPackage();
			pkg.Create(desc, res, mainAssetName);
			_packageInstById[pkg.id] = pkg;
			_packageInstByName[pkg.name] = pkg;
			_packageList.Add(pkg);
			return pkg;
		}

		/// <summary>
		/// Add a UI package from a path relative to Unity Resources path.
		/// </summary>
		/// <param name="descFilePath">Path relative to Unity Resources path.</param>
		/// <returns>UIPackage</returns>
		public static UIPackage AddPackage(string descFilePath)
		{
			return AddPackage(descFilePath, (string name, string extension, System.Type type) => { return Resources.Load(name, type); });
		}

		public static UIPackage AddPackage(string assetPath, UIPackage.LoadResource loadFunc)
		{
			if (_packageInstById.ContainsKey(assetPath))
				return _packageInstById[assetPath];

			TextAsset asset = (TextAsset)loadFunc(assetPath, ".bytes", typeof(TextAsset));
			if (asset == null)
			{
				if (Application.isPlaying)
					throw new Exception("FairyGUI: Cannot load ui package in '" + assetPath + "'");
				else
					Debug.LogWarning("FairyGUI: Cannot load ui package in '" + assetPath + "'");
			}

			UIPackage pkg = new UIPackage();
			pkg._loadFunc = loadFunc;
			pkg.Create(asset.text, null, assetPath);
			if (_packageInstById.ContainsKey(pkg.id))
				Debug.LogWarning("FairyGUI: Package id conflicts, '" + pkg.name + "' and '" + _packageInstById[pkg.id].name + "'");
			_packageInstById[pkg.id] = pkg;
			_packageInstByName[pkg.name] = pkg;
			_packageInstById[assetPath] = pkg;
			_packageList.Add(pkg);
			pkg.assetPath = assetPath;

			return pkg;
		}

		/// <summary>
		/// Remove a package. All resources in this package will be disposed.
		/// </summary>
		/// <param name="packageIdOrName"></param>
		public static void RemovePackage(string packageIdOrName)
		{
			RemovePackage(packageIdOrName, false);
		}

		/// <summary>
		/// Remove a package. All resources in this package will be disposed.
		/// </summary>
		/// <param name="packageIdOrName"></param>
		/// <param name="allowDestroyingAssets"></param>
		public static void RemovePackage(string packageIdOrName, bool allowDestroyingAssets)
		{
			UIPackage pkg = null;
			if (!_packageInstById.TryGetValue(packageIdOrName, out pkg))
			{
				if (!_packageInstByName.TryGetValue(packageIdOrName, out pkg))
					throw new Exception("FairyGUI: '" + packageIdOrName + "' is not a valid package id or name.");
			}
			pkg.Dispose(allowDestroyingAssets);
			_packageInstById.Remove(packageIdOrName);
			if (pkg._customId != null)
				_packageInstById.Remove(pkg._customId);
			if (pkg.assetPath != null)
				_packageInstById.Remove(pkg.assetPath);
			_packageInstByName.Remove(pkg.name);
			_packageList.Remove(pkg);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void RemoveAllPackages()
		{
			if (_packageInstById.Count > 0)
			{
				UIPackage[] pkgs = _packageList.ToArray();

				foreach (UIPackage pkg in pkgs)
				{
					pkg.Dispose(false);
				}
			}
			_packageList.Clear();
			_packageInstById.Clear();
			_packageInstByName.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<UIPackage> GetPackages()
		{
			return _packageList;
		}

		/// <summary>
		/// Create a UI object.
		/// </summary>
		/// <param name="pkgName">Package name.</param>
		/// <param name="resName">Resource name.</param>
		/// <returns>A UI object.</returns>
		public static GObject CreateObject(string pkgName, string resName)
		{
			UIPackage pkg = GetByName(pkgName);
			if (pkg != null)
				return pkg.CreateObject(resName);
			else
				return null;
		}

		/// <summary>
		///  Create a UI object.
		/// </summary>
		/// <param name="pkgName">Package name.</param>
		/// <param name="resName">Resource name.</param>
		/// <param name="userClass">Custom implementation of this object.</param>
		/// <returns>A UI object.</returns>
		public static GObject CreateObject(string pkgName, string resName, System.Type userClass)
		{
			UIPackage pkg = GetByName(pkgName);
			if (pkg != null)
				return pkg.CreateObject(resName, userClass);
			else
				return null;
		}

		/// <summary>
		/// Create a UI object.
		/// </summary>
		/// <param name="url">Resource url.</param>
		/// <returns>A UI object.</returns>
		public static GObject CreateObjectFromURL(string url)
		{
			PackageItem pi = GetItemByURL(url);
			if (pi != null)
				return pi.owner.CreateObject(pi, null);
			else
				return null;
		}

		/// <summary>
		/// Create a UI object.
		/// </summary>
		/// <param name="url">Resource url.</param>
		/// <param name="userClass">Custom implementation of this object.</param>
		/// <returns>A UI object.</returns>
		public static GObject CreateObjectFromURL(string url, System.Type userClass)
		{
			PackageItem pi = GetItemByURL(url);
			if (pi != null)
				return pi.owner.CreateObject(pi, userClass);
			else
				return null;
		}

		/// <summary>
		/// Get a asset with a certain name.
		/// </summary>
		/// <param name="pkgName">Package name.</param>
		/// <param name="resName">Resource name.</param>
		/// <returns>If resource is atlas, returns NTexture; If resource is sound, returns AudioClip.</returns>
		public static object GetItemAsset(string pkgName, string resName)
		{
			UIPackage pkg = GetByName(pkgName);
			if (pkg != null)
				return pkg.GetItemAsset(resName);
			else
				return null;
		}

		/// <summary>
		/// Get a asset with a certain name.
		/// </summary>
		/// <param name="url">Resource url.</param>
		/// <returns>If resource is atlas, returns NTexture; If resource is sound, returns AudioClip.</returns>
		public static object GetItemAssetByURL(string url)
		{
			PackageItem item = GetItemByURL(url);
			if (item == null)
				return null;

			return item.owner.GetItemAsset(item);
		}

		/// <summary>
		/// Get url of an item in package.
		/// </summary>
		/// <param name="pkgName">Package name.</param>
		/// <param name="resName">Resource name.</param>
		/// <returns>Url.</returns>
		public static string GetItemURL(string pkgName, string resName)
		{
			UIPackage pkg = GetByName(pkgName);
			if (pkg == null)
				return null;

			PackageItem pi;
			if (!pkg._itemsByName.TryGetValue(resName, out pi))
				return null;

			return URL_PREFIX + pkg.id + pi.id;
		}

		public static PackageItem GetItemByURL(string url)
		{
			if (url.Length > 13)
			{
				string pkgId = url.Substring(5, 8);
				string srcId = url.Substring(13);
				UIPackage pkg = GetById(pkgId);
				if (pkg != null)
					return pkg.GetItem(srcId);
			}
			return null;
		}

		/// <summary>
		/// Set strings source.
		/// </summary>
		/// <param name="source"></param>
		public static void SetStringsSource(XML source)
		{
			_stringsSource = new Dictionary<string, Dictionary<string, string>>();
			XMLList list = source.Elements("string");
			foreach (XML cxml in list)
			{
				string key = cxml.GetAttribute("name");
				string text = cxml.text;
				int i = key.IndexOf("-");
				if (i == -1)
					continue;

				string key2 = key.Substring(0, i);
				string key3 = key.Substring(i + 1);
				Dictionary<string, string> col;
				if (!_stringsSource.TryGetValue(key2, out col))
				{
					col = new Dictionary<string, string>();
					_stringsSource[key2] = col;
				}
				col[key3] = text;
			}
		}

		/// <summary>
		/// Set a custom id for package, then you can use it in GetById.
		/// </summary>
		public string customId
		{
			get { return _customId; }
			set
			{
				if (_customId != null)
					_packageInstById.Remove(_customId);
				_customId = value;
				if (_customId != null)
					_packageInstById[_customId] = this;
			}
		}

		public PixelHitTestData GetPixelHitTestData(string itemId)
		{
			PixelHitTestData ret;
			if (_hitTestDatas.TryGetValue(itemId, out ret))
				return ret;
			else
				return null;
		}

		void Create(string desc, AssetBundle res, string mainAssetName)
		{
			_descPack = new Dictionary<string, string>();
			_resBundle = res;

			DecodeDesc(desc);

			if (res != null)
			{
				if (mainAssetName != null && mainAssetName.Length > 0)
					_assetNamePrefix = mainAssetName + "@";
				else
					_assetNamePrefix = "";
				_fromBundle = true;
			}
			else
			{
				_assetNamePrefix = mainAssetName + "@";
				_fromBundle = false;
			}

			LoadPackage();
		}

		void DecodeDesc(string source)
		{
			int curr = 0;
			string fn;
			int size;
			while (true)
			{
				int pos = source.IndexOf("|", curr);
				if (pos == -1)
					break;
				fn = source.Substring(curr, pos - curr);
				curr = pos + 1;
				pos = source.IndexOf("|", curr);
				size = int.Parse(source.Substring(curr, pos - curr));
				curr = pos + 1;
				_descPack[fn] = source.Substring(curr, size);
				curr += size;
			}
		}

		void LoadPackage()
		{
			string[] arr = null;
			string str;

			str = LoadString("sprites.bytes");
			if (str == null)
			{
				Debug.LogError("FairyGUI: cannot load package from '" + _assetNamePrefix + "'");
				return;
			}
			arr = str.Split('\n');
			int cnt = arr.Length;
			for (int i = 1; i < cnt; i++)
			{
				str = arr[i];
				if (str.Length == 0)
					continue;

				string[] arr2 = str.Split(' ');
				AtlasSprite sprite = new AtlasSprite();
				string itemId = arr2[0];
				int binIndex = int.Parse(arr2[1]);
				if (binIndex >= 0)
					sprite.atlas = "atlas" + binIndex;
				else
				{
					int pos = itemId.IndexOf("_");
					if (pos == -1)
						sprite.atlas = "atlas_" + itemId;
					else
						sprite.atlas = "atlas_" + itemId.Substring(0, pos);
				}
				sprite.rect.x = int.Parse(arr2[2]);
				sprite.rect.y = int.Parse(arr2[3]);
				sprite.rect.width = int.Parse(arr2[4]);
				sprite.rect.height = int.Parse(arr2[5]);
				sprite.rotated = arr2[6] == "1";
				_sprites[itemId] = sprite;
			}

			byte[] hittestData = LoadBinary("hittest.bytes");
			if (hittestData != null)
			{
				ByteBuffer ba = new ByteBuffer(hittestData);
				while (ba.bytesAvailable)
				{
					PixelHitTestData pht = new PixelHitTestData();
					_hitTestDatas[ba.ReadString()] = pht;
					pht.Load(ba);
				}
			}

			if (!_descPack.TryGetValue("package.xml", out str))
				throw new Exception("FairyGUI: invalid package '" + _assetNamePrefix + "'");

			XML xml = new XML(str);

			id = xml.GetAttribute("id");
			name = xml.GetAttribute("name");

			XML rxml = xml.GetNode("resources");
			if (rxml == null)
				throw new Exception("FairyGUI: invalid package xml '" + _assetNamePrefix + "'");

			XMLList resources = rxml.Elements();

			_itemsById = new Dictionary<string, PackageItem>();
			_itemsByName = new Dictionary<string, PackageItem>();
			PackageItem pi;

			foreach (XML cxml in resources)
			{
				pi = new PackageItem();
				pi.owner = this;
				pi.type = FieldTypes.ParsePackageItemType(cxml.name);
				pi.id = cxml.GetAttribute("id");
				pi.name = cxml.GetAttribute("name");
				pi.exported = cxml.GetAttributeBool("exported");
				pi.file = cxml.GetAttribute("file");
				str = cxml.GetAttribute("size");
				if (str != null)
				{
					arr = str.Split(',');
					pi.width = int.Parse(arr[0]);
					pi.height = int.Parse(arr[1]);
				}
				switch (pi.type)
				{
					case PackageItemType.Image:
						{
							string scale = cxml.GetAttribute("scale");
							if (scale == "9grid")
							{
								arr = cxml.GetAttributeArray("scale9grid");
								if (arr != null)
								{
									Rect rect = new Rect();
									rect.x = int.Parse(arr[0]);
									rect.y = int.Parse(arr[1]);
									rect.width = int.Parse(arr[2]);
									rect.height = int.Parse(arr[3]);
									pi.scale9Grid = rect;
								}
							}
							else if (scale == "tile")
								pi.scaleByTile = true;
							break;
						}

					case PackageItemType.Font:
						{
							pi.bitmapFont = new BitmapFont(pi);
							FontManager.RegisterFont(pi.bitmapFont, null);
							break;
						}
				}
				_items.Add(pi);
				_itemsById[pi.id] = pi;
				if (pi.name != null)
					_itemsByName[pi.name] = pi;
			}

			bool preloadAll = Application.isPlaying;
			if (preloadAll)
			{
				cnt = _items.Count;
				for (int i = 0; i < cnt; i++)
					GetItemAsset(_items[i]);

				_descPack = null;
				_sprites = null;
			}
			else
				_items.Sort(ComparePackageItem);

			if (_resBundle != null)
			{
				_resBundle.Unload(false);
				_resBundle = null;
			}
		}

		static int ComparePackageItem(PackageItem p1, PackageItem p2)
		{
			if (p1.name != null && p2.name != null)
				return p1.name.CompareTo(p2.name);
			else
				return 0;
		}

		void Dispose(bool allowDestroyingAssets)
		{
			int cnt = _items.Count;
			for (int i = 0; i < cnt; i++)
			{
				PackageItem pi = _items[i];
				if (pi.texture != null)
				{
					if (pi.texture.alphaTexture != null)
					{
						pi.texture.alphaTexture.Dispose(allowDestroyingAssets);
						pi.texture.alphaTexture = null;
					}

					if (pi.texture != NTexture.Empty)
						pi.texture.Dispose(allowDestroyingAssets);
					else
						pi.texture.DestroyMaterials();
					pi.texture = null;
				}
				else if (pi.audioClip != null)
				{
					if (allowDestroyingAssets)
					{
						if (_fromBundle)
							AudioClip.DestroyImmediate(pi.audioClip);
						else
							Resources.UnloadAsset(pi.audioClip);
					}
					pi.audioClip = null;
				}
				else if (pi.bitmapFont != null)
					FontManager.UnregisterFont(pi.bitmapFont);
			}
			_items.Clear();

			if (_resBundle != null)
				_resBundle.Unload(true);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resName"></param>
		/// <returns></returns>
		public GObject CreateObject(string resName)
		{
			PackageItem pi;
			if (!_itemsByName.TryGetValue(resName, out pi))
			{
				Debug.LogError("FairyGUI: resource not found - " + resName + " in " + this.name);
				return null;
			}

			return CreateObject(pi, null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resName"></param>
		/// <param name="userClass"></param>
		/// <returns></returns>
		public GObject CreateObject(string resName, System.Type userClass)
		{
			PackageItem pi;
			if (!_itemsByName.TryGetValue(resName, out pi))
			{
				Debug.LogError("FairyGUI: resource not found - " + resName + " in " + this.name);
				return null;
			}

			return CreateObject(pi, userClass);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="resName"></param>
		/// <returns></returns>
		public object GetItemAsset(string resName)
		{
			PackageItem pi;
			if (!_itemsByName.TryGetValue(resName, out pi))
			{
				Debug.LogError("FairyGUI: Resource not found - " + resName + " in " + this.name);
				return null;
			}

			return GetItemAsset(pi);
		}

		public List<PackageItem> GetItems()
		{
			return _items;
		}

		internal GObject CreateObject(PackageItem item, System.Type userClass)
		{
			GObject g = null;
			if (item.type == PackageItemType.Component)
			{
				if (userClass != null)
					g = (GComponent)userClass.Assembly.CreateInstance(userClass.FullName);
				else
					g = UIObjectFactory.NewObject(item);
			}
			else
				g = UIObjectFactory.NewObject(item);

			if (g == null)
				return null;

			_constructing++;
			g.ConstructFromResource(item);
			_constructing--;
			return g;
		}

		public PackageItem GetItem(string itemId)
		{
			PackageItem pi;
			if (_itemsById.TryGetValue(itemId, out pi))
				return pi;
			else
				return null;
		}

		public PackageItem GetItemByName(string itemName)
		{
			PackageItem pi;
			if (_itemsByName.TryGetValue(itemName, out pi))
				return pi;
			else
				return null;
		}

		internal object GetItemAsset(PackageItem item)
		{
			switch (item.type)
			{
				case PackageItemType.Image:
					if (!item.decoded)
					{
						item.decoded = true;
						AtlasSprite sprite;
						if (_sprites.TryGetValue(item.id, out sprite))
							item.texture = CreateSpriteTexture(sprite);
						else
							item.texture = NTexture.Empty;
					}
					return item.texture;

				case PackageItemType.Atlas:
					if (!item.decoded)
					{
						item.decoded = true;
						string fileName = string.IsNullOrEmpty(item.file) ? (item.id + ".png") : item.file;
						string filePath = _assetNamePrefix + Path.GetFileNameWithoutExtension(fileName);
						string ext = Path.GetExtension(fileName);

						Texture2D tex;
						if (_resBundle != null)
						{
#if UNITY_5
							tex = _resBundle.LoadAsset<Texture2D>(filePath);
#else
							tex = (Texture2D)_resBundle.Load(filePath, typeof(Texture2D));
#endif
						}
						else
							tex = (Texture2D)_loadFunc(filePath, ext, typeof(Texture2D));
						if (tex == null)
						{
							Debug.LogWarning("FairyGUI: texture '" + fileName + "' not found in " + this.name);
							item.texture = NTexture.Empty;
						}
						else
						{
							if (tex.mipmapCount > 1)
								Debug.LogWarning("FairyGUI: texture '" + fileName + "' in " + this.name + " is mipmaps enabled.");
							item.texture = new NTexture(tex, (float)tex.width / item.width, (float)tex.height / item.height);
							item.texture.storedODisk = _resBundle == null;

							filePath = filePath + "!a";
							if (_resBundle != null)
							{
#if UNITY_5
								tex = _resBundle.LoadAsset<Texture2D>(filePath);
#else
								tex = (Texture2D)_resBundle.Load(filePath, typeof(Texture2D));
#endif
							}
							else
								tex = (Texture2D)_loadFunc(filePath, ext, typeof(Texture2D));
							if (tex != null)
							{
								item.texture.alphaTexture = new NTexture(tex);
								item.texture.alphaTexture.storedODisk = _resBundle == null;
							}
						}
					}
					return item.texture;

				case PackageItemType.Sound:
					if (!item.decoded)
					{
						item.decoded = true;
						string fileName = _assetNamePrefix + Path.GetFileNameWithoutExtension(item.file);
						string ext = Path.GetExtension(item.file);
						if (_resBundle != null)
						{
#if UNITY_5
							item.audioClip = _resBundle.LoadAsset<AudioClip>(fileName);
#else
							item.audioClip = (AudioClip)_resBundle.Load(fileName, typeof(AudioClip));
#endif
						}
						else
							item.audioClip = (AudioClip)_loadFunc(fileName, ext, typeof(AudioClip));
					}
					return item.audioClip;

				case PackageItemType.Font:
					if (!item.decoded)
					{
						item.decoded = true;
						LoadFont(item);
					}
					return item.bitmapFont;

				case PackageItemType.MovieClip:
					if (!item.decoded)
					{
						item.decoded = true;
						LoadMovieClip(item);
					}
					return item.frames;

				case PackageItemType.Component:
					if (!item.decoded)
					{
						item.decoded = true;
						string str = _descPack[item.id + ".xml"];
						XML xml = new XML(str);
						if (_stringsSource != null)
						{
							Dictionary<string, string> strings;
							if (_stringsSource.TryGetValue(this.id + item.id, out strings))
								TranslateComponent(xml, strings);
						}
						item.componentData = xml;
					}
					return item.componentData;

				default:
					if (!item.decoded)
					{
						item.decoded = true;
						item.binary = LoadBinary(item.file);
					}
					return item.binary;
			}
		}

		void TranslateComponent(XML xml, Dictionary<string, string> strings)
		{
			XML listNode = xml.GetNode("displayList");
			if (listNode == null)
				return;

			XMLList col = listNode.Elements();

			string ename, elementId, value;
			foreach (XML cxml in col)
			{
				ename = cxml.name;
				elementId = cxml.GetAttribute("id");
				if (cxml.HasAttribute("tooltips"))
				{
					if (strings.TryGetValue(elementId + "-tips", out value))
						cxml.SetAttribute("tooltips", value);
				}

				if (ename == "text" || ename == "richtext")
				{
					if (strings.TryGetValue(elementId, out value))
						cxml.SetAttribute("text", value);
					if (strings.TryGetValue(elementId + "-prompt", out value))
						cxml.SetAttribute("prompt", value);
				}
				else if (ename == "list")
				{
					XMLList items = cxml.Elements("item");
					int j = 0;
					foreach (XML exml in items)
					{
						if (strings.TryGetValue(elementId + "-" + j, out value))
							exml.SetAttribute("title", value);
						j++;
					}
				}
				else if (ename == "component")
				{
					XML dxml = cxml.GetNode("Button");
					if (dxml != null)
					{
						if (strings.TryGetValue(elementId, out value))
							dxml.SetAttribute("title", value);
						if (strings.TryGetValue(elementId + "-0", out value))
							dxml.SetAttribute("selectedTitle", value);
					}
					else
					{
						dxml = cxml.GetNode("Label");
						if (dxml != null)
						{
							if (strings.TryGetValue(elementId, out value))
								dxml.SetAttribute("title", value);
						}
						else
						{
							dxml = cxml.GetNode("ComboBox");
							if (dxml != null)
							{
								if (strings.TryGetValue(elementId, out value))
									dxml.SetAttribute("title", value);

								XMLList items = dxml.Elements("item");
								int j = 0;
								foreach (XML exml in items)
								{
									if (strings.TryGetValue(elementId + "-" + j, out value))
										exml.SetAttribute("title", value);
									j++;
								}
							}
						}
					}
				}
			}
		}

		NTexture CreateSpriteTexture(AtlasSprite sprite)
		{
			PackageItem atlasItem;
			if (_itemsById.TryGetValue(sprite.atlas, out atlasItem))
				return new NTexture((NTexture)GetItemAsset(atlasItem), sprite.rect);
			else
			{
				Debug.LogWarning("FairyGUI: " + sprite.atlas + " not found in " + this.name);
				return NTexture.Empty;
			}
		}

		byte[] LoadBinary(string fileName)
		{
			string ext = Path.GetExtension(fileName);
			fileName = _assetNamePrefix + Path.GetFileNameWithoutExtension(fileName);

			TextAsset ta;
			if (_resBundle != null)
			{
#if UNITY_5
				ta = _resBundle.LoadAsset<TextAsset>(fileName);
#else
				ta = (TextAsset)_resBundle.Load(fileName, typeof(TextAsset));
#endif
			}
			else
				ta = (TextAsset)_loadFunc(fileName, ext, typeof(TextAsset));
			if (ta != null)
				return ta.bytes;
			else
				return null;
		}

		string LoadString(string fileName)
		{
			byte[] data = LoadBinary(fileName);
			if (data != null)
				return Encoding.UTF8.GetString(data);
			else
				return null;
		}

		void LoadMovieClip(PackageItem item)
		{
			string str = _descPack[item.id + ".xml"];
			XML xml = new XML(str);
			string[] arr = null;

			str = xml.GetAttribute("interval");
			if (str != null)
				item.interval = float.Parse(str) / 1000f;
			item.swing = xml.GetAttributeBool("swing", false);
			str = xml.GetAttribute("repeatDelay");
			if (str != null)
				item.repeatDelay = float.Parse(str) / 1000f;
			int frameCount = xml.GetAttributeInt("frameCount");
			item.frames = new MovieClip.Frame[frameCount];

			XMLList frameNodes = xml.GetNode("frames").Elements();

			int i = 0;
			foreach (XML frameNode in frameNodes)
			{
				MovieClip.Frame frame = new MovieClip.Frame();
				arr = frameNode.GetAttributeArray("rect");
				frame.rect = new Rect(int.Parse(arr[0]), int.Parse(arr[1]), int.Parse(arr[2]), int.Parse(arr[3]));
				str = frameNode.GetAttribute("addDelay");
				if (str != null)
					frame.addDelay = float.Parse(str) / 1000f;

				AtlasSprite sprite;
				if (_sprites.TryGetValue(item.id + "_" + i, out sprite))
				{
					PackageItem atlasItem = _itemsById[sprite.atlas];
					if (atlasItem != null)
					{
						if (item.texture == null)
							item.texture = (NTexture)GetItemAsset(atlasItem);
						frame.uvRect = new Rect(sprite.rect.x / item.texture.width * item.texture.uvRect.width,
							1 - sprite.rect.yMax * item.texture.uvRect.height / item.texture.height,
							sprite.rect.width * item.texture.uvRect.width / item.texture.width,
							sprite.rect.height * item.texture.uvRect.height / item.texture.height);
					}
				}
				item.frames[i] = frame;
				i++;
			}
		}

		void LoadFont(PackageItem item)
		{
			BitmapFont font = item.bitmapFont;

			string str = _descPack[item.id + ".fnt"];
			string[] arr = str.Split('\n');
			int cnt = arr.Length;
			Dictionary<string, string> kv = new Dictionary<string, string>();
			NTexture mainTexture = null;
			Vector2 atlasOffset = new Vector2();
			bool ttf = false;
			int size = 0;
			int xadvance = 0;
			bool resizable = false;
			bool canTint = false;
			BitmapFont.BMGlyph bg = null;

			char[] splitter0 = new char[] { ' ' };
			char[] splitter1 = new char[] { '=' };

			for (int i = 0; i < cnt; i++)
			{
				str = arr[i];
				if (str.Length == 0)
					continue;

				str = str.Trim();

				string[] arr2 = str.Split(splitter0, StringSplitOptions.RemoveEmptyEntries);
				for (int j = 1; j < arr2.Length; j++)
				{
					string[] arr3 = arr2[j].Split(splitter1, StringSplitOptions.RemoveEmptyEntries);
					kv[arr3[0]] = arr3[1];
				}

				str = arr2[0];
				if (str == "char")
				{
					bg = new BitmapFont.BMGlyph();
					int bx = 0, by = 0;
					if (kv.TryGetValue("x", out str))
						bx = int.Parse(str);
					if (kv.TryGetValue("y", out str))
						by = int.Parse(str);
					if (kv.TryGetValue("xoffset", out str))
						bg.offsetX = int.Parse(str);
					if (kv.TryGetValue("yoffset", out str))
						bg.offsetY = int.Parse(str);
					if (kv.TryGetValue("width", out str))
						bg.width = int.Parse(str);
					if (kv.TryGetValue("height", out str))
						bg.height = int.Parse(str);
					if (kv.TryGetValue("xadvance", out str))
						bg.advance = int.Parse(str);
					if (kv.TryGetValue("chnl", out str))
					{
						bg.channel = int.Parse(str);
						if (bg.channel == 15)
							bg.channel = 4;
						else if (bg.channel == 1)
							bg.channel = 3;
						else if (bg.channel == 2)
							bg.channel = 2;
						else
							bg.channel = 1;
					}

					if (!ttf)
					{
						if (kv.TryGetValue("img", out str))
						{
							PackageItem charImg;
							if (_itemsById.TryGetValue(str, out charImg))
							{
								charImg.Load();
								bg.uvRect = charImg.texture.uvRect;
								if (mainTexture == null)
									mainTexture = charImg.texture.root;
								bg.width = charImg.texture.width;
								bg.height = charImg.texture.height;
							}
						}
					}
					else
					{
						Rect region = new Rect(bx + atlasOffset.x, by + atlasOffset.y, bg.width, bg.height);
						bg.uvRect = new Rect(region.x / mainTexture.width, 1 - region.yMax / mainTexture.height,
							region.width / mainTexture.width, region.height / mainTexture.height);
					}

					if (ttf)
						bg.lineHeight = size;
					else
					{
						if (bg.advance == 0)
						{
							if (xadvance == 0)
								bg.advance = bg.offsetX + bg.width;
							else
								bg.advance = xadvance;
						}

						bg.lineHeight = bg.offsetY < 0 ? bg.height : (bg.offsetY + bg.height);
						if (bg.lineHeight < size)
							bg.lineHeight = size;
					}

					int ch = int.Parse(kv["id"]);
					font.AddChar((char)ch, bg);
				}
				else if (str == "info")
				{
					if (kv.TryGetValue("face", out str))
					{
						ttf = true;
						canTint = true;

						AtlasSprite sprite;
						if (_sprites.TryGetValue(item.id, out sprite))
						{
							atlasOffset = new Vector2(sprite.rect.x, sprite.rect.y);
							PackageItem atlasItem = _itemsById[sprite.atlas];
							mainTexture = (NTexture)GetItemAsset(atlasItem);
						}
					}
					if (kv.TryGetValue("size", out str))
						size = int.Parse(str);
					if (kv.TryGetValue("resizable", out str))
						resizable = str == "true";
					if (kv.TryGetValue("colored", out str))
						canTint = str == "true";
				}
				else if (str == "common")
				{
					if (size == 0 && kv.TryGetValue("lineHeight", out str))
						size = int.Parse(str);
					if (kv.TryGetValue("xadvance", out str))
						xadvance = int.Parse(str);
				}
			}

			if (size == 0 && bg != null)
				size = bg.height;

			font.hasChannel = ttf;
			font.canTint = canTint;
			font.size = size;
			font.resizable = resizable;
			font.mainTexture = mainTexture;
			if (!ttf)
				font.shader = ShaderConfig.imageShader;
		}
	}
}
