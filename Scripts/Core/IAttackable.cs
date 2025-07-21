using UnityEngine;

/// <summary>
/// Interface cho c�c ??i t??ng c� th? b? t?n c�ng
/// D�ng ?? nh?n di?n enemy cho cursor system
/// </summary>
public interface IAttackable
{
    /// <summary>
    /// Ki?m tra xem ??i t??ng c� th? b? t?n c�ng kh�ng
    /// </summary>
    /// <returns>True n?u c� th? t?n c�ng</returns>
    bool CanBeAttacked();
    
    /// <summary>
    /// L?y position c?a ??i t??ng
    /// </summary>
    /// <returns>World position</returns>
    Vector2 GetPosition();
    
    /// <summary>
    /// L?y t�n c?a ??i t??ng
    /// </summary>
    /// <returns>T�n ??i t??ng</returns>
    string GetName();
}