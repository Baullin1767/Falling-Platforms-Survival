namespace FallingPlatformsSurvival
{
    public interface IPlatformLandingResponder
    {
        bool OnPlayerLanded(PlatformBehaviour platform, PlayerController player);
    }
}
