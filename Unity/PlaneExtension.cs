using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PlaneExtension
{
	/// <summary>
    /// 중심점을 포함한 Plane
    /// </summary>
	public struct PlaneWithPoint
	{
		public Plane plane;

		public Vector3 origin;

		public Vector3 normal { get => plane.normal; }

		public PlaneWithPoint(Vector3 _origin, Vector3 _normal)
        {
			plane = new Plane(_normal, _origin);
			origin = _origin;
        }

		public PlaneWithPoint(Vector3 p1, Vector3 p2, Vector3 p3)
        {
			plane = new Plane(p1, p2, p3);
			origin = (p1 + p2 + p3) / 3.0f;
        }

		public float SignedDistanceTo(Vector3 point)
        {
			return Vector3.Dot(point - origin, normal);
		}
	}

	/// <summary>
    /// 평면 교차점 정보
    /// </summary>
	public struct PlaneIntersectionInfo
	{
		public Vector3 point;
		public bool isFacingIntersection;
	}

	/// <summary>
    /// plane 내 point와 가장 가까운 포인트 리턴
    /// </summary>
    /// <param name="point"></param>
    /// <param name="plane"></param>
    /// <returns></returns>
	public static Vector3 ProjectedPointOnPlane(Vector3 point, PlaneWithPoint plane)
	{
		return point - SignedDistanceToPlane(point, plane.origin, plane.normal) * plane.normal;
	}

	/// <summary>
    /// 라인과 plane이 교차할 경우 true 리턴 및 교차점 정보 출력
    /// </summary>
    /// <param name="start">라인 시작 시점</param>
    /// <param name="end">라인 끝 지점</param>
    /// <param name="plane">Plane</param>
    /// <param name="resultPoint">평면 교차점 정보</param>
    /// <returns></returns>
	public static bool LineCastOnPlane(Vector3 start, Vector3 end, PlaneWithPoint plane, out PlaneIntersectionInfo resultPoint)
	{
		var startHeight = SignedDistanceToPlane(start, plane.origin, plane.normal);
		var endHeight = SignedDistanceToPlane(end, plane.origin, plane.normal);
		resultPoint = new PlaneIntersectionInfo();
		if (Mathf.Sign(startHeight) != Mathf.Sign(endHeight)) //plane을 투과했을 경우
		{
			var startOnPlane = ProjectedPointOnPlane(start, plane);
			var endOnPlane = ProjectedPointOnPlane(end, plane);
			resultPoint.point = Vector3.Lerp(startOnPlane, endOnPlane, Mathf.Abs(startHeight) / (Mathf.Abs(startHeight) + Mathf.Abs(endHeight)));
			resultPoint.isFacingIntersection = startHeight > 0;
			return true;
		}
		else
			return false;
	}

	public static float SignedDistanceToPlane(Vector3 point, Vector3 origin, Vector3 normal)
	{
		return Vector3.Dot(point - origin, normal);
	}
}
