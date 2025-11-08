using UnityEngine;

namespace VaultSystems.Weapons
{
    public class LaserAimGizmo : MonoBehaviour
    {
        [Header("Laser Settings")]
        [SerializeField] private float laserRange = 1000f;
        [SerializeField] private Color laserColor = Color.red;
        [SerializeField] private float laserWidth = 0.02f;
        
        private LineRenderer lineRenderer;
        private Transform firePoint;
        private Camera mainCamera;
        private bool isActive = false;

        private void Start()
        {
            CreateLineRenderer();
            mainCamera = Camera.main;
            firePoint = transform.Find("FirePoint") ?? transform;
        }

        private void CreateLineRenderer()
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();

            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = laserColor;
            lineRenderer.endColor = laserColor;
            lineRenderer.sortingOrder = 1;
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (!isActive || mainCamera == null || firePoint == null)
                return;

            UpdateLaserPosition();
        }

        private void UpdateLaserPosition()
        {
            Vector3 start = firePoint.position;
            Vector3 forward = mainCamera.transform.forward;
            Vector3 end = start + forward * laserRange;

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public void SetLaserActive(bool active)
        {
            isActive = active;
            if (lineRenderer != null)
                lineRenderer.enabled = active;
        }

        public bool IsLaserActive => isActive;
    }
}
