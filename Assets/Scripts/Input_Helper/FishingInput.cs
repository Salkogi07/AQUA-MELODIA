using UnityEngine;
using UnityEngine.EventSystems;

namespace FishingSystem.Input_Helper
{
    public static class FishingInput
    {
        /// <summary>
        /// 마우스 좌클릭(Down)을 감지하며, UI 및 지정된 상호작용 월드 오브젝트 클릭 시에는 무시합니다.
        /// </summary>
        public static bool GetLeftClickDown(bool ignoreUI = true, bool ignoreWorldInteractives = true)
        {
            if (!Input.GetMouseButtonDown(0)) return false;

            // 1. UI 클릭 차단
            if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            // 2. 월드 물리 상호작용(상자, NPC 등) 클릭 차단
            if (ignoreWorldInteractives && Camera.main != null)
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                // 마우스 포인트 바로 밑에 있는 2D 콜라이더 레이캐스팅 검출
                Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
                if (hit != null)
                {
                    // 클릭한 대상이 상자이거나, 오브젝트 태그가 'Interactive'인 경우 낚시 입력 무효화
                    if (hit.CompareTag("Interactive"))
                    {
                        return false; 
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 마우스 좌클릭 유지(Pressing) 상태를 감지하며, UI 및 상호작용 오브젝트 영역에서는 무시합니다.
        /// </summary>
        public static bool GetLeftClickHeld(bool ignoreUI = true, bool ignoreWorldInteractives = true)
        {
            if (!Input.GetMouseButton(0)) return false;

            // UI 충돌 체크
            if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }

            // 월드 물리 충돌 체크
            if (ignoreWorldInteractives && Camera.main != null)
            {
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
                if (hit != null)
                {
                    if (hit.CompareTag("Interactive"))
                    {
                        return false;
                    }
                }
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