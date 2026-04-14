using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Combat.AI;
using Game.Combat.Actions;
using Game.Combat.Grid;
using Game.Combat.Units;
using Game.Core.Party;

RunSingleEnemySmoke();
Console.WriteLine();
RunTwoEnemySmoke();

static void RunSingleEnemySmoke()
{
    Console.WriteLine("== Single Enemy Smoke ==");

    var grid = new HexGrid(4, 4);
    var resolver = new ActionResolver(grid);
    var planner = new EnemyDecisionService(grid, resolver);

    var skeleton = MakeEnemy(
        id: "sk_melee",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    grid.PlaceUnit(skeleton, new HexCoordinates(1, 1));
    grid.PlaceUnit(player, new HexCoordinates(1, 2));

    var allUnits = new List<Unit> { skeleton, player };
    var results = ExplainPlan(new List<Unit> { skeleton }, allUnits, grid, resolver);
    var intent = results[0].ChosenIntent;

    Console.WriteLine($"[CHECK] adjacent melee enemy chooses melee: {(intent?.Action is MeleeAttackAction ? "PASS" : "FAIL")}");
    Console.WriteLine();
}

static void RunTwoEnemySmoke()
{
    Console.WriteLine("== Two Enemy Smoke ==");

    var grid = new HexGrid(5, 5);
    var resolver = new ActionResolver(grid);
    var planner = new EnemyDecisionService(grid, resolver);
    var state = new CombatRuntimeState();

    var melee = MakeEnemy(
        id: "sk_melee",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var ranged = MakeEnemy(
        id: "sk_ranged",
        displayName: "Skeleton Ranged",
        stats: new UnitStats(maxHealth: 10, attackPower: 3, movementRange: 2, attackRange: 3),
        aiBehavior: AIBehavior.SkeletonRanged,
        actions: new List<CombatActionType> { CombatActionType.SplashAttack });

    var playerA = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    var playerB = MakePlayer(
        id: "player_2",
        displayName: "Warrior",
        stats: new UnitStats(maxHealth: 22, attackPower: 6, movementRange: 3, attackRange: 1));

    grid.PlaceUnit(melee, new HexCoordinates(1, 1));
    grid.PlaceUnit(ranged, new HexCoordinates(0, 0));
    grid.PlaceUnit(playerA, new HexCoordinates(1, 2));
    grid.PlaceUnit(playerB, new HexCoordinates(2, 2));

    state.RegisterUnit(melee, null);
    state.RegisterUnit(ranged, null);
    state.RegisterUnit(playerA, null);
    state.RegisterUnit(playerB, null);

    var allUnits = state.AllUnits;
    var results = ExplainPlan(new List<Unit> { melee, ranged }, allUnits, grid, resolver);
    var rangedResult = results.Find(result => result.Enemy == ranged);

    bool badFriendlyFireSplash = rangedResult != null && ChosenSplashHitsAlly(rangedResult.ChosenIntent, allUnits);
    Console.WriteLine($"[CHECK] ranged enemy avoids ally-hitting splash: {(badFriendlyFireSplash ? "FAIL" : "PASS")}");
    Console.WriteLine();
}

static Unit MakeEnemy(string id, string displayName, UnitStats stats, AIBehavior aiBehavior, List<CombatActionType> actions = null)
{
    return new Unit(id, displayName, UnitRole.Enemy, stats, aiBehavior, actions);
}

static Unit MakePlayer(string id, string displayName, UnitStats stats)
{
    return new Unit(id, displayName, UnitRole.Player, stats);
}

static List<ActionIntent> PreviewCandidatesForEnemy(Unit enemy, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
{
    var candidates = new List<ActionIntent>();
    var brain = BrainFactory.GetBrain(enemy.AIBehavior);
    if (brain == null)
    {
        return candidates;
    }

    var seenSignatures = new HashSet<string>();

    foreach (var action in brain.GenerateCandidateActions(enemy, allUnits, grid, resolver))
    {
        var intent = resolver.Preview(action);
        if (intent == null)
        {
            continue;
        }

        if (seenSignatures.Add(BuildIntentSignature(intent)))
        {
            candidates.Add(intent);
        }
    }

    return candidates;
}

static List<EnemyPlanResult> ExplainPlan(IReadOnlyList<Unit> enemies, IReadOnlyList<Unit> allUnits, HexGrid grid, ActionResolver resolver)
{
    var planningContext = new EnemyPlanningContext();
    var scorer = new EnemyActionScorer(grid);
    var results = new List<EnemyPlanResult>();

    foreach (var enemy in enemies)
    {
        var candidates = PreviewCandidatesForEnemy(enemy, allUnits, grid, resolver);
        var scoredCandidates = new List<ScoredCandidate>();

        foreach (var candidate in candidates)
        {
            float score = scorer.Score(enemy, candidate, allUnits, planningContext);
            scoredCandidates.Add(new ScoredCandidate(candidate, score));
        }

        scoredCandidates.Sort((left, right) => right.Score.CompareTo(left.Score));

        Console.WriteLine($"Candidates for {enemy.DisplayName}:");
        if (scoredCandidates.Count == 0)
        {
            Console.WriteLine("  <none>");
            Console.WriteLine();
            results.Add(new EnemyPlanResult(enemy, scoredCandidates, null));
            continue;
        }

        for (int i = 0; i < scoredCandidates.Count; i++)
        {
            var candidate = scoredCandidates[i];
            Console.WriteLine($"  {i + 1}. {DescribeIntent(candidate.Intent)} | score {candidate.Score:0.##}");
        }

        var chosenIntent = scoredCandidates[0].Intent;
        Console.WriteLine();
        Console.WriteLine("Chosen Intent:");
        PrintIntent(chosenIntent);

        RegisterIntentOutcome(chosenIntent, allUnits, planningContext, grid);
        results.Add(new EnemyPlanResult(enemy, scoredCandidates, chosenIntent));
    }

    return results;
}

static string BuildIntentSignature(ActionIntent intent)
{
    var signature = intent.Action.GetType().Name;

    if (intent.Action is MoveAction move)
    {
        return $"{signature}|move:{move.Destination.q},{move.Destination.r}";
    }

    foreach (var cell in intent.TargetCells)
    {
        signature += $"|{cell.q},{cell.r}";
    }

    if (intent.TargetUnit != null)
    {
        signature += $"|target:{intent.TargetUnit.Id}";
    }

    return signature;
}

static string DescribeIntent(ActionIntent intent)
{
    if (intent == null)
    {
        return "<none>";
    }

    return $"{intent.Action.GetType().Name} -> {intent.TargetUnit?.DisplayName ?? "<none>"} | dmg {intent.PredictedDamage}";
}

static void RegisterIntentOutcome(ActionIntent intent, IReadOnlyList<Unit> allUnits, EnemyPlanningContext planningContext, HexGrid grid)
{
    if (intent == null)
    {
        return;
    }

    if (intent.Action is MoveAction move)
    {
        planningContext.ReserveMoveDestination(move.Destination);

        if (IsFrontlineMove(intent.Actor, move.Destination, allUnits, grid))
        {
            planningContext.MarkFrontlineAlly(intent.Actor);
        }
    }

    foreach (var kvp in EnemyActionScorer.GetPredictedDamageToPlayers(intent, allUnits))
    {
        planningContext.AddPlannedDamage(kvp.Key, kvp.Value);
    }

    if (CreatesFriendlyFireZone(intent.Action))
    {
        foreach (var cell in intent.TargetCells)
        {
            planningContext.ReserveAttackCell(cell);
        }
    }

    if (intent.Action is MeleeAttackAction ||
        intent.Action is SweepAttackAction ||
        intent.Action is GrappleAction ||
        intent.Actor.AIBehavior == AIBehavior.SkeletonMelee ||
        intent.Actor.AIBehavior == AIBehavior.Aggressive ||
        intent.Actor.AIBehavior == AIBehavior.HydraGrappler)
    {
        planningContext.MarkFrontlineAlly(intent.Actor);
    }
}

static bool CreatesFriendlyFireZone(ICombatAction action)
{
    return action is SplashAttackAction || action is SweepAttackAction;
}

static bool IsFrontlineMove(Unit actor, HexCoordinates destination, IReadOnlyList<Unit> allUnits, HexGrid grid)
{
    if (actor == null ||
        (actor.AIBehavior != AIBehavior.SkeletonMelee &&
         actor.AIBehavior != AIBehavior.Aggressive &&
         actor.AIBehavior != AIBehavior.HydraGrappler))
    {
        return false;
    }

    foreach (var unit in allUnits)
    {
        if (unit.IsAlive && unit.IsPlayerControlled && grid.GetDistance(unit.Coordinates, destination) <= 1)
        {
            return true;
        }
    }

    return false;
}

static bool ChosenSplashHitsAlly(ActionIntent intent, IReadOnlyList<Unit> allUnits)
{
    if (intent?.Action is not SplashAttackAction)
    {
        return false;
    }

    foreach (var unit in allUnits)
    {
        if (!unit.IsAlive || unit.IsPlayerControlled || unit == intent.Actor)
        {
            continue;
        }

        if (intent.TargetCells.Contains(unit.Coordinates))
        {
            return true;
        }
    }

    return false;
}

static void PrintIntent(ActionIntent intent)
{
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
    Console.WriteLine();
}

readonly record struct ScoredCandidate(ActionIntent Intent, float Score);

sealed record EnemyPlanResult(Unit Enemy, IReadOnlyList<ScoredCandidate> Candidates, ActionIntent ChosenIntent);
