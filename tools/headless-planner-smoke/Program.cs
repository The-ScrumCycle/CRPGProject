using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Combat.Actions;
using Game.Combat.Grid;
using Game.Combat.Units;
using Game.Core.Party;

var grid = new HexGrid(4, 4);
var resolver = new ActionResolver(grid);
var planner = new EnemyDecisionService(grid, resolver);

var skeleton = new Unit(
    id: "sk_melee",
    displayName: "Skeleton Melee",
    role: UnitRole.Enemy,
    stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
    aiBehavior: AIBehavior.SkeletonMelee);

var player = new Unit(
    id: "player_1",
    displayName: "Captain",
    role: UnitRole.Player,
    stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

grid.PlaceUnit(skeleton, new HexCoordinates(1, 1));
grid.PlaceUnit(player, new HexCoordinates(1, 2));

var allUnits = new List<Unit> { skeleton, player };
var intent = planner.GenerateIntentForEnemy(skeleton, allUnits);

if (intent == null)
{
    Console.WriteLine("No intent generated.");
    return;
}

Console.WriteLine($"Actor: {intent.Actor.DisplayName}");
Console.WriteLine($"Action: {intent.Action.GetType().Name}");
Console.WriteLine($"Target: {intent.TargetUnit?.DisplayName ?? "<none>"}");
Console.WriteLine($"PredictedDamage: {intent.PredictedDamage}");
Console.WriteLine($"TargetCells: {string.Join(", ", intent.TargetCells)}");
Console.WriteLine($"PushDestination: {(intent.PushDestination.HasValue ? intent.PushDestination.Value.ToString() : "<none>")}");
