using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
	private const int MaxResolution = 1000;

	[SerializeField, Range(10, MaxResolution)]
	private int resolution = 10;

	[SerializeField]
	private FunctionLibrary.FunctionName functionName;

	[SerializeField, Min(0f)]
	private float functionDuration = 1f, transitionDuration = 1f;

	private bool _isTransitioning = false;
	private FunctionLibrary.FunctionName _transitionFunction;

	private float _duration;

	private ComputeBuffer _positionsBuffer;

	[SerializeField]
	private ComputeShader computeShader;

	[SerializeField]
	private Material material;

	[SerializeField]
	private Mesh mesh;

	private static readonly int
		positionsId = Shader.PropertyToID("_Positions"),
		resolutionId = Shader.PropertyToID("_Resolution"),
		stepId = Shader.PropertyToID("_Step"),
		timeId = Shader.PropertyToID("_Time"),
		transitionProgressId = Shader.PropertyToID("_TransitionProgress");


	private void UpdateFunctionOnGPU()
	{
		var kernelIndex =
			(int)this.functionName +
			(int)(
				this._isTransitioning
					? this._transitionFunction
					: this.functionName
			)
			* FunctionLibrary.Functions.Length;

		float step = 2f / this.resolution;
		this.computeShader.SetInt(resolutionId, this.resolution);
		this.computeShader.SetFloat(stepId, step);
		this.computeShader.SetFloat(timeId, Time.time);
		if (this._isTransitioning)
		{
			this.computeShader.SetFloat(
				transitionProgressId,
				Mathf.SmoothStep(
					0f,
					1f,
					this._duration / this.transitionDuration
				)
			);
		}

		this.computeShader.SetBuffer(kernelIndex, positionsId, this._positionsBuffer);
		int groups = Mathf.CeilToInt(this.resolution / 8f);
		this.computeShader.Dispatch(kernelIndex, groups, groups, 1);

		this.material.SetBuffer(positionsId, this._positionsBuffer);
		this.material.SetFloat(stepId, step);

		var bounds = new Bounds(
			Vector3.zero,
			Vector3.one * (2f + 2f / this.resolution)
		);
		Graphics.DrawMeshInstancedProcedural(
			this.mesh,
			0,
			this.material,
			bounds,
			this.resolution * this.resolution
		);
	}

	private void OnEnable()
	{
		this._positionsBuffer = new ComputeBuffer(
			MaxResolution * MaxResolution,
			3 * 4
		);
	}

	private void OnDisable()
	{
		this._positionsBuffer.Release();
		this._positionsBuffer = null;
	}

	private void Update()
	{
		this._duration += Time.deltaTime;

		if (this._duration >= this.transitionDuration && this._isTransitioning)
		{
			this._duration -= this.transitionDuration;
			this._isTransitioning = false;
		}

		if (this._duration >= this.functionDuration)
		{
			this._duration -= this.functionDuration;
			this._transitionFunction = this.functionName;
			this.functionName =
				(FunctionLibrary.FunctionName)UnityEngine.Random.Range(0, FunctionLibrary.Functions.Length);
			this._isTransitioning = true;
		}

		UpdateFunctionOnGPU();
	}
}