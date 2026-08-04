using UnityEngine;
using UnityEngine.EventSystems;

namespace FishingSystem.Input_Helper
{
    public static class FishingInput
    {
        /// <summary>
        /// 마우스 좌클릭(Down)을 감지합니다.
        /// ignoreUI 매개변수가 true일 경우, UI 오브젝트 위에서 클릭한 것은 무시합니다.
        /// </summary>
        public static bool GetLeftClickDown(bool ignoreUI = true)
        {
            if (!Input.GetMouseButtonDown(0)) return false;

            // UI 충돌 무시 기능 활성화 시, 포인터가 UI 위에 있다면 감지 취소
            if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 마우스 좌클릭 유지(Pressing) 상태를 감지합니다.
        /// </summary>
        public static bool GetLeftClickHeld(bool ignoreUI = true)
        {
            if (!Input.GetMouseButton(0)) return false;

            if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 마우스 좌클릭을 떼는(Up) 타이밍을 감지합니다.
        /// </summary>
        public static bool GetLeftClickUp()
        {
            return Input.GetMouseButtonUp(0);
        }
    }
}