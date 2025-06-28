using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeRenderer : MonoBehaviour
{
	private static readonly int ScreenTextureID = Shader.PropertyToID("screenTexture");
	private static readonly int ResolutionID = Shader.PropertyToID("resolution");
	private static readonly int CircleBufferID = Shader.PropertyToID("circleBuffer");
	[SerializeField] private List<Circle> circles = new();
	[SerializeField] private ComputeShader shader;
	private RenderTexture _rt;
	private Camera _cam;
	private int _kernelIndex;

	[Serializable]
	struct Circle
	{
		public Color color;
		public Vector2 position;
		public float radius;
		public float blendingFactor;
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
		}
	}

	private int GetNextMultipleOfEight(int i)
	{
		return Mathf.CeilToInt(i / 8f) * 8;
	}

	private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
	{
		ComputeBuffer circleBuffer = new(circles.Count, sizeof(float) * 8);
		circleBuffer.SetData(circles);
		shader.SetVector(ResolutionID, new Vector2(Screen.width, Screen.height));
		shader.SetTexture(_kernelIndex, ScreenTextureID, _rt);
		shader.SetBuffer(_kernelIndex, CircleBufferID, circleBuffer);
		shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);
		Graphics.Blit(_rt, dest: (RenderTexture)null);
		circleBuffer.Release();
	}

	private void OnDestroy()
	{
		RenderPipelineManager.endContextRendering -= OnEndContextRendering;
		RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
	}
}
