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
    PrintCandidates(skeleton, PreviewCandidatesForEnemy(skeleton, allUnits, grid, resolver));

    var intent = planner.GenerateIntentForEnemy(skeleton, allUnits);
    Console.WriteLine("Chosen Intent:");

    PrintIntent(intent);
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
    PrintCandidates(melee, PreviewCandidatesForEnemy(melee, allUnits, grid, resolver));
    PrintCandidates(ranged, PreviewCandidatesForEnemy(ranged, allUnits, grid, resolver));

    planner.GenerateAllIntents(state);
    Console.WriteLine("Chosen Intents:");

    foreach (var intent in state.GetIntents())
    {
        PrintIntent(intent);
    }
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

static void PrintCandidates(Unit enemy, IReadOnlyList<ActionIntent> candidates)
{
    Console.WriteLine($"Candidates for {enemy.DisplayName}:");

    if (candidates.Count == 0)
    {
        Console.WriteLine("  <none>");
        Console.WriteLine();
        return;
    }

    for (int i = 0; i < candidates.Count; i++)
    {
        var intent = candidates[i];
        Console.WriteLine($"  {i + 1}. {intent.Action.GetType().Name} -> {intent.TargetUnit?.DisplayName ?? "<none>"} | dmg {intent.PredictedDamage}");
    }

    Console.WriteLine();
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
