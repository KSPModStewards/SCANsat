using System;
using UnityEditor;
using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
    [CustomEditor(typeof(EdgeDetectionColor))]
    class EdgeDetectionColorsEditor : Editor
    {
        SerializedObject serObj;

        SerializedProperty rawimage;
        SerializedProperty sensitivityDepth;
        SerializedProperty sensitivityNormals;
        
        SerializedProperty edgesOnly;
        SerializedProperty edgesOnlyBgColor;
        
        SerializedProperty sampleDist;

        SerializedProperty edgesColor;


        void OnEnable()
        {
            serObj = new SerializedObject(target);
            
            rawimage = serObj.FindProperty("m_Texture");

            sensitivityDepth = serObj.FindProperty("sensitivityDepth");
            sensitivityNormals = serObj.FindProperty("sensitivityNormals");
            
            edgesOnly = serObj.FindProperty("edgesOnly");
            edgesOnlyBgColor = serObj.FindProperty("edgesOnlyBgColor");
            edgesColor = serObj.FindProperty("edgesColor");
            sampleDist = serObj.FindProperty("sampleDist");
        }


        public override void OnInspectorGUI()
        {
            serObj.Update();

            EditorGUILayout.PropertyField(sensitivityDepth, new GUIContent(" Depth Sensitivity"));
            EditorGUILayout.PropertyField(sensitivityNormals, new GUIContent(" Normals Sensitivity"));

            EditorGUILayout.PropertyField(sampleDist, new GUIContent(" Sample Distance"));

            EditorGUILayout.Separator();

            GUILayout.Label("Background Options");
            edgesOnly.floatValue = EditorGUILayout.Slider(" Edges only", edgesOnly.floatValue, 0.0f, 1.0f);
            EditorGUILayout.PropertyField(edgesOnlyBgColor, new GUIContent("Bg Color"));
            EditorGUILayout.PropertyField(edgesColor, new GUIContent(" Edge Color"));
            EditorGUILayout.PropertyField(rawimage, new GUIContent(" Raw Image"));

            serObj.ApplyModifiedProperties();
        }
    }
}