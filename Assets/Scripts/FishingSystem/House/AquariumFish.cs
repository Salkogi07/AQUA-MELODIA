using UnityEngine;
using System.Collections;

namespace FishingSystem.House
{
    public class AquariumFish : MonoBehaviour
    {
        private BoxCollider2D _swimArea;
        private float _minSpeed;
        private float _maxSpeed;
        private float _changeDelay;
        private bool _originalSpriteFacesRight;

        private Vector3 _targetPosition;
        private float _currentSpeed;
        private bool _isWaiting = false;
        private Vector3 _originalScale;

        public void Initialize(BoxCollider2D swimArea, float minSpeed, float maxSpeed, float changeDelay, bool originalSpriteFacesRight)
        {
            _swimArea = swimArea;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
            _changeDelay = changeDelay;
            _originalSpriteFacesRight = originalSpriteFacesRight;

            _originalScale = transform.localScale;

            SetNewTarget();
        }

        private void Update()
        {
            if (_swimArea == null || _isWaiting) return;

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _currentSpeed * Time.deltaTime);

            UpdateFacingAndRotation();

            if (Vector3.Distance(transform.position, _targetPosition) < 0.05f)
            {
                StartCoroutine(WaitAndPickNewTargetRoutine());
            }
        }

        private void SetNewTarget()
        {
            if (_swimArea == null) return;

            Bounds bounds = _swimArea.bounds;
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            float z = transform.position.z;

            _targetPosition = new Vector3(x, y, z);
            _currentSpeed = Random.Range(_minSpeed, _maxSpeed);
        }

        private IEnumerator WaitAndPickNewTargetRoutine()
        {
            _isWaiting = true;
            float waitTime = Random.Range(0.5f, _changeDelay);
            yield return new WaitForSeconds(waitTime);

            SetNewTarget();
            _isWaiting = false;
        }

        private void UpdateFacingAndRotation()
        {
            Vector3 direction = _targetPosition - transform.position;

            // 원본 이미지 머리 방향에 따른 스케일 값 보정 처리
            float facingMultiplier = _originalSpriteFacesRight ? 1f : -1f;

            if (direction.x > 0.02f)
            {
                // 우측 이동 시
                transform.localScale = new Vector3(facingMultiplier * Mathf.Abs(_originalScale.x), _originalScale.y, _originalScale.z);
            }
            else if (direction.x < -0.02f)
            {
                // 좌측 이동 시
                transform.localScale = new Vector3(-facingMultiplier * Mathf.Abs(_originalScale.x), _originalScale.y, _originalScale.z);
            }

            // 위아래 완만한 곡선 유영 연출을 위한 각도 보정식
            if (direction.magnitude > 0.05f)
            {
                float angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
                angle = Mathf.Clamp(angle, -15f, 15f);

                // 좌측(flipped) 이동 시 부호 반전을 통해 회전 방향이 어색하게 꺾이지 않고 동일하게 맞춰지도록 처리
                float finalAngle = angle;
                if (direction.x < 0)
                {
                    finalAngle = -angle;
                }

                transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);
            }
        }
    }
}