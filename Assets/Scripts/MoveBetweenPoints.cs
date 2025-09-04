using UnityEngine;

namespace QuestMarkerTracking
{
    public class MoveBetweenPoints : MonoBehaviour
    {
        [SerializeField] private Vector3 pointA;
        [SerializeField] private Vector3 pointB;
        [SerializeField] private float speed = 1.0f;
        [SerializeField] private float acceleration = 0.1f;

        private bool _inverseDirection;
        private float _startSpeed;

        private void Start()
        {
            _startSpeed = speed;
            Debug.Log(_startSpeed);
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, pointA) < 0.1f)
            {
                _inverseDirection = false;
                speed = _startSpeed;
            }
            else if (Vector3.Distance(transform.position, pointB) < 0.1f)
            {
                _inverseDirection = true;
                speed = _startSpeed;
            }

            if (_inverseDirection)
            {
                transform.position = Vector3.MoveTowards(transform.position, pointA, speed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, pointB, speed * Time.deltaTime);
            }

            speed += Time.deltaTime * acceleration; // Gradually increase speed
        }
    }
}