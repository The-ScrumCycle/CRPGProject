using System;
using System.Collections.Generic;
using Game.Combat.Grid;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogWarning(object message) => Console.WriteLine($"WARN: {message}");
    }

    public static class Mathf
    {
        public static int Abs(int value) => Math.Abs(value);
        public static float Abs(float value) => MathF.Abs(value);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Max(float a, float b) => MathF.Max(a, b);
        public static float Min(float a, float b) => MathF.Min(a, b);
    }
}

namespace Game.Combat.Units
{
    public class UnitVisual
    {
        public void Flash() { }
        public void HealEffect() { }
        public void LookAtCell(HexCoordinates cell) { }
    }
}

namespace Game.Combat.Actions
{
    public class HeavyMeleeAttackAction : ICombatAction
    {
        public Game.Combat.Units.Unit Actor { get; }
        public HexCoordinates TargetPos { get; }

        public HeavyMeleeAttackAction(Game.Combat.Units.Unit actor, HexCoordinates targetPos)
        {
            Actor = actor;
            TargetPos = targetPos;
        }

        public IEnumerable<HexCoordinates> GetTargetCells()
        {
            yield return TargetPos;
        }

        public bool IsValid(HexGrid grid) => false;

        public void Execute(HexGrid grid) { }
    }

    public class PullAction : ICombatAction
    {
        public Game.Combat.Units.Unit Actor { get; }
        public HexCoordinates TargetPos { get; }

        public PullAction(Game.Combat.Units.Unit actor, HexCoordinates targetPos)
        {
            Actor = actor;
            TargetPos = targetPos;
        }

        public IEnumerable<HexCoordinates> GetTargetCells()
        {
            yield return TargetPos;
        }

        public bool IsValid(HexGrid grid) => false;

        public void Execute(HexGrid grid) { }
    }
}
