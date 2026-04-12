using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct NetworkPlayerPrototypePresentationState : INetworkSerializable, IEquatable<NetworkPlayerPrototypePresentationState>
{
    public float HorizontalSpeed;
    public float VerticalVelocity;
    public float VisualYaw;
    public float AttackAnimationSpeed;
    public int ComboIndex;
    public bool IsGrounded;
    public bool IsAttacking;
    public ushort JumpSequence;
    public ushort LandSequence;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HorizontalSpeed);
        serializer.SerializeValue(ref VerticalVelocity);
        serializer.SerializeValue(ref VisualYaw);
        serializer.SerializeValue(ref AttackAnimationSpeed);
        serializer.SerializeValue(ref ComboIndex);
        serializer.SerializeValue(ref IsGrounded);
        serializer.SerializeValue(ref IsAttacking);
        serializer.SerializeValue(ref JumpSequence);
        serializer.SerializeValue(ref LandSequence);
    }

    public bool Equals(NetworkPlayerPrototypePresentationState other)
    {
        return HorizontalSpeed.Equals(other.HorizontalSpeed)
            && VerticalVelocity.Equals(other.VerticalVelocity)
            && VisualYaw.Equals(other.VisualYaw)
            && AttackAnimationSpeed.Equals(other.AttackAnimationSpeed)
            && ComboIndex == other.ComboIndex
            && IsGrounded == other.IsGrounded
            && IsAttacking == other.IsAttacking
            && JumpSequence == other.JumpSequence
            && LandSequence == other.LandSequence;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkPlayerPrototypePresentationState other && Equals(other);
    }

    public override int GetHashCode()
    {
        int motionHash = HashCode.Combine(
            HorizontalSpeed,
            VerticalVelocity,
            VisualYaw,
            AttackAnimationSpeed,
            ComboIndex,
            IsGrounded,
            IsAttacking);

        return HashCode.Combine(
            motionHash,
            JumpSequence,
            LandSequence);
    }
}

[Serializable]
public struct NetworkPlayerPrototypeIdentityState : INetworkSerializable, IEquatable<NetworkPlayerPrototypeIdentityState>
{
    public FixedString64Bytes Nickname;
    public FixedString64Bytes HeadId;
    public FixedString64Bytes HairId;
    public FixedString64Bytes EyesId;
    public FixedString64Bytes MouthId;
    public FixedString64Bytes OutfitId;
    public FixedString64Bytes WeaponRightId;
    public FixedString64Bytes HatId;
    public FixedString64Bytes CapeId;
    public FixedString64Bytes HornsId;
    public FixedString64Bytes AccessoryId;
    public FixedString64Bytes NinjaMaskId;
    public FixedString64Bytes MustacheId;
    public Color32 HairColor;
    public Color32 SkinColor;
    public Color32 WeaponRightColor;
    public Color32 PrimaryClothingColor;
    public Color32 SecondaryClothingColor;

    public bool IsPopulated =>
        Nickname.Length > 0
        || HeadId.Length > 0
        || HairId.Length > 0
        || OutfitId.Length > 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Nickname);
        serializer.SerializeValue(ref HeadId);
        serializer.SerializeValue(ref HairId);
        serializer.SerializeValue(ref EyesId);
        serializer.SerializeValue(ref MouthId);
        serializer.SerializeValue(ref OutfitId);
        serializer.SerializeValue(ref WeaponRightId);
        serializer.SerializeValue(ref HatId);
        serializer.SerializeValue(ref CapeId);
        serializer.SerializeValue(ref HornsId);
        serializer.SerializeValue(ref AccessoryId);
        serializer.SerializeValue(ref NinjaMaskId);
        serializer.SerializeValue(ref MustacheId);
        SerializeColor(serializer, ref HairColor);
        SerializeColor(serializer, ref SkinColor);
        SerializeColor(serializer, ref WeaponRightColor);
        SerializeColor(serializer, ref PrimaryClothingColor);
        SerializeColor(serializer, ref SecondaryClothingColor);
    }

    public bool Equals(NetworkPlayerPrototypeIdentityState other)
    {
        return Nickname.Equals(other.Nickname)
            && HeadId.Equals(other.HeadId)
            && HairId.Equals(other.HairId)
            && EyesId.Equals(other.EyesId)
            && MouthId.Equals(other.MouthId)
            && OutfitId.Equals(other.OutfitId)
            && WeaponRightId.Equals(other.WeaponRightId)
            && HatId.Equals(other.HatId)
            && CapeId.Equals(other.CapeId)
            && HornsId.Equals(other.HornsId)
            && AccessoryId.Equals(other.AccessoryId)
            && NinjaMaskId.Equals(other.NinjaMaskId)
            && MustacheId.Equals(other.MustacheId)
            && HairColor.Equals(other.HairColor)
            && SkinColor.Equals(other.SkinColor)
            && WeaponRightColor.Equals(other.WeaponRightColor)
            && PrimaryClothingColor.Equals(other.PrimaryClothingColor)
            && SecondaryClothingColor.Equals(other.SecondaryClothingColor);
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkPlayerPrototypeIdentityState other && Equals(other);
    }

    public override int GetHashCode()
    {
        int idsHash = HashCode.Combine(
            HeadId.GetHashCode(),
            HairId.GetHashCode(),
            EyesId.GetHashCode(),
            MouthId.GetHashCode(),
            OutfitId.GetHashCode(),
            WeaponRightId.GetHashCode(),
            HatId.GetHashCode());

        int extrasHash = HashCode.Combine(
            CapeId.GetHashCode(),
            HornsId.GetHashCode(),
            AccessoryId.GetHashCode(),
            NinjaMaskId.GetHashCode(),
            MustacheId.GetHashCode(),
            HairColor.GetHashCode(),
            SkinColor.GetHashCode());

        return HashCode.Combine(
            Nickname.GetHashCode(),
            idsHash,
            extrasHash,
            WeaponRightColor.GetHashCode(),
            PrimaryClothingColor.GetHashCode(),
            SecondaryClothingColor.GetHashCode());
    }

    public CharacterAppearanceData ToAppearanceData()
    {
        return new CharacterAppearanceData
        {
            HeadId = ResolveValue(HeadId, "Head01_Male"),
            HairId = ResolveValue(HairId, "Hair01"),
            EyesId = ResolveValue(EyesId, "Eye01"),
            MouthId = ResolveValue(MouthId, "Mouth01"),
            OutfitId = ResolveValue(OutfitId, "Body01"),
            WeaponRightId = ResolveValue(WeaponRightId, "None"),
            HatId = ResolveValue(HatId, "None"),
            CapeId = ResolveValue(CapeId, "None"),
            HornsId = ResolveValue(HornsId, "None"),
            AccessoryId = ResolveValue(AccessoryId, "None"),
            NinjaMaskId = ResolveValue(NinjaMaskId, "None"),
            MustacheId = ResolveValue(MustacheId, "None"),
            HairColor = HairColor,
            SkinColor = SkinColor,
            WeaponRightColor = WeaponRightColor,
            PrimaryClothingColor = PrimaryClothingColor,
            SecondaryClothingColor = SecondaryClothingColor
        };
    }

    public string GetNickname(string fallback = "Player")
    {
        return ResolveValue(Nickname, fallback);
    }

    public static NetworkPlayerPrototypeIdentityState FromData(string nickname, CharacterAppearanceData appearance)
    {
        CharacterAppearanceData resolvedAppearance = appearance ?? new CharacterAppearanceData();

        return new NetworkPlayerPrototypeIdentityState
        {
            Nickname = ToFixedString(nickname, "Player"),
            HeadId = ToFixedString(resolvedAppearance.HeadId, "Head01_Male"),
            HairId = ToFixedString(resolvedAppearance.HairId, "Hair01"),
            EyesId = ToFixedString(resolvedAppearance.EyesId, "Eye01"),
            MouthId = ToFixedString(resolvedAppearance.MouthId, "Mouth01"),
            OutfitId = ToFixedString(resolvedAppearance.OutfitId, "Body01"),
            WeaponRightId = ToFixedString(resolvedAppearance.WeaponRightId, "None"),
            HatId = ToFixedString(resolvedAppearance.HatId, "None"),
            CapeId = ToFixedString(resolvedAppearance.CapeId, "None"),
            HornsId = ToFixedString(resolvedAppearance.HornsId, "None"),
            AccessoryId = ToFixedString(resolvedAppearance.AccessoryId, "None"),
            NinjaMaskId = ToFixedString(resolvedAppearance.NinjaMaskId, "None"),
            MustacheId = ToFixedString(resolvedAppearance.MustacheId, "None"),
            HairColor = (Color32)resolvedAppearance.HairColor,
            SkinColor = (Color32)resolvedAppearance.SkinColor,
            WeaponRightColor = (Color32)resolvedAppearance.WeaponRightColor,
            PrimaryClothingColor = (Color32)resolvedAppearance.PrimaryClothingColor,
            SecondaryClothingColor = (Color32)resolvedAppearance.SecondaryClothingColor
        };
    }

    private static FixedString64Bytes ToFixedString(string value, string fallback)
    {
        return new FixedString64Bytes(string.IsNullOrWhiteSpace(value) ? fallback : value.Trim());
    }

    private static string ResolveValue(FixedString64Bytes value, string fallback)
    {
        return value.Length > 0 ? value.ToString() : fallback;
    }

    private static void SerializeColor<T>(BufferSerializer<T> serializer, ref Color32 color) where T : IReaderWriter
    {
        byte r = color.r;
        byte g = color.g;
        byte b = color.b;
        byte a = color.a;

        serializer.SerializeValue(ref r);
        serializer.SerializeValue(ref g);
        serializer.SerializeValue(ref b);
        serializer.SerializeValue(ref a);

        if (serializer.IsReader)
            color = new Color32(r, g, b, a);
    }
}

[Serializable]
public struct NetworkPlayerPrototypeCombatState : INetworkSerializable, IEquatable<NetworkPlayerPrototypeCombatState>
{
    public int Strength;
    public int Dexterity;
    public int WeaponAttack;
    public float SkillMastery;
    public int ComboCounter;
    public PlayerJobType CurrentJob;

    public bool IsPopulated =>
        Strength > 0
        || Dexterity > 0
        || WeaponAttack > 0
        || SkillMastery > 0f;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Strength);
        serializer.SerializeValue(ref Dexterity);
        serializer.SerializeValue(ref WeaponAttack);
        serializer.SerializeValue(ref SkillMastery);
        serializer.SerializeValue(ref ComboCounter);

        int jobValue = (int)CurrentJob;
        serializer.SerializeValue(ref jobValue);

        if (serializer.IsReader)
            CurrentJob = (PlayerJobType)jobValue;
    }

    public bool Equals(NetworkPlayerPrototypeCombatState other)
    {
        return Strength == other.Strength
            && Dexterity == other.Dexterity
            && WeaponAttack == other.WeaponAttack
            && SkillMastery.Equals(other.SkillMastery)
            && ComboCounter == other.ComboCounter
            && CurrentJob == other.CurrentJob;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkPlayerPrototypeCombatState other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Strength,
            Dexterity,
            WeaponAttack,
            SkillMastery,
            ComboCounter,
            CurrentJob);
    }

    public PlayerCombatSnapshot ToCombatSnapshot()
    {
        return new PlayerCombatSnapshot
        {
            Strength = Strength,
            Dexterity = Dexterity,
            WeaponAttack = WeaponAttack,
            SkillMastery = SkillMastery
        };
    }

    public static NetworkPlayerPrototypeCombatState FromData(
        PlayerCombatSnapshot snapshot,
        PlayerJobType currentJob,
        int comboCounter)
    {
        return new NetworkPlayerPrototypeCombatState
        {
            Strength = snapshot.Strength,
            Dexterity = snapshot.Dexterity,
            WeaponAttack = snapshot.WeaponAttack,
            SkillMastery = snapshot.SkillMastery,
            ComboCounter = Mathf.Max(0, comboCounter),
            CurrentJob = currentJob
        };
    }
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-2100)]
public class NetworkPlayerPrototypeAvatar : NetworkBehaviour
{
    private const float PrototypePositionInterpolationTime = 0.035f;
    private const float PrototypeRotationInterpolationTime = 0.02f;

    [SerializeField] private PlayerCharacter character;
    [SerializeField] private PlayerFacade playerFacade;
    [SerializeField] private PlayerInputAdapter inputAdapter;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerAppearanceController appearanceController;
    [SerializeField] private PlayerMotor playerMotor;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private NetworkTransform networkTransform;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private MultiplayerPrototypeNameplate nameplate;
    [SerializeField] private Transform visual;
    [SerializeField] private float remoteVisualRotationSpeed = 720f;
    [SerializeField] private float localCameraFollowSmoothTime = 0.02f;
    [SerializeField] private float localCameraSnapDistance = 1.25f;
    [SerializeField] private GameBootstrap sessionBootstrap;

    private bool wasOwner;
    private ushort lastAppliedJumpSequence;
    private ushort lastAppliedLandSequence;
    private Transform ownerCameraFollowTarget;
    private Vector3 ownerCameraFollowVelocity;
    private bool ownerCameraFollowInitialized;
    private NetworkPlayerPrototypeIdentityState lastAppliedIdentityState;

    private readonly NetworkVariable<NetworkPlayerPrototypePresentationState> presentationState =
        new NetworkVariable<NetworkPlayerPrototypePresentationState>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<NetworkPlayerPrototypeIdentityState> identityState =
        new NetworkVariable<NetworkPlayerPrototypeIdentityState>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<NetworkPlayerPrototypeCombatState> combatState =
        new NetworkVariable<NetworkPlayerPrototypeCombatState>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    public void BindBootstrap(GameBootstrap bootstrap)
    {
        sessionBootstrap = bootstrap;
    }

    private void Reset()
    {
        CacheComponents();
    }

    private void Awake()
    {
        CacheComponents();

        if (!MultiplayerPrototypeRuntime.IsEnabled)
            return;

        ConfigurePrototypeNetworkTransform();
        character?.SetRuntimeLocalPlayer(false);
        SetOwnerOnlyComponentsEnabled(false);
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (!IsOwner)
        {
            ApplyRemotePresentation(presentationState.Value, false);
            TryApplyIdentityState(identityState.Value, false);
        }
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner)
            return;

        UpdateOwnerCameraFollowTarget();
        PublishPresentationState();
        PublishIdentityState();
        PublishCombatState();
        TryApplyIdentityState(identityState.Value, true);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ConfigurePrototypeNetworkTransform();
        ApplyOwnershipState();

        if (!IsOwner)
        {
            ApplyRemotePresentation(presentationState.Value, true);
            TryApplyIdentityState(identityState.Value, false, true);
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        ApplyOwnershipState();
    }

    public override void OnLostOwnership()
    {
        base.OnLostOwnership();
        ApplyOwnershipState();
    }

    public override void OnNetworkDespawn()
    {
        if (wasOwner)
            MultiplayerPrototypeCameraBinder.ClearBindings(GetCameraBindingTarget());

        appearanceController?.ClearRuntimeAppearanceOverride();
        nameplate?.SetText(string.Empty);
        DestroyOwnerCameraFollowTarget();
        character?.SetRuntimeLocalPlayer(false);
        SetOwnerOnlyComponentsEnabled(false);
        wasOwner = false;
        lastAppliedIdentityState = default;

        base.OnNetworkDespawn();
    }

    private void CacheComponents()
    {
        if (character == null)
            character = GetComponent<PlayerCharacter>();

        if (playerFacade == null)
            playerFacade = GetComponent<PlayerFacade>();

        if (inputAdapter == null)
            inputAdapter = GetComponent<PlayerInputAdapter>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (appearanceController == null)
            appearanceController = GetComponent<PlayerAppearanceController>();

        if (playerMotor == null)
            playerMotor = GetComponent<PlayerMotor>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (networkTransform == null)
            networkTransform = GetComponent<NetworkTransform>();

        if (animationController == null)
            animationController = GetComponentInChildren<PlayerAnimationController>(true);

        if (visual == null && playerFacade != null)
            visual = playerFacade.VisualTransform != null ? playerFacade.VisualTransform : transform;

        if (nameplate == null)
            nameplate = GetComponent<MultiplayerPrototypeNameplate>();

        if (nameplate == null)
            nameplate = gameObject.AddComponent<MultiplayerPrototypeNameplate>();

        nameplate.SetTarget(visual != null ? visual : transform);
    }

    private void ApplyOwnershipState()
    {
        bool isOwnerInstance = IsOwner;
        if (isOwnerInstance && sessionBootstrap == null)
            sessionBootstrap = GameBootstrap.FindReadyBootstrap();

        if (character != null)
        {
            character.SetRuntimeLocalPlayer(isOwnerInstance);
            character.SetRuntimeCharacterId(BuildRuntimeCharacterId());

            if (isOwnerInstance && sessionBootstrap != null)
                character.BindBootstrap(sessionBootstrap);
        }

        if (isOwnerInstance && playerFacade != null && sessionBootstrap != null)
            playerFacade.BindBootstrap(sessionBootstrap);

        if (isOwnerInstance && appearanceController != null && sessionBootstrap != null)
            appearanceController.BindBootstrap(sessionBootstrap);

        SetOwnerOnlyComponentsEnabled(isOwnerInstance);

        if (isOwnerInstance)
        {
            EnsureOwnerCameraFollowTarget();
            MultiplayerPrototypeCameraBinder.BindTo(GetCameraBindingTarget());
            appearanceController?.ClearRuntimeAppearanceOverride();
            appearanceController?.ApplyActiveCharacterAppearance();
            character?.RefreshPrototypeSessionState();
        }
        else
        {
            if (wasOwner)
                MultiplayerPrototypeCameraBinder.ClearBindings(GetCameraBindingTarget());

            DestroyOwnerCameraFollowTarget();
        }

        if (!isOwnerInstance)
        {
            ApplyRemotePresentation(presentationState.Value, true);
            TryApplyIdentityState(identityState.Value, false, true);
        }

        wasOwner = isOwnerInstance;
    }

    private void SetOwnerOnlyComponentsEnabled(bool isEnabled)
    {
        if (playerInput != null)
            playerInput.enabled = isEnabled;

        if (inputAdapter != null)
            inputAdapter.enabled = isEnabled;

        if (playerFacade != null)
            playerFacade.enabled = isEnabled;

        if (appearanceController != null)
            appearanceController.enabled = isEnabled;

        if (playerMotor != null)
            playerMotor.enabled = isEnabled;

        if (characterController != null)
            characterController.enabled = isEnabled;

        if (animationController != null)
            animationController.enabled = isEnabled;
    }

    private string BuildRuntimeCharacterId()
    {
        return $"client-{OwnerClientId}";
    }

    private void ConfigurePrototypeNetworkTransform()
    {
        if (networkTransform == null)
            return;

        networkTransform.AutoOwnerAuthorityTickOffset = false;
        networkTransform.PositionInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
        networkTransform.RotationInterpolationType = NetworkTransform.InterpolationTypes.Lerp;
        networkTransform.PositionLerpSmoothing = true;
        networkTransform.RotationLerpSmoothing = true;
        networkTransform.PositionMaxInterpolationTime = PrototypePositionInterpolationTime;
        networkTransform.RotationMaxInterpolationTime = PrototypeRotationInterpolationTime;
    }

    private void PublishPresentationState()
    {
        if (playerFacade == null)
            return;

        NetworkPlayerPrototypePresentationState nextState = new NetworkPlayerPrototypePresentationState
        {
            HorizontalSpeed = Quantize(playerFacade.PresentationHorizontalSpeed, 100f),
            VerticalVelocity = Quantize(playerFacade.PresentationVerticalVelocity, 100f),
            VisualYaw = Quantize(GetVisualYaw(), 10f),
            AttackAnimationSpeed = Quantize(playerFacade.PresentationAttackAnimationSpeed, 100f),
            ComboIndex = playerFacade.PresentationComboIndex,
            IsGrounded = playerFacade.PresentationIsGrounded,
            IsAttacking = playerFacade.PresentationIsAttacking,
            JumpSequence = playerFacade.JumpPresentationSequence,
            LandSequence = playerFacade.LandPresentationSequence
        };

        if (!presentationState.Value.Equals(nextState))
            presentationState.Value = nextState;
    }

    private void PublishIdentityState()
    {
        PlayerSessionCharacterApplicationService characterSession = sessionBootstrap != null ? sessionBootstrap.CharacterSession : null;
        PlayerSessionEquipmentApplicationService equipmentSession = sessionBootstrap != null ? sessionBootstrap.EquipmentSession : null;
        CharacterSaveData activeCharacter = characterSession != null ? characterSession.ActiveCharacter : null;
        CharacterAppearanceData appearance = equipmentSession != null
            ? equipmentSession.GetResolvedActiveCharacterAppearance()
            : activeCharacter != null ? activeCharacter.Appearance : null;

        string nickname = activeCharacter != null && !string.IsNullOrWhiteSpace(activeCharacter.Nickname)
            ? activeCharacter.Nickname
            : $"Player {OwnerClientId}";

        NetworkPlayerPrototypeIdentityState nextState =
            NetworkPlayerPrototypeIdentityState.FromData(nickname, appearance);

        if (!identityState.Value.Equals(nextState))
            identityState.Value = nextState;
    }

    private void PublishCombatState()
    {
        if (character == null)
            return;

        PlayerCombatSnapshot snapshot = character.GetCombatSnapshot();
        int comboCounter = playerFacade != null ? playerFacade.CurrentComboCounter : 0;

        NetworkPlayerPrototypeCombatState nextState =
            NetworkPlayerPrototypeCombatState.FromData(
                snapshot,
                character.CurrentJob,
                comboCounter);

        if (!combatState.Value.Equals(nextState))
            combatState.Value = nextState;
    }

    public bool TryGetAuthoritativeCombatState(out NetworkPlayerPrototypeCombatState resolvedState)
    {
        if (IsOwner && character != null)
        {
            resolvedState = NetworkPlayerPrototypeCombatState.FromData(
                character.GetCombatSnapshot(),
                character.CurrentJob,
                playerFacade != null ? playerFacade.CurrentComboCounter : 0);
            return true;
        }

        resolvedState = combatState.Value;
        return resolvedState.IsPopulated;
    }

    private void ApplyRemotePresentation(NetworkPlayerPrototypePresentationState state, bool snapRotation)
    {
        bool jumpedThisFrame = !snapRotation && state.JumpSequence != lastAppliedJumpSequence;
        bool landedThisFrame = !snapRotation && state.LandSequence != lastAppliedLandSequence;

        if (animationController != null)
        {
            animationController.UpdateAnimation(
                state.HorizontalSpeed,
                state.VerticalVelocity,
                state.IsGrounded,
                jumpedThisFrame,
                landedThisFrame,
                state.ComboIndex,
                state.IsAttacking,
                Mathf.Max(0.1f, state.AttackAnimationSpeed));
        }

        if (visual != null)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, state.VisualYaw, 0f);
            visual.rotation = snapRotation
                ? targetRotation
                : Quaternion.RotateTowards(
                    visual.rotation,
                    targetRotation,
                    remoteVisualRotationSpeed * Time.deltaTime);
        }

        lastAppliedJumpSequence = state.JumpSequence;
        lastAppliedLandSequence = state.LandSequence;
    }

    private void TryApplyIdentityState(
        NetworkPlayerPrototypeIdentityState state,
        bool isOwnerInstance,
        bool forceApply = false)
    {
        if (!isOwnerInstance && !state.IsPopulated)
            return;

        if (!forceApply && (!state.IsPopulated || state.Equals(lastAppliedIdentityState)))
            return;

        if (nameplate != null)
        {
            nameplate.SetTarget(visual != null ? visual : transform);
            nameplate.SetText(state.GetNickname($"Player {OwnerClientId}"));
        }

        if (appearanceController != null)
        {
            if (isOwnerInstance)
                appearanceController.ClearRuntimeAppearanceOverride();
            else
                appearanceController.SetRuntimeAppearanceOverride(state.ToAppearanceData());
        }

        lastAppliedIdentityState = state;
    }

    private float GetVisualYaw()
    {
        Transform targetVisual = visual != null ? visual : transform;
        return targetVisual.eulerAngles.y;
    }

    private void EnsureOwnerCameraFollowTarget()
    {
        if (ownerCameraFollowTarget != null)
            return;

        GameObject followTargetObject = new GameObject($"{name} [PrototypeCameraFollow]");
        ownerCameraFollowTarget = followTargetObject.transform;
        ownerCameraFollowTarget.position = transform.position;
        ownerCameraFollowTarget.rotation = Quaternion.identity;
        ownerCameraFollowInitialized = false;
        ownerCameraFollowVelocity = Vector3.zero;
    }

    private void UpdateOwnerCameraFollowTarget()
    {
        if (ownerCameraFollowTarget == null)
            return;

        Vector3 desiredPosition = transform.position;
        if (!ownerCameraFollowInitialized)
        {
            ownerCameraFollowTarget.position = desiredPosition;
            ownerCameraFollowInitialized = true;
            ownerCameraFollowVelocity = Vector3.zero;
            return;
        }

        float snapDistance = localCameraSnapDistance > 0f ? localCameraSnapDistance : 1.25f;
        if ((ownerCameraFollowTarget.position - desiredPosition).sqrMagnitude > snapDistance * snapDistance)
        {
            ownerCameraFollowTarget.position = desiredPosition;
            ownerCameraFollowVelocity = Vector3.zero;
            return;
        }

        float smoothTime = localCameraFollowSmoothTime > 0f ? localCameraFollowSmoothTime : 0.02f;
        ownerCameraFollowTarget.position = Vector3.SmoothDamp(
            ownerCameraFollowTarget.position,
            desiredPosition,
            ref ownerCameraFollowVelocity,
            smoothTime);
    }

    private Transform GetCameraBindingTarget()
    {
        return ownerCameraFollowTarget != null ? ownerCameraFollowTarget : transform;
    }

    private void DestroyOwnerCameraFollowTarget()
    {
        if (ownerCameraFollowTarget == null)
            return;

        if (ownerCameraFollowTarget.gameObject != null)
            Destroy(ownerCameraFollowTarget.gameObject);

        ownerCameraFollowTarget = null;
        ownerCameraFollowVelocity = Vector3.zero;
        ownerCameraFollowInitialized = false;
    }

    private static float Quantize(float value, float precision)
    {
        if (precision <= 0f)
            return value;

        return Mathf.Round(value * precision) / precision;
    }
}
