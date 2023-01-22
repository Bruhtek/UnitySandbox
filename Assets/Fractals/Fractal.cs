using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour {
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	struct UpdateFractalLevelJob : IJobFor {
		public float spinAngleDelta;
		public float scale;

		[ReadOnly]
		public NativeArray<FractalPart> parents;

		public NativeArray<FractalPart> parts;

		[WriteOnly]
		public NativeArray<float3x4> matrices;

		public void Execute(int i) {
			FractalPart parent = parents[i / 5];
			FractalPart part = parts[i];
			part.spinAngle += spinAngleDelta;
			part.worldRotation = mul(parent.worldRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
			part.worldPosition =
				parent.worldPosition + mul(parent.worldRotation,
					(1.5f * scale * part.direction));
			parts[i] = part;
			float3x3 r = float3x3(part.worldRotation) * scale;
			matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
		}
	}

	struct FractalPart {
		public float3 direction, worldPosition;
		public quaternion rotation, worldRotation;
		public float spinAngle;
	}

	private NativeArray<FractalPart>[] _parts;
	private NativeArray<float3x4>[] _matrices;

	private ComputeBuffer[] _matricesBuffers;

	private static readonly int matricesId = Shader.PropertyToID("_Matrices");
	private static MaterialPropertyBlock _propertyBlock;

	[SerializeField]
	private Mesh mesh;

	[SerializeField]
	private Material material;

	[SerializeField, Range(1, 8)]
	private int depth = 4;

	private static float3[] directions = {
		up(),
		right(),
		left(),
		forward(),
		back()
	};

	private static quaternion[] rotations = {
		quaternion.identity,
		quaternion.RotateZ(-0.5f * PI),
		quaternion.RotateZ(0.5f * PI),
		quaternion.RotateX(0.5f * PI),
		quaternion.RotateX(-0.5f * PI)
	};

	private void OnEnable() {
		_parts = new NativeArray<FractalPart>[depth];
		_matrices = new NativeArray<float3x4>[depth];
		_matricesBuffers = new ComputeBuffer[depth];
		int stride = 12 * 4;
		for (int i = 0, length = 1; i < _parts.Length; i++, length *= 5) {
			_parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
			_matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
			_matricesBuffers[i] = new ComputeBuffer(length, stride);
		}

		FractalPart rootPart = CreatePart(0);

		_parts[0][0] = rootPart;
		for (int li = 1; li < _parts.Length; li++) {
			NativeArray<FractalPart> levelParts = _parts[li];
			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
				for (int ci = 0; ci < 5; ci++) {
					levelParts[fpi + ci] = CreatePart(ci);
				}
			}

			_parts[li] = levelParts;
		}

		_propertyBlock ??= new MaterialPropertyBlock();
	}

	private void OnDisable() {
		for (int i = 0; i < _matricesBuffers.Length; i++) {
			_matricesBuffers[i].Release();
			_parts[i].Dispose();
			_matrices[i].Dispose();
		}

		_parts = null;
		_matrices = null;
		_matricesBuffers = null;
	}

	private void OnValidate() {
		if (_parts != null && enabled) {
			OnDisable();
			OnEnable();
		}
	}

	private void Update() {
		float spinAngleDelta = 0.125f * PI * Time.deltaTime;

		FractalPart rootPart = _parts[0][0];
		rootPart.spinAngle += spinAngleDelta;
		rootPart.worldRotation =
			mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
		rootPart.worldPosition = transform.position;
		_parts[0][0] = rootPart;
		float objectScale = transform.lossyScale.x;
		float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
		_matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

		JobHandle jobHandle = default;
		float scale = objectScale;
		for (int li = 1; li < _parts.Length; li++) {
			scale *= 0.5f;
			jobHandle = new UpdateFractalLevelJob {
				spinAngleDelta = spinAngleDelta,
				scale = scale,
				parents = _parts[li - 1],
				parts = _parts[li],
				matrices = _matrices[li]
			}.ScheduleParallel(_parts[li].Length, 5, jobHandle);
		}

		jobHandle.Complete();

		var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
		for (int i = 0; i < _matricesBuffers.Length; i++) {
			ComputeBuffer buffer = _matricesBuffers[i];
			buffer.SetData(_matrices[i]);
			_propertyBlock.SetBuffer(matricesId, buffer);
			Graphics.DrawMeshInstancedProcedural(
				mesh, 0, material, bounds, buffer.count, _propertyBlock
			);
		}
	}

	private FractalPart CreatePart(int childIndex) => new FractalPart {
		direction = directions[childIndex],
		rotation = rotations[childIndex],
	};
}