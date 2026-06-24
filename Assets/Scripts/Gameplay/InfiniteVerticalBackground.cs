namespace FallingPlatformsSurvival
{
    using System.Collections.Generic;
    using UnityEngine;

    public class InfiniteVerticalBackground : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform[] backgrounds;
        [SerializeField] private bool useChildBackgrounds = true;
        [SerializeField] private List<Vector3> positions = new List<Vector3>();

        private void Start()
        {
            backgrounds = GetUsableBackgrounds();

            if (backgrounds.Length == 0)
            {
                Debug.LogError("No backgrounds assigned.");
                return;
            }

            if (targetCamera == null && cameraTransform != null)
            {
                targetCamera = cameraTransform.GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                Debug.LogError("InfiniteVerticalBackground requires a camera.");
                return;
            }

            cameraTransform = targetCamera.transform;
            positions.Clear();

            foreach (Transform background in backgrounds)
            {
                if (background == null)
                {
                    Debug.LogError("Background reference missing.");
                    positions.Add(Vector3.zero);
                    continue;
                }

                positions.Add(background.position);
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || backgrounds == null || backgrounds.Length == 0)
            {
                return;
            }

            RepositionBackgrounds();
        }

        private void RepositionBackgrounds()
        {
            float screenBottomY = GetScreenBottomY();
            int maxRepositions = backgrounds.Length * 2;

            for (int i = 0; i < maxRepositions; i++)
            {
                Transform lowestBackground = GetLowestBackground(out Bounds lowestBounds);

                if (lowestBackground == null || lowestBounds.max.y > screenBottomY)
                {
                    return;
                }

                MoveAboveHighestBackground(lowestBackground, lowestBounds);
            }
        }

        private Transform[] GetUsableBackgrounds()
        {
            if (useChildBackgrounds || HasInvalidBackgrounds())
            {
                List<Transform> childBackgrounds = GetChildBackgrounds();

                if (childBackgrounds.Count > 0)
                {
                    return childBackgrounds.ToArray();
                }
            }

            return GetUniqueBackgrounds();
        }

        private bool HasInvalidBackgrounds()
        {
            if (backgrounds == null || backgrounds.Length == 0)
            {
                return true;
            }

            HashSet<Transform> uniqueBackgrounds = new HashSet<Transform>();

            foreach (Transform background in backgrounds)
            {
                if (background == null || !uniqueBackgrounds.Add(background))
                {
                    return true;
                }
            }

            return false;
        }

        private List<Transform> GetChildBackgrounds()
        {
            List<Transform> childBackgrounds = new List<Transform>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (child.GetComponentInChildren<Renderer>() != null)
                {
                    childBackgrounds.Add(child);
                }
            }

            childBackgrounds.Sort((left, right) => left.position.y.CompareTo(right.position.y));
            return childBackgrounds;
        }

        private Transform[] GetUniqueBackgrounds()
        {
            List<Transform> uniqueBackgrounds = new List<Transform>();
            HashSet<Transform> seenBackgrounds = new HashSet<Transform>();

            if (backgrounds == null)
            {
                return uniqueBackgrounds.ToArray();
            }

            foreach (Transform background in backgrounds)
            {
                if (background != null && seenBackgrounds.Add(background))
                {
                    uniqueBackgrounds.Add(background);
                }
            }

            uniqueBackgrounds.Sort((left, right) => left.position.y.CompareTo(right.position.y));
            return uniqueBackgrounds.ToArray();
        }

        private Transform GetLowestBackground(out Bounds lowestBounds)
        {
            Transform lowestBackground = null;
            lowestBounds = default;
            float lowestBottomY = float.MaxValue;

            foreach (Transform background in backgrounds)
            {
                if (background == null)
                {
                    continue;
                }

                Bounds bounds = GetWorldBounds(background);

                if (bounds.min.y < lowestBottomY)
                {
                    lowestBottomY = bounds.min.y;
                    lowestBackground = background;
                    lowestBounds = bounds;
                }
            }

            return lowestBackground;
        }

        private void MoveAboveHighestBackground(Transform background, Bounds bounds)
        {
            float highestTopY = GetHighestTopY(background);

            if (highestTopY == float.MinValue)
            {
                return;
            }

            float targetCenterY = highestTopY + bounds.extents.y;
            float centerOffsetY = bounds.center.y - background.position.y;

            background.position = new Vector3(
                background.position.x,
                targetCenterY - centerOffsetY,
                background.position.z
            );
        }

        private float GetHighestTopY(Transform ignoredBackground)
        {
            float highestTopY = float.MinValue;

            foreach (Transform background in backgrounds)
            {
                if (background == null || background == ignoredBackground)
                {
                    continue;
                }

                highestTopY = Mathf.Max(highestTopY, GetWorldBounds(background).max.y);
            }

            return highestTopY;
        }

        private float GetScreenBottomY()
        {
            if (targetCamera.orthographic)
            {
                return targetCamera.transform.position.y - targetCamera.orthographicSize;
            }

            return targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, targetCamera.nearClipPlane)).y;
        }

        private Bounds GetWorldBounds(Transform background)
        {
            Renderer[] renderers = background.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogError($"Renderer missing on background '{background.name}'.");
                return new Bounds(Vector3.zero, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        public void ResetBG()
        {
            if (backgrounds == null)
            {
                return;
            }

            for (int i = 0; i < backgrounds.Length && i < positions.Count; i++)
            {
                if (backgrounds[i] != null)
                {
                    backgrounds[i].position = positions[i];
                }
            }
        }
    }
}
