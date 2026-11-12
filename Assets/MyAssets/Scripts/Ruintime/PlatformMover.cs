using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformCoinLab.Runtime
{
    public sealed class PlatformMover : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private GameSession session;
        [SerializeField] private float speed = 5f;

        private void OnEnable()
        {
            move.action.Enable();
        }

        private void OnDisable()
        {
            move.action.Disable();
        }

        private void Update()
        {
            if (!session.CanMove)
            {
                return;
            }

            Vector2 input = Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f);

            Vector3 next = transform.position + new Vector3(input.x, 0f, input.y) * speed * Time.deltaTime;

            // 이동은 XZ 평면에서 일어나므로 X·Z를 Ground(20×20) 안쪽으로 제한합니다.
            // 수정 전에는 next.y를 제한해 앞뒤(Z) 방향으로 Ground 밖까지 나갈 수 있었습니다.
            next.x = Mathf.Clamp(next.x, -9f, 9f);
            next.z = Mathf.Clamp(next.z, -9f, 9f);

            transform.position = next;
        }
    }
}
