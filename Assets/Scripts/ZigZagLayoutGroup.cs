using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 子要素をジグザグに並べるレイアウトグループ
/// </summary>
public class ZigZagLayoutGroup : LayoutGroup
{
    public float spacingX = 100f;   // 横間隔
    public float spacingY = 100f;   // 縦間隔
    public float offsetX = 50f;     // 偶数行の横ズレ量
    public int itemsPerRow = 4;     // 1行あたりのアイテム数

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        int row = 0;
        int col = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            var item = rectChildren[i];

            float x = col * spacingX + (row % 2 == 1 ? offsetX : 0);
            float y = -row * spacingY;

            SetChildAlongAxis(item, 0, x, item.sizeDelta.x);
            SetChildAlongAxis(item, 1, y, item.sizeDelta.y);

            col++;
            if (col >= itemsPerRow)
            {
                col = 0;
                row++;
            }
        }
    }

    public override void CalculateLayoutInputVertical() { }
    public override void SetLayoutHorizontal() => CalculateLayoutInputHorizontal();
    public override void SetLayoutVertical() => CalculateLayoutInputHorizontal();
}