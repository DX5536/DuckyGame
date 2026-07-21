using System;
using TMPro;
using UnityEngine;
using System.Reflection;
#if UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore;
using UnityEditor; 
#endif


namespace OnlineFonts2TextMeshPro.Core
{
    public static class OF2TMP_FontAssetCreator
    {
#if UNITY_6000_0_OR_NEWER
        //For Unity 6 and above
        public static void FillAtlasAndMaterial(Font font, string relativeSourceFontFilePath, TMP_FontAsset fontAsset, string assetName)
        {
            Type fontAssetType = typeof(TMP_FontAsset);

            UnityEngine.Object target = (UnityEngine.Object)font;
            // Load Font Face
            if (FontEngine.LoadFontFace(font, 90) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + font.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.", font);
                return;
            }

            var versionField = fontAssetType.GetField("m_Version", BindingFlags.NonPublic | BindingFlags.Instance);
            versionField.SetValue(fontAsset, "1.1.0");
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            var sourceFontFileField = fontAssetType.GetField("m_SourceFontFile", BindingFlags.NonPublic | BindingFlags.Instance);
            sourceFontFileField.SetValue(fontAsset, font);
            var sourceFontFileGUIDField = fontAssetType.GetField("m_SourceFontFileGUID", BindingFlags.NonPublic | BindingFlags.Instance);
            sourceFontFileGUIDField.SetValue(fontAsset, AssetDatabase.AssetPathToGUID(relativeSourceFontFilePath));
            var sourceFontFile_EditorRefField = fontAssetType.GetField("m_SourceFontFile_EditorRef", BindingFlags.NonPublic | BindingFlags.Instance);
            sourceFontFile_EditorRefField.SetValue(fontAsset, font);

            fontAsset.atlasPopulationMode = TMPro.AtlasPopulationMode.Dynamic;
#if UNITY_6000_0_OR_NEWER
            var clearDynamicDataOnBuildField = fontAssetType.GetField("m_ClearDynamicDataOnBuild", BindingFlags.NonPublic | BindingFlags.Instance);
            clearDynamicDataOnBuildField.SetValue(fontAsset, TMP_Settings.clearDynamicDataOnBuild);
#endif
            // Get all font features
            //fontAsset.ImportFontFeatures();

            // Default atlas resolution is 1024 x 1024.
            fontAsset.atlasTextures = new Texture2D[1];

            int atlasWidth = 1024;
            var atlasWidthField = fontAssetType.GetField("m_AtlasWidth", BindingFlags.NonPublic | BindingFlags.Instance);
            atlasWidthField.SetValue(fontAsset, atlasWidth);

            int atlasHeight = 1024;
            var atlasHeightField = fontAssetType.GetField("m_AtlasHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            atlasHeightField.SetValue(fontAsset, atlasHeight);

            int atlasPadding = 9;
            var atlasPaddingField = fontAssetType.GetField("m_AtlasPadding", BindingFlags.NonPublic | BindingFlags.Instance);
            atlasPaddingField.SetValue(fontAsset, atlasPadding);

            Texture2D texture;
            Material mat;
            Shader shader;
            int packingModifier;

            var atlasRenderModeField = fontAssetType.GetField("m_AtlasRenderMode", BindingFlags.NonPublic | BindingFlags.Instance);
            atlasRenderModeField.SetValue(fontAsset, GlyphRenderMode.SDFAA);

            texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
            shader = Shader.Find("TextMeshPro/Distance Field");
            packingModifier = 1;
            mat = new Material(shader);

            mat.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);
            mat.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
            mat.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);


            texture.name = assetName + " Atlas";
            mat.name = texture.name + " Material";

            fontAsset.atlasTextures[0] = texture;
            AssetDatabase.AddObjectToAsset(texture, fontAsset);

            var freeGlyphRectsField = fontAssetType.GetField("m_FreeGlyphRects", BindingFlags.NonPublic | BindingFlags.Instance);
            freeGlyphRectsField.SetValue(fontAsset, new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) });

            var usedGlyphRectsField = fontAssetType.GetField("m_UsedGlyphRects", BindingFlags.NonPublic | BindingFlags.Instance);
            usedGlyphRectsField.SetValue(fontAsset, new List<GlyphRect>());

            mat.SetTexture(ShaderUtilities.ID_MainTex, texture);
            mat.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            mat.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

            fontAsset.material = mat;
            AssetDatabase.AddObjectToAsset(mat, fontAsset);

            // Add Font Asset Creation Settings
            Type[] types = new Type[10];
            types[0] = typeof(string);
            types[1] = typeof(int);
            types[2] = typeof(int);
            types[3] = typeof(int);
            types[4] = typeof(int);
            types[5] = typeof(int);
            types[6] = typeof(int);
            types[7] = typeof(int);
            types[8] = typeof(string);
            types[9] = typeof(int);

            ConstructorInfo settingsConstructorInfo = (ConstructorInfo)typeof(FontAssetCreationSettings).GetConstructor(
                  BindingFlags.NonPublic | BindingFlags.Instance,
                  null, types, null);

            FontAssetCreationSettings settings = (FontAssetCreationSettings)settingsConstructorInfo.Invoke(parameters: new object[] { AssetDatabase.AssetPathToGUID(relativeSourceFontFilePath), (int)fontAsset.faceInfo.pointSize, 0, atlasPadding, 0, 1024, 1024, 7, string.Empty, (int)GlyphRenderMode.SDFAA });

            fontAsset.creationSettings = settings;

            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
        }
#else
        //Only for Unity 2021 & 2022
        public static void CreateFontAsset(Font font)
        {
            Type fontAssetCreationMenuType = typeof(TMP_FontAsset_CreationMenu);

            MethodInfo createFontAssetMethodInfo = fontAssetCreationMenuType.GetMethod("CreateFontAssetFromSelectedObject", BindingFlags.NonPublic | BindingFlags.Static);
            UnityEngine.Object target = (UnityEngine.Object)font;

            createFontAssetMethodInfo.Invoke(null,  new object[] { target });
        }
#endif
    }
}
