using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Objects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering
{
	public class ComputeRenderer : MonoBehaviour
	{
		public static readonly List<CircleObj> Circles = new();
		public static readonly List<BoxObj> Boxes = new();
		public static readonly List<TriangleObj> Triangles = new();
		public static Action OnShapesChanged;
	
		private static readonly int ScreenTextureID = Shader.PropertyToID("screenTexture");
		private static readonly int ResolutionID = Shader.PropertyToID("resolution");
		private static readonly int CircleBufferID = Shader.PropertyToID("circleBuffer");
		private static readonly int BoxBufferID = Shader.PropertyToID("boxBuffer");
		
		private static readonly int NoiseTextureID = Shader.PropertyToID("noiseTexture");
		private static readonly int NoiseAmountID = Shader.PropertyToID("noiseAmount");
		private static readonly int TimeID = Shader.PropertyToID("time");
		private static readonly int TriangleBufferID = Shader.PropertyToID("triangleBuffer");

		[SerializeField] private ComputeShader shader;
		
		[SerializeField] private Texture2D noiseTexture;
		[SerializeField] private float noiseAmount;
		[SerializeField] private float scrollSpeed;

		[SerializeField] private List<GameObject> shapePrefabs;
		
		private Circle[] _circleStructs;
		private Box[] _boxStructs;
		private Triangle[] _triangleStructs;
		
		private RenderTexture _rt;
		private Camera _cam;
		private int _kernelIndex;

		private bool _hasBeenRenderedThisFrame;

		struct Circle
		{
			[UsedImplicitly] public Color Color;
			[UsedImplicitly] public Vector2 Position;
			[UsedImplicitly] public float Radius;
			[UsedImplicitly] public float BlendingFactor;
		}
		
		struct Box
		{
			[UsedImplicitly] public Color Color;
			[UsedImplicitly] public Vector2 Position;
			[UsedImplicitly] public Vector2 Dimensions;
			[UsedImplicitly] public float BlendingFactor;
		}
		
		struct Triangle
		{
			[UsedImplicitly] public Color Color;
			[UsedImplicitly] public Vector2 Position;
			[UsedImplicitly] public float Radius;
			[UsedImplicitly] public float BlendingFactor;
		}
		
		private void Awake()
		{
			OnShapesChanged += RegenerateShapes;
			_cam = GetComponent<Camera>();
			int width = GetNextMultipleOfEight(Screen.width);
			int height = GetNextMultipleOfEight(Screen.height);
			_rt = new(width, height, 24)
			{
				enableRandomWrite = true
			};
			_cam.targetTexture = _rt;
			RenderPipelineManager.endContextRendering += OnEndContextRendering;
			RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
			_kernelIndex = shader.FindKernel("CSMain");
			
			// needed as to not create empty compute buffers
			foreach (GameObject prefab in shapePrefabs)
				Instantiate(prefab, new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue), Quaternion.identity);
		}

		private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
			_hasBeenRenderedThisFrame = false;
			int width = GetNextMultipleOfEight(Screen.width);
			int height = GetNextMultipleOfEight(Screen.height);
			if (_rt.width != width || _rt.height != height)
			{
				_rt?.Release();
				_rt = new(width, height, 24)
				{
					enableRandomWrite = true
				};
				_cam.targetTexture = _rt;
				Debug.Log("Size of render texture does not match size of screen, creating new render texture");
			}
		}

		private int GetNextMultipleOfEight(int i)
		{
			return Mathf.CeilToInt(i / 8f) * 8;
		}

		private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
			if (_hasBeenRenderedThisFrame) return;
			_hasBeenRenderedThisFrame = true;
			UpdateStructs();

			ComputeBuffer circleBuffer = new(_circleStructs.Length, sizeof(float) * 8);
			circleBuffer.SetData(_circleStructs);
			shader.SetBuffer(_kernelIndex, CircleBufferID, circleBuffer);
		
			ComputeBuffer boxBuffer = new(_boxStructs.Length, sizeof(float) * 9);
			boxBuffer.SetData(_boxStructs);
			shader.SetBuffer(_kernelIndex, BoxBufferID, boxBuffer);
			
			ComputeBuffer triangleBuffer = new(_triangleStructs.Length, sizeof(float) * 8);
			triangleBuffer.SetData(_triangleStructs);
			shader.SetBuffer(_kernelIndex, TriangleBufferID, triangleBuffer);

			shader.SetTexture(_kernelIndex, NoiseTextureID, noiseTexture);
			shader.SetFloat(NoiseAmountID, ScaleValue(noiseAmount));
			shader.SetFloat(TimeID, Time.time * ScaleValue(scrollSpeed));
			
			shader.SetVector(ResolutionID, new Vector2(Screen.width, Screen.height));
			shader.SetTexture(_kernelIndex, ScreenTextureID, _rt);
			shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);
			
			Graphics.Blit(_rt, dest: (RenderTexture)null);
			
			circleBuffer.Release();
			boxBuffer.Release();
			triangleBuffer.Release();
		}

		private float ScaleValue(float f)
		{
			return (_cam.WorldToScreenPoint(new Vector3(f, 0, 0)) - _cam.WorldToScreenPoint(Vector3.zero)).x;
		}
		
		private Vector2 ScaleValue(Vector2 v)
		{
			return new Vector2(ScaleValue(v.x), ScaleValue(v.y));
		}
		
		private void UpdateStructs()
		{
			for (int i = 0; i < _circleStructs.Length; i++)
			{
				var shapeObj = Circles[i];
				_circleStructs[i].Position = _cam.WorldToScreenPoint(shapeObj.transform.position);
				_circleStructs[i].Radius = ScaleValue(shapeObj.radius);
				_circleStructs[i].BlendingFactor = ScaleValue(shapeObj.blendingFactor);
				_circleStructs[i].Color = shapeObj.color;
			}

			for (int i = 0; i < _boxStructs.Length; i++)
			{
				var shapeObj = Boxes[i];
				_boxStructs[i].Position = _cam.WorldToScreenPoint(shapeObj.transform.position);
				_boxStructs[i].Dimensions = ScaleValue(shapeObj.dimensions) / 2;
				_boxStructs[i].BlendingFactor = ScaleValue(shapeObj.blendingFactor);
				_boxStructs[i].Color = shapeObj.color;
			}
			
			for (int i = 0; i < _triangleStructs.Length; i++)
			{
				var shapeObj = Triangles[i];
				_triangleStructs[i].Position = _cam.WorldToScreenPoint(shapeObj.transform.position);
				_triangleStructs[i].Radius = ScaleValue(shapeObj.radius);
				_triangleStructs[i].BlendingFactor = ScaleValue(shapeObj.blendingFactor);
				_triangleStructs[i].Color = shapeObj.color;
			}
		}
		
		private void RegenerateShapes()
		{
			_circleStructs = new Circle[Circles.Count];
			for (int i = 0; i < Circles.Count; i++)
			{
				Circle shapeStruct = new();
				_circleStructs[i] = shapeStruct;
			}

			_boxStructs = new Box[Boxes.Count];
			for (int i = 0; i < Boxes.Count; i++)
			{
				Box shapeStruct = new();
				_boxStructs[i] = shapeStruct;
			}
			
			_triangleStructs = new Triangle[Triangles.Count];
			for (int i = 0; i < Boxes.Count; i++)
			{
				Triangle shapeStruct = new();
				_triangleStructs[i] = shapeStruct;
			}
			
			UpdateStructs();
		}
		
		private void OnDestroy()
		{
			RenderPipelineManager.endContextRendering -= OnEndContextRendering;
			RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
			OnShapesChanged -= RegenerateShapes;
		}
	}
}
