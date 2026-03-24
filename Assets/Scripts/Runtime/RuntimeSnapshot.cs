using System.Collections.Generic;
using UnityEngine;

namespace FallingPlatformsSurvival
{
    public enum GameRunState
    {
        Waiting,
        Playing,
        GameOver
    }

    public enum PlatformCollapseMode
    {
        Fall,
        Vanish
    }

    public enum PlatformCollapseState
    {
        Idle,
        Triggered,
        Falling,
        Vanished,
        Recycling
    }

    public readonly struct PlayerRuntimeState
    {
        public PlayerRuntimeState(
            Vector2 position,
            Vector2 velocity,
            bool isGrounded,
            bool isAlive,
            int currentPlatformId,
            bool canJump,
            bool hasBufferedJump)
        {
            Position = position;
            Velocity = velocity;
            IsGrounded = isGrounded;
            IsAlive = isAlive;
            CurrentPlatformId = currentPlatformId;
            CanJump = canJump;
            HasBufferedJump = hasBufferedJump;
        }

        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public bool IsGrounded { get; }
        public bool IsAlive { get; }
        public int CurrentPlatformId { get; }
        public bool CanJump { get; }
        public bool HasBufferedJump { get; }
    }

    public readonly struct PlatformRuntimeState
    {
        public PlatformRuntimeState(
            int platformId,
            bool isActive,
            Vector2 position,
            PlatformCollapseMode collapseMode,
            float countdownRemaining,
            PlatformCollapseState collapseState,
            bool isCurrentPlatform)
        {
            PlatformId = platformId;
            IsActive = isActive;
            Position = position;
            CollapseMode = collapseMode;
            CountdownRemaining = countdownRemaining;
            CollapseState = collapseState;
            IsCurrentPlatform = isCurrentPlatform;
        }

        public int PlatformId { get; }
        public bool IsActive { get; }
        public Vector2 Position { get; }
        public PlatformCollapseMode CollapseMode { get; }
        public float CountdownRemaining { get; }
        public PlatformCollapseState CollapseState { get; }
        public bool IsCurrentPlatform { get; }
    }

    public sealed class GameRuntimeSnapshot
    {
        public GameRuntimeSnapshot(
            GameRunState state,
            float elapsedSurvivalTime,
            PlayerRuntimeState player,
            IReadOnlyList<PlatformRuntimeState> activePlatforms)
        {
            State = state;
            ElapsedSurvivalTime = elapsedSurvivalTime;
            Player = player;
            ActivePlatforms = activePlatforms;
        }

        public GameRunState State { get; }
        public float ElapsedSurvivalTime { get; }
        public PlayerRuntimeState Player { get; }
        public IReadOnlyList<PlatformRuntimeState> ActivePlatforms { get; }
    }
}
