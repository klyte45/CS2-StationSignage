using UnityEngine;
using Transform = Game.Objects.Transform;

namespace StationSignage.Models;

 public class OwnerSortData 
 {
    public Vector3 Position { get; private set; }
    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }

    public OwnerSortData(Transform owner)
    {
        Position = owner.m_Position;
        Quaternion rot = owner.m_Rotation;

        // Optimized quaternion to vector rotation
        float x = rot.x * 2f, y = rot.y * 2f, z = rot.z * 2f;
        float xx = rot.x * x, yy = rot.y * y, zz = rot.z * z;
        float xy = rot.x * y, xz = rot.x * z, yz = rot.y * z;
        float wx = rot.w * x, wy = rot.w * y, wz = rot.w * z;

        Forward = new Vector3(xz + wy, yz - wx, 1f - (xx + yy));
        Right = new Vector3(1f - (yy + zz), xy + wz, xz - wy);
        Up = new Vector3(xy - wz, 1f - (xx + zz), yz + wx);
    }

    public int Compare(Vector3 aPos, Vector3 bPos)
    {
        Vector3 aRel = aPos - Position;
        Vector3 bRel = bPos - Position;

        // Manual dot products for maximum performance
        float aForward = aRel.x * Forward.x + aRel.y * Forward.y + aRel.z * Forward.z;
        float bForward = bRel.x * Forward.x + bRel.y * Forward.y + bRel.z * Forward.z;
        int compare = bForward.CompareTo(aForward); // Descending
        if (compare != 0) return compare;

        float aRight = aRel.x * Right.x + aRel.y * Right.y + aRel.z * Right.z;
        float bRight = bRel.x * Right.x + bRel.y * Right.y + bRel.z * Right.z;
        compare = aRight.CompareTo(bRight); // Ascending
        if (compare != 0) return compare;

        float aUp = aRel.x * Up.x + aRel.y * Up.y + aRel.z * Up.z;
        float bUp = bRel.x * Up.x + bRel.y * Up.y + bRel.z * Up.z;
        return aUp.CompareTo(bUp); // Ascending
    }
}