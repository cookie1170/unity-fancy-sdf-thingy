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
	
		private static readonly int ScreenTextureID = Shader.PropertyToID("screenTexture");
		private static readonly int ResolutionID = Shader.PropertyToID("resolution");
		private static readonly int CircleBufferID = Shader.PropertyToID("circleBuffer");
		[SerializeField] private ComputeShader shader;
		private RenderTexture _rt;
		private Camera _cam;
		private int _kernelIndex;
	
		struct Circle
		{
			[UsedImplicitly] public Color Color;
			[UsedImplicitly] public Vector2 Position;
			[UsedImplicitly] public float Radius;
			[UsedImplicitly] public float BlendingFactor;
		}
		private void Awake()
		{
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
		}

		private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
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
			Circle[] circleStructs = new Circle[Circles.Count];
			for (int i = 0; i < Circles.Count; i++)
			{
				Circle circleStruct = new();
				CircleObj circle = Circles[i];
				circleStruct.Position = _cam.WorldToScreenPoint(circle.transform.position);
				circleStruct.Radius = ScaleFloat(circle.radius);
				circleStruct.BlendingFactor = ScaleFloat(circle.blendingFactor);
				circleStruct.Color = circle.color;
				circleStructs[i] = circleStruct;
			}
			ComputeBuffer circleBuffer = new(circleStructs.Length, sizeof(float) * 8);
			circleBuffer.SetData(circleStructs);
			shader.SetVector(ResolutionID, new Vector2(Screen.width, Screen.height));
			shader.SetTexture(_kernelIndex, ScreenTextureID, _rt);
			shader.SetBuffer(_kernelIndex, CircleBufferID, circleBuffer);
			shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);
			Graphics.Blit(_rt, dest: (RenderTexture)null);
			circleBuffer.Release();
		}

		private float ScaleFloat(float f)
		{
			return (_cam.WorldToScreenPoint(new Vector3(f, 0, 0)) - _cam.WorldToScreenPoint(Vector3.zero)).x;
		}

		private void OnDestroy()
		{
			RenderPipelineManager.endContextRendering -= OnEndContextRendering;
			RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
		}
	}
}
