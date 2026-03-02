using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Utils
{
    /// <summary>
    /// Sprite의 삽입, 위치 조정, 크기 조정을 제어함
    /// </summary>
    public static class SpriteController
    {
        /// <summary>
        /// 삽입될 이미지와 맞춤형 위치/크기를 적용함
        /// </summary>
        /// <param name="visualElement">사용 Sprite가 삽입될 공간</param>
        /// <param name="sprite">사용 이미지</param>
        /// <param name="offsetX">좌우 이동 %값 (50 입력 시 50% 이동)</param>
        /// <param name="offsetY">상하 이동 %값 (50 입력 시 50% 이동)</param>
        /// <param name="scale">확대/축소 배율</param>
        public static void SetImage(this VisualElement visualElement, Sprite sprite, float offsetX = 0, float offsetY = 0, float scale = 1)
        {
            if (visualElement == null) return;
            if (sprite == null) {
                visualElement.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }
            else {
                visualElement.style.backgroundImage = new StyleBackground(sprite);
            }
            visualElement.style.translate = new StyleTranslate(new Translate(Length.Percent(offsetX), Length.Percent(offsetY), 0));
            visualElement.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
        }
    }
}