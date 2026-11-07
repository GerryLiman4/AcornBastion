using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] public GameStateId currentState;
    [SerializeField] public int maxRound = 3;
    [SerializeField] public int currentGold;
    [SerializeField] public int currentRound = 1;
    [Header("Round Management")]
    [SerializeField] public GameObject spawnRoot;
    [SerializeField] public MineController mineController;
    [SerializeField] public DamageTrap damageTrapPrefab;
    [Header("HUD")]
    [SerializeField] public NotificationPanel notificationUI;
    [Tooltip("End-of-level panel (CampaignOutcome prefab).")]
    [SerializeField] private CampaignHUD campaignHUD;
    [SerializeField] public CardListUI cardListUI;
    [SerializeField] public GameObject deployUI;
    [SerializeField] public WaveSkillsUI waveSkillsUI;
    [SerializeField] public TextMeshProUGUI goldText;
    [SerializeField] public TextMeshProUGUI manaText;
    [SerializeField] public TextMeshProUGUI roundText;
    [SerializeField] public GameObject gameOverUI;
    [SerializeField] public TextMeshProUGUI levelText;
    [Header("Sound")]
    [SerializeField] public AudioSource bgmSource;
    public GameObject[] buildingPathList;
    public GameObject[] enemyPath;
    public List<CreepController> creepControllerList = new List<CreepController>();
    public bool isGameOver;

    public CampaignConfig Config { get; private set; }
    public CampaignLevel Definition => Config.Level(LevelNumber);
    public CampaignRound Round => Definition.rounds[currentRound - 1];
    public int LevelNumber;
    public int Mana { get; private set; }
    public int MinerCount { get; private set; }
    public int SpawnedCount { get; private set; }
    public bool LevelComplete { get; private set; }
    public HealthController BaseHealth { get; private set; }
    public WaveSpellController Spells { get; private set; }
    public string Notice { get; private set; }
    public event Action Changed;

    private Coroutine waveSpawnCoroutine;
    private bool isSpawningWave;
    private bool shuttingDown;
    private TowerController buildPreview;
    private TilePiece previewTile;
    private UnitId previewUnitId = UnitId.None;
    private int buildTileLayerMask;
    private readonly List<TowerController> towers = new List<TowerController>();
    private readonly Dictionary<UnitId, CreepController> enemyPrefabs = new Dictionary<UnitId, CreepController>();
    private readonly Dictionary<UnitId, TowerController> towerPrefabs = new Dictionary<UnitId, TowerController>();

    private void Awake()
    {
        Config = CampaignConfig.Load();
        buildTileLayerMask = LayerMask.GetMask("Tile");
        if (GameManager.Instance == null) new GameObject("GameManager").AddComponent<GameManager>();
        LevelNumber = GameManager.Instance.CampaignLevel;
        foreach (TowerDefinition tower in Config.towers)
            towerPrefabs[(UnitId)tower.id] = Resources.Load<TowerController>("Tower/" + (UnitId)tower.id);
        foreach (EnemyDefinition enemy in Config.enemies)
            enemyPrefabs[(UnitId)enemy.id] = Resources.Load<CreepController>("Enemy/" + (UnitId)enemy.id);
    }

    private void Start()
    {
        currentGold = Definition.startingGold;
        currentRound = 1;
        maxRound = Definition.rounds.Length;
        Mana = 0;
        isGameOver = false;
        LevelComplete = false;
        BaseController playerBase = FindFirstObjectByType<BaseController>();
        BaseHealth = playerBase.hpController;
        BaseHealth.maxHp = Config.baseHealth;
        BaseHealth.Initialize(FactionId.Player);
        BaseHealth.Damaged += RefreshHud;
        SignalManager.OnPlayerBaseDestroyed += OnPlayerBaseDestroyed;
        SignalManager.OnMineralObjectDestroyed += OnMineralObjectDestroyed;
        mineController.Initalize(buildingPathList);
        mineController.SetOwner(this);
        cardListUI.Configure(this);
        Spells = gameObject.AddComponent<WaveSpellController>();
        Spells.Initialize(this);
        if (campaignHUD != null) campaignHUD.Initialize(this);
        if (waveSkillsUI != null) waveSkillsUI.Initialize(this);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (notificationUI != null) notificationUI.gameObject.SetActive(false);
        if (bgmSource != null) bgmSource.Play();
        currentState = GameStateId.None;
        SwitchState(GameStateId.Deploy);
    }

    public void SwitchState(GameStateId designatedStateId, bool canOverride = false)
    {
        if (currentState == designatedStateId && !canOverride) return;
        OnStateExited();
        currentState = designatedStateId;
        OnStateEntered();
    }

    public void OnStateEntered()
    {
        if (waveSkillsUI != null) waveSkillsUI.gameObject.SetActive(currentState == GameStateId.Wave);
        switch (currentState)
        {
            case GameStateId.Standby:
                SwitchState(GameStateId.Deploy);
                return;
            case GameStateId.Deploy:
                mineController.SpawnMinerals(Round.nodes, Definition.manaNodes);
                deployUI.SetActive(true);
                Notice = currentRound == 1 ? Definition.lesson : $"Round {currentRound - 1} clear! +{Config.clearBonus} gold. Prepare your defenses.";
                ShowStateNotification("Deploy Phase");
                break;
            case GameStateId.Wave:
                SpawnedCount = 0;
                isSpawningWave = true;
                foreach (TowerController tower in towers) if (tower != null) tower.BeginWave();
                Notice = currentRound == maxRound ? "FINAL WAVE!" : "Defend the base! Marked Grunts grant 2 mana.";
                waveSpawnCoroutine = StartCoroutine(DoRoundWave());
                ShowStateNotification("Wave Phase");
                break;
            case GameStateId.Finish:
                if (isGameOver) Notice = "The base has fallen. Retry this level with a fresh budget.";
                else if (currentRound == maxRound)
                {
                    LevelComplete = true;
                    Notice = LevelNumber == Config.levels.Length ? "CAMPAIGN COMPLETE! You held the last stand." : $"{Definition.name} complete!";
                }
                else
                {
                    currentGold += Config.clearBonus;
                    currentRound++;
                    SwitchState(GameStateId.Deploy);
                    return;
                }
                ShowStateNotification("Finish");
                break;
        }
        RefreshHud();
    }

    private Coroutine notificationRoutine;

    // Plays the phase banner without holding up the state flow; a new banner replaces one still showing.
    private void ShowStateNotification(string text)
    {
        if (notificationUI == null) return;
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(notificationUI.ShowNotification(text));
    }

    public void OnStateExited()
    {
        ClearBuildPreview();
        if (cardListUI != null) cardListUI.ResetSelected();
        if (currentState == GameStateId.Deploy)
        {
            deployUI.SetActive(false);
            mineController.ClearMinerals();
        }
        if (currentState != GameStateId.Wave) return;
        if (waveSpawnCoroutine != null) StopCoroutine(waveSpawnCoroutine);
        waveSpawnCoroutine = null;
        isSpawningWave = false;
        foreach (TowerController tower in towers) if (tower != null) tower.EndWave();
        if (Spells != null) Spells.EndWave();
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Destroy(projectile.gameObject);
    }

    public bool CanBuild(UnitId id)
    {
        TowerDefinition tower = Config.Tower(id);
        return currentState == GameStateId.Deploy && !isGameOver && tower != null
            && 
            //tower.unlockLevel <= LevelNumber && 
            currentGold >= tower.cost
            && (id != UnitId.FarmerTower || MinerCount < Definition.minerLimit);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) cardListUI.ResetSelected();
        UpdateBuildPointer(Input.mousePosition, Input.GetMouseButtonDown(0));
    }

    private void UpdateBuildPointer(Vector2 screenPosition, bool placeRequested)
    {
        UnitId id = cardListUI != null ? cardListUI.GetSelectedUnitId() : UnitId.None;
        if (!CanBuild(id) || PointerUIBlocker.IsPointerOverUI(screenPosition)) { ClearBuildPreview(); return; }
        Camera camera = Camera.main;
        Collider2D hit = camera != null ? Physics2D.OverlapPoint(camera.ScreenToWorldPoint(screenPosition), buildTileLayerMask) : null;
        TilePiece tile = hit != null ? hit.GetComponent<TilePiece>() : null;
        if (tile == null || !tile.canBeOccupied || tile.isOccupied || tile.root == null)
        { ClearBuildPreview(); return; }
        if (buildPreview == null || previewTile != tile || previewUnitId != id)
        {
            ClearBuildPreview();
            buildPreview = Instantiate(towerPrefabs[id], tile.root.transform.position, Quaternion.identity, transform);
            buildPreview.Configure(Config.Tower(id));
            buildPreview.SetBuildMode(true);
            previewTile = tile;
            previewUnitId = id;
        }
        if (placeRequested) TryPlaceTower(tile, id);
    }

    public bool TryPlaceTower(TilePiece tile, UnitId id)
    {
        if (!CanBuild(id) || tile == null || !tile.canBeOccupied || tile.isOccupied || tile.root == null
            || !Array.Exists(buildingPathList, item => item == tile.gameObject)) return false;
        TowerController tower;
        if (buildPreview != null && previewTile == tile && previewUnitId == id)
        {
            tower = buildPreview;
            buildPreview = null;
            previewTile = null;
            previewUnitId = UnitId.None;
        }
        else
        {
            ClearBuildPreview();
            tower = Instantiate(towerPrefabs[id], tile.root.transform.position, Quaternion.identity);
            tower.Configure(Config.Tower(id));
        }
        currentGold -= Config.Tower(id).cost;
        tower.SetBuildMode(false);
        tower.EndWave();
        tile.OccupyTile(tower.gameObject);
        towers.Add(tower);
        if (id == UnitId.FarmerTower) MinerCount++;
        RefreshHud();
        return true;
    }

    private void ClearBuildPreview()
    {
        if (buildPreview != null)
        {
            buildPreview.gameObject.SetActive(false);
            Destroy(buildPreview.gameObject);
        }
        buildPreview = null;
        previewTile = null;
        previewUnitId = UnitId.None;
    }

    private void OnMineralObjectDestroyed(MineralObject mineral)
    {
        if (currentState != GameStateId.Deploy || mineral == null) return;
        currentGold += Config.oreGold + MinerCount * Config.minerBonus;
        if (mineral.HasMana) AddMana(Config.oreMana);
        RefreshHud();
    }

    public void AddMana(int amount)
    {
        Mana = Mathf.Clamp(Mana + Mathf.Max(0, amount), 0, Config.manaCap);
        RefreshHud();
    }

    public bool TrySpendMana(int amount)
    {
        if (currentState != GameStateId.Wave || isGameOver || amount < 0 || Mana < amount) return false;
        Mana -= amount;
        RefreshHud();
        return true;
    }

    public void StartWave()
    {
        if (currentState != GameStateId.Deploy || isGameOver || LevelComplete) return;
        SwitchState(GameStateId.Wave);
    }

    public IEnumerator DoRoundWave()
    {
        List<CampaignSpawn> sequence = CampaignWavePlanner.Build(Round, new System.Random());
        var introduced = new HashSet<UnitId>();
        foreach (CampaignSpawn spawn in sequence)
        {
            bool firstIntroduction = currentRound == 1 && !introduced.Contains(spawn.Id)
                && ((LevelNumber == 1 && spawn.Id == UnitId.ShadowGrunt)
                    || (LevelNumber == 2 && spawn.Id == UnitId.ShadowBrute)
                    || (LevelNumber == 3 && spawn.Id == UnitId.ShadowElder));
            introduced.Add(spawn.Id);
            yield return new WaitForSeconds(Round.interval + (firstIntroduction ? 3f : 0f));
            CreepController enemy = Instantiate(enemyPrefabs[spawn.Id], spawnRoot.transform.position, Quaternion.identity);
            enemy.path = enemyPath;
            enemy.Configure(spawn.Id, Config.Enemy(spawn.Id), LevelNumber, spawn.Marked);
            enemy.Resolved += OnEnemyResolved;
            creepControllerList.Add(enemy);
            SpawnedCount++;
            RefreshHud();
            if (firstIntroduction)
            {
                SetNotice(spawn.Id == UnitId.ShadowGrunt ? "Grunt: the basic invader. Blue diamonds mark mana carriers."
                    : spawn.Id == UnitId.ShadowBrute ? "Brute: high health, 2 base damage. Use Knights at bends!"
                    : "Elder: heals nearby allies. Break the group with Area Burst!");
                yield return new WaitForSeconds(3f);
            }
        }
        isSpawningWave = false;
        waveSpawnCoroutine = null;
        CheckRoundComplete();
    }

    private void OnEnemyResolved(CreepController enemy, EnemyResolution reason)
    {
        enemy.Resolved -= OnEnemyResolved;
        creepControllerList.Remove(enemy);
        if (shuttingDown || currentState != GameStateId.Wave) return;
        if (reason == EnemyResolution.CombatKill)
        {
            currentGold += enemy.Bounty;
            if (enemy.IsMarked) AddMana(Config.markedMana);
            SignalManager.CreepGoldEarned(enemy, enemy.Bounty);
        }
        else if (reason == EnemyResolution.BaseArrival && BaseHealth != null)
            BaseHealth.GetDamaged(enemy.gameObject, enemy.damage);
        RefreshHud();
        CheckRoundComplete();
    }

    private void CheckRoundComplete()
    {
        if (!shuttingDown && !isSpawningWave && currentState == GameStateId.Wave
            && !isGameOver && creepControllerList.Count == 0) SwitchState(GameStateId.Finish);
    }

    private void OnPlayerBaseDestroyed()
    {
        isGameOver = true;
        SwitchState(GameStateId.Finish);
        foreach (CreepController enemy in creepControllerList.ToArray()) if (enemy != null) enemy.Cleanup();
    }

    public void SetNotice(string text) { Notice = text; RefreshHud(); }
    public void RefreshHud()
    {
        if (goldText != null) goldText.text = currentGold.ToString();
        if (manaText != null) manaText.text = $"{Mana}/{Config.manaCap}";
        if (roundText != null) roundText.text = $"Round {currentRound}/{maxRound}";
        if (levelText != null) levelText.text = $"Level {LevelNumber}/{Config.levels.Length}";
        Changed?.Invoke();
    }

    public void NextLevel()
    {
        if (LevelComplete && LevelNumber < Config.levels.Length) GameManager.Instance.AdvanceLevel();
    }
    public void Retry() => GameManager.Instance.LoadLevel();
    public void GoToMainMenu() => GameManager.Instance.LoadMainMenu();

    private void OnDisable()
    {
        shuttingDown = true;
        ClearBuildPreview();
        if (waveSpawnCoroutine != null) StopCoroutine(waveSpawnCoroutine);
    }
    private void OnDestroy()
    {
        SignalManager.OnPlayerBaseDestroyed -= OnPlayerBaseDestroyed;
        SignalManager.OnMineralObjectDestroyed -= OnMineralObjectDestroyed;
        if (BaseHealth != null) BaseHealth.Damaged -= RefreshHud;
        foreach (CreepController enemy in creepControllerList) if (enemy != null) enemy.Resolved -= OnEnemyResolved;
        Changed = null;
    }
}
