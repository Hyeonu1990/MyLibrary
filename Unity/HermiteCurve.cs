using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HermiteCurve
{
	/// <summary>
	/// Hermit 커브 표현을 위한 구조체
	/// point : 위치
	/// distance : 시작지점부터의 길이
	/// </summary>
	public struct HermitCurveData
	{
		public Vector3 point;
		public float distance;
	}

	/// <summary>
	/// m0, m1의 경우 각 포인트 transform forward 방향을 사용하며 m(n)_strength값으로 강도 조절
	/// https://en.wikibooks.org/wiki/Cg_Programming/Unity/Hermite_Curves
	/// </summary>
	/// <param name="lineRenderer">LineRenderer</param>
	/// <param name="p0">point0 transform</param>
	/// <param name="p1">point1 transform</param>
	/// <param name="m0_strength">p0 forward 방향 강도 조절</param>
	/// <param name="m1_strength">p1 forward 방향 강도 조절</param>
	/// <param name="_count">커브 표현을 위한 두 지점 사이 포인트 갯수</param>
	/// <param name="bones">두 지점이 연결된 본타입일 경우 사이 본 위치값 수정 및 _count 갱신</param>
	/// <returns></returns>
	public static List<HermitCurveData> HermitCurvePos(LineRenderer lineRenderer, Transform p0, Transform p1, float m0_strength, float m1_strength, int _count, List<Transform> bones = null)
    {
		if (bones != null && _count < bones.Count)
			_count = bones.Count;

		if (lineRenderer != null)
			lineRenderer.positionCount = _count;

		Vector3 m0 = p0.forward * m0_strength;
		Vector3 m1 = p1.forward * m1_strength;
		List<HermitCurveData> hermite_points = new List<HermitCurveData>();
		float total_distance = 0;

		//위치 포인트
		float t = 0;
		Vector3 position;
		for (int i = 0; i < _count; i++)
		{
			t = i / (_count - 1.0f);
			position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0.position
						+ (t * t * t - 2.0f * t * t + t) * m0
						+ (-2.0f * t * t * t + 3.0f * t * t) * p1.position
						+ (t * t * t - t * t) * m1;
			if (i != 0)
			{
				total_distance += (position - hermite_points.LastOrDefault().point).magnitude;
			}
			hermite_points.Add(new HermitCurveData() { point = position, distance = total_distance });
			if (lineRenderer != null)
			{
				lineRenderer.SetPosition(i, position);
			}
		}

		if (bones != null)
		{
			int bone_num = 0;
			//거리비율로 포인트 위치 사용
			for (int i = 0; i < hermite_points.Count; i++)
			{
				if (bone_num >= bones.Count)
					break;
				var hermite_point = hermite_points[i];
				if (i == 0 || hermite_point.distance > total_distance * bone_num / _count)
				{
					bones[bone_num].position = hermite_point.point;
					if (i != hermite_points.Count - 1)
						bones[bone_num].forward = hermite_points[i + 1].point - hermite_point.point;
					else
						bones[bone_num].forward = bones[bone_num - 1].forward;
					bone_num++;
				}
			}
		}
		return hermite_points;
	}
}
