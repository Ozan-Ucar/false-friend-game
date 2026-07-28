using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a simple vertical two-colour gradient to any UI Graphic
/// (Image / TMP). Gives flat buttons a beveled, "wooden" look.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class UIGradient : BaseMeshEffect
{
    public Color top    = new Color(0.55f, 0.36f, 0.18f);
    public Color bottom = new Color(0.26f, 0.15f, 0.06f);

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        var verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < verts.Count; i++)
        {
            float y = verts[i].position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        float h = Mathf.Max(0.0001f, maxY - minY);

        for (int i = 0; i < verts.Count; i++)
        {
            var v = verts[i];
            float t = (v.position.y - minY) / h;
            v.color = Color.Lerp(bottom, top, t) * v.color;
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}
