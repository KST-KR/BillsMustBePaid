using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [BILL-03-01] 청구서 배경 텍스처에 얕은 곡면 효과 적용
/// </summary>
public class BillCurvedRawImage : RawImage
{
    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        //Rect uv = uvRect;
        //Color vertexColor = color;

        //vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMin, 0.0f), vertexColor, new Vector2(uv.xMin, uv.yMin)); // 0: 좌하
        //vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax, 0.0f), vertexColor, new Vector2(uv.xMin, uv.yMax)); // 1: 좌상
        //vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax, 0.0f), vertexColor, new Vector2(uv.xMax, uv.yMax)); // 2: 우상
        //vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMin, 0.0f), vertexColor, new Vector2(uv.xMax, uv.yMin)); // 3: 우하

        //vertexHelper.AddTriangle(0, 1, 2);
        //vertexHelper.AddTriangle(0, 2, 3);

        int hSegments = 24;
        int vSegments = 4;
        var curveDepth = 20.0f;

        Rect uv = uvRect;
        Color vertexColor = color;

        for (int y = 0; y <= vSegments; y++)
        {
            float v = (float)y / vSegments;
            float loY = Mathf.Lerp(rect.yMin, rect.yMax, v);
            float side = Mathf.Sin(v * Mathf.PI) * curveDepth;

            for (int x = 0; x <= hSegments; x++)
            {
                float u = (float)x / hSegments;
                float loX = Mathf.Lerp(rect.xMin + side, rect.xMax - side, u);
                Vector3 position = new Vector3(loX, loY, 0.0f);
                Vector2 textureCoordinate = new Vector2(
                    Mathf.Lerp(uv.xMin, uv.xMax, u),
                    Mathf.Lerp(uv.yMin, uv.yMax, v));

                vertexHelper.AddVert(position, vertexColor, textureCoordinate);
            }
        }

        int rowLength = hSegments + 1;
        for (int y = 0; y < vSegments; y++)
        {
            for (int x = 0; x < hSegments; x++)
            {
                int loLeft = y * rowLength + x;
                int loRight = loLeft + 1;
                int upLeft = loLeft + rowLength;
                int upRight = upLeft + 1;

                vertexHelper.AddTriangle(loLeft, upLeft, upRight);
                vertexHelper.AddTriangle(loLeft, upRight, loRight);
            }
        }
    }
}
