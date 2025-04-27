using UnityEngine;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{
    [AddComponentMenu("TopDown Engine/Character/Abilities/CharacterMindControl")]
    public class CharacterMindControl : CharacterAbility
    {
        public override string HelpBoxText() { return "Mind Control: After getting hit, this character will attack other enemies for 10 seconds instead of the player."; }

        [Header("Mind Control Settings")]
        public float MindControlDuration = 10f;
        public LayerMask EnemyLayerMask;
        public LayerMask PlayerLayerMask;
        public MMCooldown MindControlCooldown;

        [Header("Feedbacks")]
        [Tooltip("The feedbacks to play when Mind Control ends")]
        public MMFeedbacks MindControlEndFeedback;

        protected CharacterHandleWeapon _weaponHandler;
        protected AIBrain _aiBrain;
        protected AIActionMoveTowardsTarget2D _aiMoveTowardsTarget2D;
        protected AIDecisionDetectTargetRadius2D _aiDecisionDetectTargetRadius2D;
        protected Character _characterComponent;
        protected LayerMask _originalTargetLayer;

        protected bool _isMindControlled = false;
        protected bool _waitingForNewMindControl = false;

        protected override void Initialization()
        {
            base.Initialization();
            _weaponHandler = _character.FindAbility<CharacterHandleWeapon>();
            _aiBrain = _character.GetComponentInChildren<AIBrain>();

            if (_aiBrain != null)
            {
                _aiMoveTowardsTarget2D = _aiBrain.GetComponentInChildren<AIActionMoveTowardsTarget2D>();
                _aiDecisionDetectTargetRadius2D = _aiBrain.GetComponentInChildren<AIDecisionDetectTargetRadius2D>();
            }

            _characterComponent = _character;
            _health = _character.GetComponent<Health>();

            MindControlCooldown.Initialization();
        }

        protected override void OnHit()
        {
            var attacker = _health?.LastInstigator;
            base.OnHit();

            if ((attacker != null) 
                && (attacker.layer == LayerMask.NameToLayer("Player")) 
                && !_isMindControlled 
                && !_waitingForNewMindControl)
            {
                StartMindControl();
            }
        }

        protected virtual void StartMindControl()
        {
            if (_isMindControlled)
                return;

            _isMindControlled = true;
            _waitingForNewMindControl = true;

            MindControlCooldown.ConsumptionDuration = MindControlDuration;
            MindControlCooldown.Initialization();
            MindControlCooldown.Start();

            if (_aiDecisionDetectTargetRadius2D != null)
            {
                _originalTargetLayer = _aiDecisionDetectTargetRadius2D.TargetLayer;
                _aiDecisionDetectTargetRadius2D.TargetLayer = EnemyLayerMask;
            }
        }

        protected virtual void EndMindControl()
        {
            _isMindControlled = false;
            _waitingForNewMindControl = false;

            PlayAbilityStopFeedbacks();
            MindControlEndFeedback?.PlayFeedbacks(this.transform.position);

            if (_aiDecisionDetectTargetRadius2D != null)
            {
                _aiDecisionDetectTargetRadius2D.TargetLayer = _originalTargetLayer;
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();
            MindControlCooldown.Update();

            if (_isMindControlled && (MindControlCooldown.CooldownState != MMCooldown.CooldownStates.Consuming))
            {
                EndMindControl();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MindControlCooldown != null)
                MindControlCooldown.OnStateChange += OnCooldownStateChange;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (MindControlCooldown != null)
                MindControlCooldown.OnStateChange -= OnCooldownStateChange;
        }

        protected virtual void OnCooldownStateChange(MMCooldown.CooldownStates newState)
        {
            if (newState == MMCooldown.CooldownStates.Stopped)
            {
                StopStartFeedbacks();
            }
        }
    }
}