using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ComputeRenderer : MonoBehaviour
{
	private static readonly int Result = Shader.PropertyToID("Result");
	[SerializeField] private ComputeShader shader;
	private Camera _cam;
	[SerializeField] private RenderTexture _rt;
	private int _kernelIndex;

	private void Awake()
	{
		_cam = GetComponent<Camera>();
		_cam.targetTexture = _rt;
		RenderPipelineManager.endContextRendering += OnEndContextRendering;
		RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
		_kernelIndex = shader.FindKernel("CSMain");
	}

	private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
	{
		_rt.Release();
		_rt = new(Screen.width, Screen.height, 24)
		{
			enableRandomWrite = true
		};
	}

	private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
	{
		shader.SetTexture(_kernelIndex, Result, _rt);
		shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);
		Graphics.Blit(_rt, dest: (RenderTexture)null);
	}

	private void OnDestroy()
	{
		RenderPipelineManager.endContextRendering -= OnEndContextRendering;
		RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
	}
}
