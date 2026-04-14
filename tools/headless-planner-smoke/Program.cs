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
Console.WriteLine();
RunSweepAttackSmoke();
Console.WriteLine();
RunHealerSupportSmoke();
Console.WriteLine();
RunHealerInitiatesHealSmoke();
Console.WriteLine();
RunHydraGrappleSmoke();
Console.WriteLine();
RunRetreatToHealerSmoke();
Console.WriteLine();
RunRangedRepositionSmoke();
Console.WriteLine();
RunAvoidReservedSplashMoveSmoke();
Console.WriteLine();
RunAvoidSplashOnPlannedAllyMoveSmoke();

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

static void RunSweepAttackSmoke()
{
    Console.WriteLine("== Sweep Attack Smoke ==");

    var grid = new HexGrid(5, 5);
    var resolver = new ActionResolver(grid);

    var sweeper = MakeEnemy(
        id: "sk_sweep",
        displayName: "Skeleton Sweeper",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee,
        actions: new List<CombatActionType> { CombatActionType.SweepAttack });

    var playerA = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    var playerB = MakePlayer(
        id: "player_2",
        displayName: "Warrior",
        stats: new UnitStats(maxHealth: 22, attackPower: 6, movementRange: 3, attackRange: 1));

    PlaceUnits(
        grid,
        (sweeper, new HexCoordinates(1, 1)),
        (playerA, new HexCoordinates(1, 2)),
        (playerB, new HexCoordinates(2, 2)));

    var allUnits = new List<Unit> { sweeper, playerA, playerB };
    var intent = ExplainPlan(new List<Unit> { sweeper }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseSweep =
        intent?.Action is SweepAttackAction &&
        HitsUnit(intent, playerA) &&
        HitsUnit(intent, playerB);

    PrintCheck("melee sweeper prefers multi-target sweep", choseSweep);
    Console.WriteLine();
}

static void RunHealerSupportSmoke()
{
    Console.WriteLine("== Healer Support Smoke ==");

    var grid = new HexGrid(5, 5);
    var resolver = new ActionResolver(grid);

    var healer = MakeEnemy(
        id: "healer",
        displayName: "Healer",
        stats: new UnitStats(maxHealth: 10, attackPower: 2, movementRange: 2, attackRange: 3, healPower: 4),
        aiBehavior: AIBehavior.Healer);

    var frontline = MakeEnemy(
        id: "sk_front",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var backline = MakeEnemy(
        id: "sk_back",
        displayName: "Skeleton Ranged",
        stats: new UnitStats(maxHealth: 10, attackPower: 3, movementRange: 2, attackRange: 3),
        aiBehavior: AIBehavior.SkeletonRanged);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    SetHealth(frontline, 6);
    SetHealth(backline, 5);

    PlaceUnits(
        grid,
        (healer, new HexCoordinates(0, 0)),
        (frontline, new HexCoordinates(1, 1)),
        (backline, new HexCoordinates(0, 2)),
        (player, new HexCoordinates(1, 2)));

    var allUnits = new List<Unit> { healer, frontline, backline, player };
    var intent = ExplainPlan(new List<Unit> { healer }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseFrontlineHeal =
        intent?.Action is RangedHealAction heal &&
        heal.Target == frontline;

    PrintCheck("healer prefers healing frontline ally", choseFrontlineHeal);
    Console.WriteLine();
}

static void RunHealerInitiatesHealSmoke()
{
    Console.WriteLine("== Healer Initiates Heal Smoke ==");

    var grid = new HexGrid(7, 5);
    var resolver = new ActionResolver(grid);

    var healer = MakeEnemy(
        id: "healer",
        displayName: "Healer",
        stats: new UnitStats(maxHealth: 10, attackPower: 2, movementRange: 2, attackRange: 3, healPower: 4),
        aiBehavior: AIBehavior.Healer);

    var frontline = MakeEnemy(
        id: "sk_front",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    SetHealth(frontline, 6);

    PlaceUnits(
        grid,
        (healer, new HexCoordinates(0, 0)),
        (frontline, new HexCoordinates(4, 0)),
        (player, new HexCoordinates(5, 0)));

    var allUnits = new List<Unit> { healer, frontline, player };
    var intent = ExplainPlan(new List<Unit> { healer }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseSupportMove =
        intent?.Action is MoveAction move &&
        grid.GetDistance(move.Destination, frontline.Coordinates) <= healer.Stats.attackRange &&
        grid.GetDistance(move.Destination, frontline.Coordinates) >= 1;

    PrintCheck("healer moves into heal range for damaged ally", choseSupportMove);
    Console.WriteLine();
}

static void RunHydraGrappleSmoke()
{
    Console.WriteLine("== Hydra Grapple Smoke ==");

    var grid = new HexGrid(5, 5);
    var resolver = new ActionResolver(grid);

    var hydra = MakeEnemy(
        id: "hydra",
        displayName: "Hydra",
        stats: new UnitStats(maxHealth: 18, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.HydraGrappler);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    var otherPlayer = MakePlayer(
        id: "player_2",
        displayName: "Warrior",
        stats: new UnitStats(maxHealth: 22, attackPower: 6, movementRange: 3, attackRange: 1));

    PlaceUnits(
        grid,
        (hydra, new HexCoordinates(1, 1)),
        (player, new HexCoordinates(1, 2)),
        (otherPlayer, new HexCoordinates(4, 4)));

    var allUnits = new List<Unit> { hydra, player, otherPlayer };
    var intent = ExplainPlan(new List<Unit> { hydra }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseGrapple =
        intent?.Action is GrappleAction grapple &&
        grapple.Target == player;

    PrintCheck("hydra grapples adjacent player", choseGrapple);
    Console.WriteLine();
}

static void RunRetreatToHealerSmoke()
{
    Console.WriteLine("== Retreat To Healer Smoke ==");

    var grid = new HexGrid(6, 4);
    var resolver = new ActionResolver(grid);

    var healer = MakeEnemy(
        id: "healer",
        displayName: "Healer",
        stats: new UnitStats(maxHealth: 10, attackPower: 2, movementRange: 2, attackRange: 3, healPower: 4),
        aiBehavior: AIBehavior.Healer);

    var retreating = MakeEnemy(
        id: "sk_melee",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    SetHealth(retreating, 3);

    PlaceUnits(
        grid,
        (healer, new HexCoordinates(0, 0)),
        (retreating, new HexCoordinates(2, 0)),
        (player, new HexCoordinates(3, 0)));

    var allUnits = new List<Unit> { healer, retreating, player };
    var intent = ExplainPlan(new List<Unit> { retreating }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseRetreatMove =
        intent?.Action is MoveAction move &&
        grid.GetDistance(move.Destination, healer.Coordinates) < grid.GetDistance(retreating.Coordinates, healer.Coordinates);

    PrintCheck("low-hp melee retreats toward healer", choseRetreatMove);
    Console.WriteLine();
}

static void RunRangedRepositionSmoke()
{
    Console.WriteLine("== Ranged Reposition Smoke ==");

    var grid = new HexGrid(7, 5);
    var resolver = new ActionResolver(grid);

    var ranged = MakeEnemy(
        id: "sk_ranged",
        displayName: "Skeleton Ranged",
        stats: new UnitStats(maxHealth: 10, attackPower: 3, movementRange: 2, attackRange: 3),
        aiBehavior: AIBehavior.SkeletonRanged);

    var player = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    PlaceUnits(
        grid,
        (ranged, new HexCoordinates(0, 0)),
        (player, new HexCoordinates(5, 0)));

    var allUnits = new List<Unit> { ranged, player };
    var intent = ExplainPlan(new List<Unit> { ranged }, allUnits, grid, resolver)[0].ChosenIntent;

    bool choseRepositionMove =
        intent?.Action is MoveAction move &&
        grid.GetDistance(move.Destination, player.Coordinates) < grid.GetDistance(ranged.Coordinates, player.Coordinates);

    PrintCheck("ranged enemy repositions toward ideal range", choseRepositionMove);
    Console.WriteLine();
}

static void RunAvoidReservedSplashMoveSmoke()
{
    Console.WriteLine("== Avoid Reserved Splash Move Smoke ==");

    var grid = new HexGrid(7, 5);
    var resolver = new ActionResolver(grid);

    var ranged = MakeEnemy(
        id: "sk_ranged",
        displayName: "Skeleton Ranged",
        stats: new UnitStats(maxHealth: 10, attackPower: 3, movementRange: 2, attackRange: 3),
        aiBehavior: AIBehavior.SkeletonRanged,
        actions: new List<CombatActionType> { CombatActionType.SplashAttack });

    var melee = MakeEnemy(
        id: "sk_melee",
        displayName: "Skeleton Melee",
        stats: new UnitStats(maxHealth: 12, attackPower: 4, movementRange: 2, attackRange: 1),
        aiBehavior: AIBehavior.SkeletonMelee);

    var playerA = MakePlayer(
        id: "player_1",
        displayName: "Captain",
        stats: new UnitStats(maxHealth: 20, attackPower: 5, movementRange: 3, attackRange: 1));

    var playerB = MakePlayer(
        id: "player_2",
        displayName: "Warrior",
        stats: new UnitStats(maxHealth: 22, attackPower: 6, movementRange: 3, attackRange: 1));

    SetHealth(playerA, 8);
    SetHealth(playerB, 20);

    PlaceUnits(
        grid,
        (ranged, new HexCoordinates(0, 0)),
        (melee, new HexCoordinates(4, 1)),
        (playerA, new HexCoordinates(2, 1)),
        (playerB, new HexCoordinates(6, 4)));

    var allUnits = new List<Unit> { ranged, melee, playerA, playerB };
    var results = ExplainPlan(new List<Unit> { ranged, melee }, allUnits, grid, resolver);
    var rangedIntent = results[0].ChosenIntent;
    var meleeIntent = results[1].ChosenIntent;

    bool meleeAvoidedReservedSplash =
        rangedIntent?.Action is SplashAttackAction &&
        meleeIntent?.Action is MoveAction move &&
        !rangedIntent.TargetCells.Contains(move.Destination);

    PrintCheck("melee avoids walking into reserved splash area", meleeAvoidedReservedSplash);
    Console.WriteLine();
}

static void RunAvoidSplashOnPlannedAllyMoveSmoke()
{
    Console.WriteLine("== Avoid Splash On Planned Ally Move Smoke ==");

    var grid = new HexGrid(7, 5);
    var resolver = new ActionResolver(grid);

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

    PlaceUnits(
        grid,
        (melee, new HexCoordinates(4, 3)),
        (ranged, new HexCoordinates(0, 0)),
        (playerA, new HexCoordinates(2, 1)));

    var allUnits = new List<Unit> { melee, ranged, playerA };
    var results = ExplainPlan(new List<Unit> { melee, ranged }, allUnits, grid, resolver);
    var meleeIntent = results[0].ChosenIntent;
    var rangedResult = results[1];
    var rangedIntent = rangedResult.ChosenIntent;

    var plannedMove = meleeIntent?.Action as MoveAction;
    bool meleePlannedMove = plannedMove != null;
    bool hadSplashCandidateOnPlannedMove = false;
    if (meleePlannedMove)
    {
        foreach (var candidate in rangedResult.Candidates)
        {
            if (candidate.Intent?.Action is SplashAttackAction && candidate.Intent.TargetCells.Contains(plannedMove.Destination))
            {
                hadSplashCandidateOnPlannedMove = true;
                break;
            }
        }
    }

    bool rangedAvoidedPlannedAllyMove =
        meleePlannedMove &&
        hadSplashCandidateOnPlannedMove &&
        (rangedIntent?.Action is not SplashAttackAction || !rangedIntent.TargetCells.Contains(plannedMove.Destination));

    PrintCheck("ranged avoids splashing ally planned destination", rangedAvoidedPlannedAllyMove);
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

static void SetHealth(Unit unit, int currentHealth)
{
    unit.Stats.currentHealth = currentHealth;
}

static void PlaceUnits(HexGrid grid, params (Unit unit, HexCoordinates coordinates)[] placements)
{
    foreach (var placement in placements)
    {
        grid.PlaceUnit(placement.unit, placement.coordinates);
    }
}

static bool HitsUnit(ActionIntent intent, Unit unit)
{
    return intent != null &&
           unit != null &&
           intent.TargetCells.Contains(unit.Coordinates);
}


static void PrintCheck(string label, bool passed)
{
    Console.WriteLine($"[CHECK] {label}: {(passed ? "PASS" : "FAIL")}");
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
            Console.WriteLine($"  {i + 1}. {DescribeIntent(candidate.Intent, allUnits)} | score {candidate.Score:0.##}");
        }

        var chosenIntent = scoredCandidates[0].Intent;
        Console.WriteLine();
        Console.WriteLine("Chosen Intent:");
        PrintIntent(chosenIntent, allUnits);

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

static string DescribeIntent(ActionIntent intent, IReadOnlyList<Unit> allUnits)
{
    if (intent == null)
    {
        return "<none>";
    }

    var parts = new List<string> { intent.Action.GetType().Name };
    string targetSummary = GetTargetSummary(intent);
    if (!string.IsNullOrEmpty(targetSummary))
    {
        parts.Add($"target {targetSummary}");
    }

    string hitSummary = GetHitSummary(intent, allUnits);
    if (!string.IsNullOrEmpty(hitSummary))
    {
        parts.Add($"hits {hitSummary}");
    }

    parts.Add($"dmg {intent.PredictedDamage}");
    return string.Join(" | ", parts);
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
        planningContext.SetPlannedPosition(intent.Actor, move.Destination);

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

static void PrintIntent(ActionIntent intent, IReadOnlyList<Unit> allUnits)
{
    if (intent == null)
    {
        Console.WriteLine("No intent generated.");
        return;
    }

    Console.WriteLine($"Actor: {intent.Actor.DisplayName}");
    Console.WriteLine($"Action: {intent.Action.GetType().Name}");
    Console.WriteLine($"Target: {GetTargetSummary(intent) ?? "<none>"}");
    Console.WriteLine($"HitUnits: {GetHitSummary(intent, allUnits) ?? "<none>"}");
    Console.WriteLine($"PredictedDamage: {intent.PredictedDamage}");
    Console.WriteLine($"TargetCells: {string.Join(", ", intent.TargetCells)}");
    Console.WriteLine($"PushDestination: {(intent.PushDestination.HasValue ? intent.PushDestination.Value.ToString() : "<none>")}");
    Console.WriteLine();
}

static string GetTargetSummary(ActionIntent intent)
{
    if (intent?.Action == null)
    {
        return null;
    }

    return intent.Action switch
    {
        MoveAction move => $"dest {move.Destination}",
        MeleeAttackAction melee => melee.Target?.DisplayName ?? "<none>",
        RangedAttackAction ranged => ranged.Target?.DisplayName ?? "<none>",
        GrappleAction grapple => grapple.Target?.DisplayName ?? "<none>",
        RangedHealAction heal => heal.Target?.DisplayName ?? "<none>",
        SplashAttackAction splash => BuildAreaTargetSummary("center", splash.TargetCenter, intent.TargetUnit),
        SweepAttackAction sweep => BuildAreaTargetSummary("main", sweep.MainTarget, intent.TargetUnit),
        _ when intent.TargetUnit != null => intent.TargetUnit.DisplayName,
        _ => null
    };
}

static string BuildAreaTargetSummary(string label, HexCoordinates cell, Unit previewTarget)
{
    if (previewTarget == null)
    {
        return $"{label} {cell}";
    }

    return $"{label} {cell} | preview {previewTarget.DisplayName}";
}

static string GetHitSummary(ActionIntent intent, IReadOnlyList<Unit> allUnits)
{
    if (intent == null || allUnits == null || intent.TargetCells.Count == 0)
    {
        return null;
    }

    var hitUnits = new List<string>();

    foreach (var unit in allUnits)
    {
        if (unit == null || !unit.IsAlive)
        {
            continue;
        }

        if (intent.TargetCells.Contains(unit.Coordinates))
        {
            hitUnits.Add(unit.DisplayName);
        }
    }

    return hitUnits.Count > 0 ? string.Join(", ", hitUnits) : null;
}

readonly record struct ScoredCandidate(ActionIntent Intent, float Score);

sealed record EnemyPlanResult(Unit Enemy, IReadOnlyList<ScoredCandidate> Candidates, ActionIntent ChosenIntent);
