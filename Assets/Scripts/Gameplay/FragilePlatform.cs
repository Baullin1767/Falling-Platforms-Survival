using UnityEngine;

namespace FallingPlatformsSurvival
{
    public sealed class FragilePlatform : MonoBehaviour, IPlatformLandingResponder
    {
        [SerializeField] private float breakDelay = 0.3f;

        public bool OnPlayerLanded(PlatformBehaviour platform, PlayerController player)
        {
            platform.StartCollapseCountdown(breakDelay, PlatformCollapseMode.Vanish);
            return true;
        }
    }
}
