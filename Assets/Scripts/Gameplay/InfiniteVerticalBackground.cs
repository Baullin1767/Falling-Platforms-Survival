using System.Collections.Generic;
using UnityEngine;

namespace FallingPlatformsSurvival
{
    using UnityEngine;

    public class InfiniteVerticalBackground : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform[] backgrounds;
        [SerializeField] private List<Vector3> positions;

        private float backgroundHeight;

        private void Start()
        {
            if (backgrounds.Length == 0)
            {
                Debug.LogError("No backgrounds assigned.");
                return;
            }

            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
            }

            SpriteRenderer sr = backgrounds[0].GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                backgroundHeight = sr.bounds.size.y;
            }
            else
            {
                Debug.LogError("SpriteRenderer missing on background.");
            }

            foreach (var transformBG in backgrounds)
            {
                positions.Add(transformBG.position);
            }
        }

        private void LateUpdate()
        {
            RepositionBackgrounds();
        }

        private void RepositionBackgrounds()
        {
            float cameraY = cameraTransform.position.y;

            foreach (Transform bg in backgrounds)
            {
                // If background is far below camera
                if (cameraY - bg.position.y >= backgroundHeight)
                {
                    float highestY = GetHighestBackgroundY();

                    bg.position = new Vector3(
                        bg.position.x,
                        highestY + backgroundHeight,
                        bg.position.z
                    );
                }
            }
        }

        private float GetHighestBackgroundY()
        {
            float highestY = backgrounds[0].position.y;

            foreach (Transform bg in backgrounds)
            {
                if (bg.position.y > highestY)
                    highestY = bg.position.y;
            }

            return highestY;
        }

        public void ResetBG()
        {
            for (int i = 0; i < backgrounds.Length; i++)
            {
                backgrounds[i].position = positions[i];
            }
        }
    }}
