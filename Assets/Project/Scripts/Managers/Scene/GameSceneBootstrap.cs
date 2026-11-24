using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 씬 진입 시 새 게임과 이어하기를 구분하여 초기화
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    #region 상수
    private const float PigBankPreparationShare = 0.6f;
    private const float FragmentWeight = 5f;
    private const float CoinWeight = 2f;
    private const float HammerWeight = 2f;
    private const float SpecialCoinWeight = 1f;
    private const float GameBgmWeight = 2f;
    #endregion

    #region 내부 클래스
    private sealed class InitialPreparationStep
    {
        public readonly float Weight;
        public readonly Func<Action<float>, IEnumerator> Prepare;

        public InitialPreparationStep(float weight, Func<Action<float>, IEnumerator> prepare)
        {
            Weight = weight;
            Prepare = prepare;
        }
    }
    #endregion

    #region 프로퍼티
    public bool IsInitializationComplete { get; private set; }
    public bool IsInitializationSucceeded { get; private set; }
    #endregion

    #region 이벤트
    public event Action OnInitializationFinished;
    #endregion

    #region 이벤트 함수
    private IEnumerator Start()
    {
        IsInitializationSucceeded = false;

        // 다른 컴포넌트의 Start와 이벤트 구독이 끝날 때까지 대기
        yield return null;

        try
        {
            // Addressables 로딩
            GameDataLoader dataLoader = GameDataLoader.Instance;
            GameDataCatalog catalog = null;

            if (dataLoader == null)
            {
                Debug.LogError("GameDataLoader를 찾을 수 없음.");
                yield break;
            }

            // 게임 데이터 카탈로그 로딩
            yield return dataLoader.LoadCatalog(loadedCatalog => catalog = loadedCatalog);

            // 카탈로그 로딩 결과 확인
            if (catalog == null)
            {
                Debug.LogError("게임 데이터 카탈로그 로딩 실패");
                yield break;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager를 찾을 수 없음.");
                yield break;
            }

            if (StageManager.Instance == null)
            {
                Debug.LogError("StageManager를 찾을 수 없음.");
                yield break;
            }

            if (SaveManager.Instance == null)
            {
                Debug.LogError("SaveManager를 찾을 수 없음.");
                yield break;
            }

            int slotIndex = GameLaunchData.SlotIndex;
            GameLaunchData.ELaunchMode launchMode = GameLaunchData.LaunchMode;

            // 슬롯 번호 검증 및 현재 슬롯 설정
            if (!SaveManager.Instance.SetCurrentSlotIndex(slotIndex))
            {
                Debug.LogError("게임 실행에 필요한 저장 슬롯 정보가 없음.");
                yield break;
            }

            // Addressables 데이터 기반 Manager 초기화
            if (!InitializeManagers(catalog))
            {
                Debug.LogError("게임 데이터 초기화 실패");
                yield break;
            }

            // 2. 저장 데이터 읽기
            SaveSlotData saveData = null;
            if (launchMode == GameLaunchData.ELaunchMode.LoadGame || launchMode == GameLaunchData.ELaunchMode.OpenSkillTree)
            {
                saveData = SaveManager.Instance.LoadGame(slotIndex);
            }

            float preparationStart = 0f;
            if (launchMode == GameLaunchData.ELaunchMode.NewGame)
            {
                yield return SpawnPigBankManager.Instance.PrepareStagePoolsAsync(1, progress => dataLoader.ReportInitialPrefabPreparation(progress, 0f, PigBankPreparationShare));
                preparationStart = PigBankPreparationShare;
            }

            FragmentObjectPoolManager fragmentPool = FragmentObjectPoolManager.Instance;
            bool prepareFragments = fragmentPool != null && fragmentPool.IsEnabled;
            CoinObjectPoolManager coinPool = CoinObjectPoolManager.Instance;
            bool prepareCoins = coinPool != null && coinPool.IsEnabled;

            PlayerEquipment playerEquipment = FindObjectOfType<PlayerEquipment>();
            EquipmentData initialEquipment = ResolveInitialEquipment(playerEquipment, catalog, launchMode, saveData);
            bool prepareHammer = playerEquipment != null && initialEquipment != null;
            bool hasAddressableHammer = prepareHammer && initialEquipment.HammerPrefabReference != null && initialEquipment.HammerPrefabReference.RuntimeKeyIsValid();

            SpecialCoinPoolManager specialCoinPool = SpecialCoinPoolManager.Instance;
            bool prepareSpecialCoins = specialCoinPool != null && specialCoinPool.IsEnabled && specialCoinPool.HasAvailablePrefabs;
            AudioManager audioManager = AudioManager.Instance;
            bool prepareGameBgm = audioManager != null;

            List<InitialPreparationStep> preparationSteps = new List<InitialPreparationStep>();
            if (prepareFragments)
            {
                FragmentObjectPoolManager currentFragmentPool = fragmentPool;
                preparationSteps.Add(new InitialPreparationStep(FragmentWeight, progress => currentFragmentPool.PreparePoolsAsync(progress)));
            }

            if (prepareCoins)
            {
                CoinObjectPoolManager currentCoinPool = coinPool;
                preparationSteps.Add(new InitialPreparationStep(CoinWeight, progress => currentCoinPool.PreparePrefabsAsync(progress)));
            }

            if (prepareHammer)
            {
                PlayerEquipment currentPlayerEquipment = playerEquipment;
                EquipmentData currentInitialEquipment = initialEquipment;
                preparationSteps.Add(new InitialPreparationStep(hasAddressableHammer ? HammerWeight : 0f, progress => currentPlayerEquipment.PrepareInitialHammerAsync(currentInitialEquipment, progress)));
            }

            if (prepareSpecialCoins)
            {
                SpecialCoinPoolManager currentSpecialCoinPool = specialCoinPool;
                preparationSteps.Add(new InitialPreparationStep(SpecialCoinWeight, progress => currentSpecialCoinPool.PreparePrefabsAsync(progress)));
            }

            if (prepareGameBgm)
            {
                AudioManager currentAudioManager = audioManager;
                preparationSteps.Add(new InitialPreparationStep(GameBgmWeight, progress => currentAudioManager.PrepareBGMAsync(EAudioType.GameBGM, progress)));
            }

            float remainingWeight = 0f;
            for (int i = 0; i < preparationSteps.Count; i++)
            {
                remainingWeight += preparationSteps[i].Weight;
            }

            for (int i = 0; i < preparationSteps.Count; i++)
            {
                InitialPreparationStep currentStep = preparationSteps[i];
                float stepStart = preparationStart;
                float stepEnd = stepStart;
                if (currentStep.Weight > 0f && remainingWeight > 0f)
                {
                    stepEnd = stepStart + (1f - stepStart) * currentStep.Weight / remainingWeight;
                }

                float progressStart = stepStart;
                float progressEnd = stepEnd;
                Action<float> onProgress = progress => dataLoader.ReportInitialPrefabPreparation(progress, progressStart, progressEnd);
                yield return currentStep.Prepare(onProgress);
                preparationStart = stepEnd;
                remainingWeight -= currentStep.Weight;
            }

            dataLoader.ReportInitialPrefabPreparation(1f);
            audioManager?.PlayBGM(EAudioType.GameBGM);

            switch (launchMode)
            {
                case GameLaunchData.ELaunchMode.NewGame:
                    IsInitializationSucceeded = StartNewGame();
                    break;

                case GameLaunchData.ELaunchMode.LoadGame:
                    IsInitializationSucceeded = LoadSavedGame(slotIndex, saveData);
                    break;

                case GameLaunchData.ELaunchMode.OpenSkillTree:
                    IsInitializationSucceeded = LoadSavedGameForSkillTree(slotIndex, saveData);
                    break;

                case GameLaunchData.ELaunchMode.StartNextCycle:
                    IsInitializationSucceeded = StartNextCycle(slotIndex);
                    break;

                default:
                    Debug.LogError($"지원하지 않는 게임 실행 모드: {launchMode}");
                    break;
            }
        }
        finally
        {
            // 실행 성공 여부와 관계없이 진입 데이터 정리
            GameLaunchData.Clear();
            IsInitializationComplete = true;
            OnInitializationFinished?.Invoke();
        }
    }
    #endregion

    #region 커스텀
    /// <summary>
    /// GameDataCatalog의 정적 데이터를 각 Manager에 전달
    /// </summary>
    /// <param name="catalog"></param>
    /// <returns></returns>
    private bool InitializeManagers(GameDataCatalog catalog)
    {
        // 카탈로그 내부 데이터 검증
        if (!catalog.Validate())
        {
            Debug.LogError("GameDataCatalog 데이터 검증 실패");
            return false;
        }

        // 필요한 Manager 확인
        if (GrowthManager.Instance == null ||
            EquipmentManager.Instance == null ||
            BillManager.Instance == null ||
            LegacyShopManager.Instance == null ||
            PerkManager.Instance == null ||
            PigBankUnlock.Instance == null ||
            StageRewardManager.Instance == null ||
            PigBankObjectPoolManager.Instance == null ||
            SpawnPigBankManager.Instance == null)
        {
            Debug.LogError("게임 데이터 초기화에 필요한 Manager가 없습니다.");
            return false;
        }

        // 성장 데이터
        GrowthManager.Instance.Initialize(catalog.Upgrades, catalog.SkillSets);

        // 장비 데이터
        EquipmentManager.Instance.Initialize(catalog.EquipmentDatabase);

        // 고지서 데이터
        BillManager.Instance.Initialize(catalog.BillRules);

        // 영구 상점 데이터
        LegacyShopManager.Instance.Initialize(catalog.LegacyShopItems);

        // 퍼크 데이터
        PerkManager.Instance.ConfigurePerks(catalog.Perks);

        // 저금통 해금 데이터
        PigBankUnlock.Instance.Initialize(catalog.PigBankUnlockData);

        // 저금통 파괴 레벨 데이터
        StageRewardManager.Instance.Initialize(catalog.PigCrashLevelData);

        if (!StageRewardManager.Instance.IsInitialized)
        {
            Debug.LogError("StageRewardManager 초기화 실패.");
            return false;
        }

        // 저금통 데이터베이스 및 오브젝트 풀
        PigBankObjectPoolManager.Instance.Initialize(catalog.PigBankDatabase);

        if (!PigBankObjectPoolManager.Instance.IsInitialized)
        {
            Debug.LogError("PigBankObjectPoolManager 초기화 실패.");
            return false;
        }

        // 저금통 스폰 데이터
        SpawnPigBankManager.Instance.Initialize(catalog.PigBankDatabase);

        if (!SpawnPigBankManager.Instance.IsInitialized)
        {
            Debug.LogError("SpawnPigBankManager 초기화 실패.");
            return false;
        }

        Debug.Log("모든 Manager 데이터 초기화 완료");

        return true;
    }

    private EquipmentData ResolveInitialEquipment(PlayerEquipment playerEquipment, GameDataCatalog catalog, GameLaunchData.ELaunchMode launchMode, SaveSlotData saveData)
    {
        EquipmentData initialEquipment = playerEquipment != null ? playerEquipment.StartingEquipment : null;

        if (launchMode != GameLaunchData.ELaunchMode.LoadGame && launchMode != GameLaunchData.ELaunchMode.OpenSkillTree)
        {
            return initialEquipment;
        }

        string equipmentId = saveData != null && saveData.runData != null && saveData.runData.equipmentData != null ? saveData.runData.equipmentData.equippedEquipmentId : null;

        EquipmentData savedEquipment = catalog.EquipmentDatabase != null ? catalog.EquipmentDatabase.FindEquipmentById(equipmentId) : null;

        bool savedHammerIsAddressable = savedEquipment != null && savedEquipment.HammerPrefabReference != null && savedEquipment.HammerPrefabReference.RuntimeKeyIsValid();

        return savedHammerIsAddressable ? savedEquipment : initialEquipment;
    }

    /// <summary>
    /// 새로운 런을 시작합니다.
    /// </summary>
    private bool StartNewGame()
    {
        GameManager.Instance.StartRun();

        // StageManager가 OnRunStarted를 구독하고 있으므로
        // StartRun에서 첫 번째 스테이지가 자동으로 시작됨.
        return true;
    }

    /// <summary>
    /// [SCENE-02] 상점에서 돌아온 다음 사이클의 영구 데이터 복원 후 새 런 시작
    /// </summary>
    private bool StartNextCycle(int slotIndex)
    {
        SaveManager saveManager = SaveManager.Instance;
        CurrencyManager currencyManager = CurrencyManager.Instance;
        GameManager gameManager = GameManager.Instance;
        GameSaveController saveController = GameSaveController.Instance;

        // 필수 매니저 확인
        if (currencyManager == null || saveController == null)
        {
            Debug.LogError("다음 사이클 시작에 필요한 매니저가 없음.");
            return false;
        }

        // 저장 데이터 로드 및 검증
        SaveSlotData saveData = saveManager.LoadGame(slotIndex);

        if (saveData == null || saveData.playerData == null)
        {
            Debug.LogError("다음 사이클 저장 데이터 로드 실패.");
            return false;
        }

        // 복원 작업 시작
        saveController.SetRestoringSaveData(true);

        try
        {
            // 레거시 포인트 복원
            if (!currencyManager.RestoreLegacyPoints(saveData.playerData.legacyPoints))
            {
                Debug.LogError("레거시 포인트 복원 실패.");
                return false;
            }

            // 파산 사이클 수 복원
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.RestoreCycleCount(saveData.playerData.bankruptcyCycle);
            }

            // 영구 성장 데이터 복원
            LegacyShopManager.Instance.ImportSaveData(saveData.playerData.permanentGrowth);

            // 이전 런 데이터 초기화
            currencyManager.ResetRunGold();

            GrowthManager.Instance.ResetRunGrowth();

            PerkManager.Instance.ResetRunPerks();

            BillManager.Instance.ResetRunBills();
        }
        finally
        {
            // 복원 성공 여부와 관계없이 플래그 해제
            saveController.SetRestoringSaveData(false);
        }

        // 새로운 런 시작
        // StageManager가 1스테이지를 자동으로 시작
        gameManager.StartRun();

        Debug.Log($"다음 사이클 런 시작: {saveData.playerData.bankruptcyCycle}");
        return true;
    }

    /// <summary>
    /// 저장 데이터를 검증한 뒤 게임 상태를 순서대로 복원
    /// </summary>
    private bool LoadSavedGame(int slotIndex, SaveSlotData saveData)
    {
        SaveManager saveManager = SaveManager.Instance;
        CurrencyManager currencyManager = CurrencyManager.Instance;
        GameManager gameManager = GameManager.Instance;
        StageManager stageManager = StageManager.Instance;
        GameSaveController saveController = GameSaveController.Instance;
        UIManager uiManager = UIManager.Instance;

        // 필요한 매니저 검증
        if (uiManager == null)
        {
            Debug.LogError("게임 데이터 복원에 필요한 매니저가 없음.");
            return false;
        }

        if (!RestoreSavedGameData(saveData, slotIndex, "이어하기", "게임 데이터 복원에 필요한 매니저가 없음.", saveManager, currencyManager, gameManager, stageManager, saveController, () =>
        {
            // 저장 당시 스테이지 상태에 따라 복원
            if (saveData.runData.isStageInProgress)
            {
                // 진행 중이던 스테이지를 처음부터 다시 시작
                stageManager.StartStage(saveData.runData.currentStage);

                Debug.Log($"진행 중이던 스테이지 재시작: {saveData.runData.currentStage}");
            }
            else
            {
                // 클리어 후 결과 화면 상태 복원
                stageManager.RestoreStageClear(saveData.runData.currentStage);

                Debug.Log($"스테이지 클리어 데이터 복원: {saveData.runData.currentStage}");
            }
        }, null))
        {
            return false;
        }

        if (!saveData.runData.isStageInProgress)
        {
            uiManager.ShowSkillTreeFromMainMenu();
        }

        Debug.Log($"이어하기 데이터 복원 완료: " + $"스테이지 {saveData.runData.currentStage}, " + $"골드 {currencyManager.Gold}, " + $"레거시 포인트 {currencyManager.LegacyPoints}");
        return true;
    }

    /// <summary>
    /// 저장 데이터를 복원한 뒤 스테이지를 시작하지 않고 스킬트리 화면 표시
    /// </summary>
    private bool LoadSavedGameForSkillTree(int slotIndex, SaveSlotData saveData)
    {
        SaveManager saveManager = SaveManager.Instance;
        CurrencyManager currencyManager = CurrencyManager.Instance;
        UIManager uiManager = UIManager.Instance;
        GameManager gameManager = GameManager.Instance;
        StageManager stageManager = StageManager.Instance;
        GameSaveController saveController = GameSaveController.Instance;

        // 필요한 매니저 검증
        if (uiManager == null)
        {
            Debug.LogError("스킬트리 진입에 필요한 매니저가 없음.");
            return false;
        }

        if (!RestoreSavedGameData(saveData, slotIndex, "스킬트리 진입", "스킬트리 진입에 필요한 매니저가 없음.", saveManager, currencyManager, gameManager, stageManager, saveController, () => stageManager.RestoreStageProgress(saveData.runData.currentStage), () =>
        {
            // 스킬트리 전용 모드 설정
            saveController.SetSkillTreeOnlyMode(saveData.runData.isStageInProgress);

            // 스킬트리 화면 진입을 위한 게임 상태 설정
            gameManager.ChangeGameState(EGameState.StageClear);
        }))
        {
            return false;
        }

        // 복원이 끝난 뒤 스킬트리 화면 표시
        uiManager.ShowSkillTreeFromMainMenu();

        Debug.Log($"스킬트리 진입 완료: 슬롯 {slotIndex}, " + $"스테이지 {saveData.runData.currentStage}, " + $"골드 {currencyManager.Gold}, " + $"레거시 포인트 {currencyManager.LegacyPoints}");
        return true;
    }

    private bool RestoreSavedGameData(SaveSlotData saveData, int slotIndex, string launchModeName, string managerErrorMessage, SaveManager saveManager, CurrencyManager currencyManager, GameManager gameManager, StageManager stageManager, GameSaveController saveController, Action restoreStageState, Action restoreAfterBillState)
    {
        // 필요한 매니저 검증
        if (saveManager == null || currencyManager == null || gameManager == null || stageManager == null || saveController == null)
        {
            Debug.LogError(managerErrorMessage);
            return false;
        }

        // 저장 데이터 검증
        if (saveData == null || saveData.runData == null || saveData.playerData == null)
        {
            Debug.LogError($"저장 슬롯 {slotIndex}의 데이터가 올바르지 않음.");
            return false;
        }

        if (!saveData.runData.isRunActive || saveData.runData.currentStage <= 0)
        {
            Debug.LogError($"{launchModeName}에 필요한 런 또는 스테이지 정보가 올바르지 않음.");
            return false;
        }

        // 모든 검증이 끝난 뒤 복원 시작
        saveController.SetRestoringSaveData(true);

        try
        {
            // 재화 복원
            if (!currencyManager.RestoreBalances(saveData.runData.currentGold, saveData.playerData.legacyPoints))
            {
                Debug.LogError("재화 데이터 복원에 실패.");
                return false;
            }

            // 파산 사이클 수 복원
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.RestoreCycleCount(saveData.playerData.bankruptcyCycle);
            }

            // 성장 및 장비 데이터 복원
            saveManager.NotifyGameLoaded(saveData);

            // 저금통 해금 및 파괴 레벨 데이터 복원
            if (saveData.runData.piggyBankData != null)
            {
                PigBankUnlock.Instance.ImportSaveData(saveData.runData.piggyBankData);
                StageRewardManager.Instance.ImportSaveData(saveData.runData.piggyBankData);
            }

            // 런 활성 상태 복원
            gameManager.RestoreRunState();
            restoreStageState?.Invoke();

            if (saveData.runData.hasBillSaveData)
            {
                BillManager.Instance.ImportSaveData(saveData.runData.billData);
            }

            restoreAfterBillState?.Invoke();
        }
        finally
        {
            saveController.SetRestoringSaveData(false);
        }

        return true;
    }
    #endregion

}
