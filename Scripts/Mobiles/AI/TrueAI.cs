#region Header
// **********
// ServUO - TrueAI.cs
// **********
#endregion

#region References
using System;
using System.Collections.Generic;
using System.IO;

using Server.ContextMenus;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;
using Server.Engines.XmlSpawner2;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.Targeting;
using Server.Targets;
using System.Linq;

using MoveImpl = Server.Movement.MovementImpl;
#endregion

namespace Server.Mobiles
{
	public abstract class TrueAI : BaseAI
	{
		private bool m_IsFleeing;
		private bool m_IsTooFar;
		private bool m_WasTooNear;
		private bool m_WasHiding;
		private bool m_IsSetUp;
		private long m_NextAidTime;
		private long m_NextAttackTime;
		private long m_NextCastTime;
		private long m_NextHealTime;
		private long m_NextCureTime;
		private long m_NextDecurseTime;
		private long m_NextSkillTime;
		private long m_NextStopGuard;
		private long m_NextTurn;
		private long m_SpellDelay;
		private long m_StallTime;
		private long m_WayPointTime;
		private long m_WanderTime;
		private long m_ShotClock;
		private long m_LockClock;
		private long m_NextEquip;
		private long m_NextEquipPassive;
		private long m_NextAttackerTime;
		private int m_AbsMinRange;
		private int m_Combo;
		private int m_MaxRange;
		private int m_MinRange;

		private Spell m_CurrentSpell;
        private Point3D m_LastTargetLoc;
        private LandTarget m_RevealTarget;

		private IDamageable m_LastSpellTarget;
        private IDamageable m_LastTarget;

		public bool IsFleeing { get { return m_IsFleeing; } set { m_IsFleeing = value; } }
		public bool IsTooFar { get { return m_IsTooFar; } set { m_IsTooFar = value; } }
		public bool WasTooNear { get { return m_WasTooNear; } set { m_WasTooNear = value; } }
		public bool WasHiding { get { return m_WasHiding; } set { m_WasHiding = value; } }
		public bool IsSetUp { get { return m_IsSetUp; } set { m_IsSetUp = value; } }
		public long NextAidTime { get { return m_NextAidTime; } set { m_NextAidTime = value; } }
		public long NextAttackTime { get { return m_NextAttackTime; } set { m_NextAttackTime = value; } }
		public long NextCastTime { get { return m_NextCastTime; } set { m_NextCastTime = value; } }
		public long NextHealTime { get { return m_NextHealTime; } set { m_NextHealTime = value; } }
		public long NextCureTime { get { return m_NextCureTime; } set { m_NextCureTime = value; } }
		public long NextDecurseTime { get { return m_NextDecurseTime; } set { m_NextDecurseTime = value; } }
		public long NextSkillTime { get { return m_NextSkillTime; } set { m_NextSkillTime = value; } }
		public long NextStopGuard { get { return m_NextStopGuard; } set { m_NextStopGuard = value; } }
		public long NextTurn { get { return m_NextTurn; } set { m_NextTurn = value; } }
		public long StallTime { get { return m_StallTime; } set { m_StallTime = value; } }
		public long WayPointTime { get { return m_WayPointTime; } set { m_WayPointTime = value; } }
		public long WanderTime { get { return m_WanderTime; } set { m_WanderTime = value; } }
		public long ShotClock { get { return m_ShotClock; } set { m_ShotClock = value; } }
		public long SpellDelay { get { return m_SpellDelay; } set { m_SpellDelay = value; } }
		public long LockClock { get { return m_LockClock; } set { m_LockClock = value; } }
		public long NextEquip { get { return m_NextEquip; } set { m_NextEquip = value; } }
		public long NextEquipPassive { get { return m_NextEquipPassive; } set { m_NextEquipPassive = value; } }
		public long NextAttackerTime { get { return m_NextAttackerTime; } set { m_NextAttackerTime = value; } }
		public int AbsMinRange { get { return m_AbsMinRange; } set { m_AbsMinRange = value; } }
		public int Combo { get { return m_Combo; } set { m_Combo = value; } }
		public int MaxRange { get { return m_MaxRange; } set { m_MaxRange = value; } }
		public int MinRange { get { return m_MinRange; } set { m_MinRange = value; } }

		public Spell CurrentSpell { get { return m_CurrentSpell; } set { m_CurrentSpell = value; } }
		public Point3D LastTargetLoc { get { return m_LastTargetLoc; } set { m_LastTargetLoc = value; } }
		public LandTarget RevealTarget { get { return m_RevealTarget; } set { m_RevealTarget = value; } }

		public IDamageable LastSpellTarget { get { return m_LastSpellTarget; } set { m_LastSpellTarget = value; } }
		public IDamageable LastTarget { get { return m_LastTarget; } set { m_LastTarget = value; } }

		public virtual int CheckAttackerDelay { get { return 5000; } }
		public virtual int CheckCureDelay { get { return 5000; } }
		public virtual int CheckHealDelay { get { return 5000; } }
		public virtual int CheckAidDelay { get { return 5000; } }

		public TrueAI(BaseCreature m)
			: base(m)
		{
		}


		public override bool Think()
		{
			if (m_Mobile.Deleted)
			{
				return false;
			}

			if (CheckCharming())
				return true;

			switch (Action)
			{
				case ActionType.Wander:
					m_Mobile.OnActionWander();
					return DoActionWander();

				case ActionType.Combat:
					m_Mobile.OnActionCombat();
					return DoActionCombat();

				case ActionType.Guard:
					m_Mobile.OnActionGuard();
					return DoActionGuard();

				case ActionType.Flee:
					m_Mobile.OnActionFlee();
					return DoActionFlee();

				case ActionType.Interact:
					m_Mobile.OnActionInteract();
					return DoActionInteract();

				case ActionType.Backoff:
					m_Mobile.OnActionBackoff();
					return DoActionBackoff();

				default:
					return false;
			}
		}

		public override void OnActionChanged()
		{
			switch (Action)
			{
				case ActionType.Wander:
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					m_Mobile.FocusMob = null;
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					CheckNavPoint();
					break;
				case ActionType.Combat:
					m_Mobile.Warmode = true;
					m_Mobile.FocusMob = null;
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					break;
				case ActionType.Guard:
					m_Mobile.Warmode = true;
					m_Mobile.FocusMob = null;
					m_Mobile.Combatant = null;
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_NextStopGuard = Core.TickCount + 10000;
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					break;
				case ActionType.Flee:
					m_Mobile.Warmode = true;
					m_Mobile.FocusMob = null;
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					break;
				case ActionType.Interact:
					m_Mobile.Warmode = false;
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					break;
				case ActionType.Backoff:
					m_Mobile.Warmode = false;
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					break;
			}
		}

		public override bool DoActionWander()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;
			int rp = m.RangePerception;

			//If I don't have a home, WanderTime isn't used
//			ResetRecall();

			//If I already have a combatant, fight him
			if(HasCombatant())
			{
				m.DebugSay("My combatant is already {0}", m.Combatant.Name);
				ShotClock = Core.TickCount;
				Action = ActionType.Combat;
				return true;
			}

			//If I can find a combatant, fight him
			if(CheckTarget())
			{
				m.DebugSay("I am going to attack {0}", m.Combatant.Name);
				ShotClock = Core.TickCount;
				Action = ActionType.Combat;
				return true;
			}

			//lol herding check
			if (CheckHerding())
				return true;

			//If I'm animated dead, shamble towards my master
			if (CheckShamble())
				return true;

			// Do I have a waypoint? Should I do stuff with it?
//			CheckWayPoint();

			// Should I recall?
//			if (CheckRecall())
//				return true;

            // Should I check special abilities?
			if (CheckWanderAbility())
                return true;

			// Should I move about?
			CheckWander();
			return true;
		}

		public override bool DoActionCombat()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Do I lack a combatant?
			if (!CheckCombatant())
			{
				m.DebugSay ("Lost combatant, guarding!");
				m.Combatant = null;
				Action = ActionType.Guard;
				return true;
			}

			// Is my combatant hurting me too much?
			if (CheckFlee())
			{
				m.DebugSay("I'm fleeing!");
				IsFleeing = true;
				Action = ActionType.Flee;
				return true;
			}

			DoCombatMove();
            DoCombatAbility();

			return true;
		}

		public override bool DoActionGuard()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Do I have over 50% health?
			if (CheckStopFlee())
				IsFleeing = false;

			// I already have a Combatant!
			if (HasCombatant())
			{
				m.DebugSay("My combatant is already {0}", m.Combatant.Name);
				ShotClock = Core.TickCount;
				if (IsFleeing)
					Action = ActionType.Flee;
				else
					Action = ActionType.Combat;
				return true;
			}

			// Is there an attacker already lurking nearby?
			if (CheckAttacker())
			{
				m.DebugSay("Guarding against {0}!", m.Combatant.Name);
				ShotClock = Core.TickCount;
				if (IsFleeing)
					Action = ActionType.Flee;
				else
					Action = ActionType.Combat;
				return true;
			}

			// Is there a valid target nearby?
			if (CheckTarget())
			{
				m.DebugSay("I am going to attack {0}!", m.Combatant.Name);
				ShotClock = Core.TickCount;
				if (IsFleeing)
					Action = ActionType.Flee;
				else
					Action = ActionType.Combat;
				return true;
			}

			// Is it time to wander around?
			if (Core.TickCount >= NextStopGuard)
			{
				m_Mobile.DebugSay("I stopped being on guard");
				IsFleeing = false;
				WanderTime = NewTime(60);
				Action = ActionType.Wander;
				return true;
			}

            CheckWanderAbility();

			if (Utility.Random(100) == 0)
				m_Mobile.DebugSay("I'm on guard");

			return true;
		}

		public override bool DoActionFlee()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Do I need to stop fleeing?
			if (CheckStopFlee())
				IsFleeing = false;

			// Is there a more immediate threat?
			if (CheckAttacker())
			{
				m.DebugSay("I'm scared of {0}!", m.Combatant.Name);
				ShotClock = Core.TickCount;
			}

			// No combatant
			if (!HasCombatant())
			{
				m.DebugSay("I have lost him");
				m.Combatant = null;
				Action = ActionType.Guard;
				return true;
			}

			// Is my target eluding me?
			if (!CheckShotClock())
			{
				m.DebugSay("My target eludes me");
				m.Combatant = null;
				Action = ActionType.Guard;
				return true;
			}

			// Am I ready to fight?
			if (!IsFleeing)
			{
				m_Mobile.DebugSay("I am stronger now");
				ShotClock = Core.TickCount;
				Action = ActionType.Combat;
				return true;
			}

			DoFleeMove();
			return true;
		}

		public override bool DoActionInteract()
		{
			return true;
		}

		public override bool DoActionBackoff()
		{
			return true;
		}

		public override bool Obey()
		{
			if (m_Mobile.Deleted)
			{
				return false;
			}

			switch (m_Mobile.ControlOrder)
			{
				case OrderType.None:
					return DoOrderNone();

				case OrderType.Come:
					return DoOrderCome();

				case OrderType.Drop:
					return DoOrderDrop();

				case OrderType.Friend:
					return DoOrderFriend();

				case OrderType.Unfriend:
					return DoOrderUnfriend();

				case OrderType.Guard:
					return DoOrderGuard();

				case OrderType.Attack:
					return DoOrderAttack();

				case OrderType.Patrol:
					return DoOrderPatrol();

				case OrderType.Release:
					return DoOrderRelease();

				case OrderType.Stay:
					return DoOrderStay();

				case OrderType.Stop:
					return DoOrderStop();

				case OrderType.Follow:
					return DoOrderFollow();

				case OrderType.Transfer:
					return DoOrderTransfer();
/*
				case OrderType.Aggro:
					return CheckFamiliar();

				case OrderType.Heel:
					return CheckFamiliar();

				case OrderType.Fetch:
					return DoOrderFetch();
*/
				default:
					return false;
			}
		}

		public override void OnCurrentOrderChanged()
		{
			if (m_Mobile.Deleted || m_Mobile.ControlMaster == null || m_Mobile.ControlMaster.Deleted)
			{
				return;
			}

			switch (m_Mobile.ControlOrder)
			{
				case OrderType.None:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.Home = m_Mobile.Location;
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Come:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Drop:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = true;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Friend:
				case OrderType.Unfriend:
					m_Mobile.ControlMaster.RevealingAction();
					break;
				case OrderType.Guard:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = true;
					m_Mobile.Combatant = null;
					m_Mobile.ControlTarget = null;
					string petname = String.Format("{0}", m_Mobile.Name);
					m_Mobile.ControlMaster.SendLocalizedMessage(1049671, petname); //~1_PETNAME~ is now guarding you.
					break;
				case OrderType.Attack:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = true;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Patrol:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Release:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Stay:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Stop:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.Home = m_Mobile.Location;
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Follow:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Transfer:
					m_Mobile.ControlMaster.RevealingAction();
					m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
/*				case OrderType.Aggro:
					m_Mobile.DebugSay("I aggro for my master");
					m_Mobile.CurrentSpeed = 0.1;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = true;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Heel:
					m_Mobile.DebugSay("I heel to my master");
					m_Mobile.CurrentSpeed = 0.01;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
				case OrderType.Fetch:
					m_Mobile.DebugSay("I fetch for my master");
					m_Mobile.CurrentSpeed = 0.3;
					m_Mobile.PlaySound(m_Mobile.GetIdleSound());
					m_Mobile.Warmode = false;
					m_Mobile.Combatant = null;
					break;
*/			}
		}

		public virtual bool HasCombatant()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;

			// I have no combatant!
			if (c == null)
				return false;

			// I can't harm my combatant!
			if (!m.CanBeHarmful(c))
				return false;

			// My combatant is on another map!
			if (c.Map != m.Map)
				return false;

			// My combatant is out of chase range!
			if (!m.InRange(c, m.RangePerception * 2))
				return false;

			// I have a combatant!
			return true;
		}

		public virtual bool CheckTarget()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Look for those who are valid targets
			if (AcquireFocusMob(m.RangePerception, m.FightMode))
			{
				m.Combatant = m.FocusMob;
				m.FocusMob = null;
				return true;
			}
			return false;
		}

        public override bool CheckHerding()
		{
			IPoint2D target = m_Mobile.TargetLocation;

			if (target == null)
			{
				return false; // Creature is not being herded
			}

			double distance = m_Mobile.GetDistanceToSqrt(target);

			if (distance < 1 || distance > 15)
			{
				m_Mobile.TargetLocation = null;
				return false; // At the target or too far away
			}

			DoMove(m_Mobile.GetDirectionTo(target));

			return true;
		}

		public virtual bool CheckShamble()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.IsAnimatedDead)
			{
				// animated dead follow their master
				Mobile master = m.SummonMaster;

				if (master != null && master.Map == m.Map && master.InRange(m, m.RangePerception * 2))
					MoveTo(master, false, 1);
				else
					WalkRandomInHome(2, 2, 1);
				if (Utility.Random(100) == 0)
					m.DebugSay("Shamble, shamble shamble!");
				return true;
			}
			return false;
		}

		public virtual bool CheckWanderAbility()
		{
			return false;
		}

		public virtual bool CheckWander()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			//If it's past time to start moving after getting to a waypoint
			if (Core.TickCount >= StallTime)
			{
				WayPoint point = m.CurrentWayPoint;
				// Is my current waypoint valid?
				//If I should walk to my waypoint
				if (ValidWayPoint(point) && Utility.Random(100) >= 80)//point.WanderChance)
				{
					if (Utility.Random(100) == 0)
						m.DebugSay("I will walk towards my waypoint");
					WalkWaypoint(point);
				}
				//If nothing else...
				else if (CheckMove() && !m.CheckIdle())
				{
					if (Utility.Random(100) == 0)
						m.DebugSay("I will wander randomly");
					WalkRandom(2, 2, 1);
				}
				return true;
			}
			return false;
		}

		public virtual bool CheckCombatant()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Do I lack a combatant?
			if (!HasCombatant())
			{
				if (CheckTarget())
				{
					m.DebugSay("No combatant, I am going to attack {0}", m.Combatant.Name);
					ShotClock = Core.TickCount;
					return true;
				}
				return false;
			}

			// Is my target eluding me?
			if (!CheckShotClock())
			{
				if (CheckTarget())
				{
					m.DebugSay("Combatant eluded, I am going to attack {0}", m.Combatant.Name);
					ShotClock = Core.TickCount;
					return true;
				}
				return false;
			}

			// Should I find a better target?
			if (BadTarget())
			{
				if (CheckTarget())
				{
					m.DebugSay("Bad combatant, I am going to attack {0}", m.Combatant.Name);
					ShotClock = Core.TickCount;
				}
			}

			return true;
		}

		public virtual bool CheckAttacker()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// Look for those already attacking me
			if(AcquireAttacker(m.RangePerception))
			{
				m.Combatant = m.FocusMob;
				m.FocusMob = null;
				return true;
			}
			return false;
		}

		public virtual bool CheckStopFlee()
		{
			// More than 50% of my health
			if (HighHits())
				return true;

			// Keep fleeing!
			return false;
		}

		public override bool CheckFlee()
		{
			// Not a coward
			if (!Coward())
				return false;

			// More than 20% of my health
			if (!LowHits())
				return false;

			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;

			// Random check failed
			if (Utility.Random(100) >= 10 + Math.Max(0, c.Hits - m.Hits))
				return false;

			// Flee!
			return true;
		}

		public virtual bool Coward()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// I am a summon!
			if (m.Summoned)
				return false;

			// I am a player pet!
			if (m.Controlled)
				return false;

			// I can not flee!
//			if (!m.CanFlee)
//				return false;

			// I am afraid!
			return true;
		}

		public virtual bool BadTarget()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;

			// They cannot be harmed!
			if (m.Blessed)
				return true;

			// They are not close!
			if (!m.InRange(c, m.RangePerception))
				return true;

			// I can not see them!
			if (!m.CanSee(c))
				return true;

			// I can not see them!
			if (!m.InLOS(c))
				return true;

			// My target is fine!
			return false;
		}

		public virtual void DoCombatMove()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;
			Point3D locus = m.Location;

			// Did I reach my combatant?
			if (WalkMobileRange(c, m.RangeFight))
				return;

			// Did I fail to move?
			if (locus == m.Location)
			{
				if (OnFailedMove())
				{
					m.DebugSay("My move is blocked, so I am going to attack {0}", m.Combatant.Name);
					ShotClock = Core.TickCount;
					return;
				}
			}

			m_LastTarget = m.Combatant;
			m_LastTargetLoc = m_LastTarget.Location;

			if (Utility.Random(100) == 0)
				m.DebugSay("I should be closer to {0}", c.Name);
		}

		public virtual void DoCombatAbility()
		{
		}

		public virtual bool OnFailedMove()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.DisallowAllMoves)
				return false;

			if (AcquireFocusMob(m.RangeFight, m.FightMode))
			{
				m.Combatant = m.FocusMob;
				m.FocusMob = null;
				return true;
			}

			return false;
		}

		public override double TransformMoveDelay(double delay)
		{
			bool isPassive = (delay == m_Mobile.PassiveSpeed);
			bool isControlled = (m_Mobile.Controlled || m_Mobile.Summoned);

			if (m_Mobile.Controlled)
			{
				if (m_Mobile.ControlOrder == OrderType.Follow && m_Mobile.ControlTarget == m_Mobile.ControlMaster)
					delay *= 0.5;

				delay -= 0.075;
			}

			if (m_Mobile.ReduceSpeedWithDamage || m_Mobile.IsSubdued)
			{
				double offset = (double)(m_Mobile.Hits / m_Mobile.HitsMax * 0.4);
				if (offset < 0)
					offset = 0;
				else if (offset > 0.2)
					offset = 0.2;
				offset = 0.2 - offset;
				//+0.00 at 50%
				//+0.04 at 40%
				//+0.08 at 30%
				//+0.10 at 25%
				//+0.12 at 20%
				//+0.16 at 10%
				//+0.20 at 0%

				delay += offset;
			}

			if (delay < 0.05)
				delay = 0.05;

			if (double.IsNaN(delay))
			{
				using (StreamWriter op = new StreamWriter("nan_transform.txt", true))
				{
					op.WriteLine(String.Format("NaN in TransformMoveDelay: {0}, {1}, {2}, {3}", DateTime.UtcNow, this.GetType().ToString(), m_Mobile == null ? "null" : m_Mobile.GetType().ToString(), m_Mobile.HitsMax));
				}

				return 1.0;
			}

			return delay;
		}

		public virtual void DoFleeMove()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;
			int cd = (int) m.GetDistanceToSqrt(c);
			int rp = m.RangePerception;

			// If I'm in spell range, get out of spell range
			if (cd <= 10)
				WalkMobileRange(c, 11);
			// If I'm in perception range, get out of perception range
			else if (cd <= rp)
				WalkMobileRange(c, rp + 1);
			// If I'm in chase range, get out of chase range
			else if (cd <= rp * 2)
				WalkMobileRange(c, rp * 2 + 1);

			if (Utility.Random(100) == 0)
				m.DebugSay("I am fleeing!");
		}

		public virtual bool ShouldRun()
		{
			if (m_Mobile.AllowedStealthSteps > 0)
				return false;

			int delay = (int)(TransformMoveDelay(m_Mobile.CurrentSpeed) * 1000);

			bool mounted = m_Mobile.Mounted || m_Mobile.Flying;
			bool running = (mounted && delay <= Mobile.WalkMount) || (!mounted && delay <= Mobile.WalkFoot);

			return running; //m_Mobile.CanRun;
		}

		public virtual bool WalkMobileRange(IDamageable f, int iWantDist)
		{
			bool bRun = ShouldRun();
			return WalkMobileRange(f, 1, bRun, iWantDist, iWantDist);
		}

		public virtual bool WalkMobileRange(IDamageable f, int iWantDistMin, int iWantDistMax)
		{
			bool bRun = ShouldRun();
			return WalkMobileRange(f, 1, bRun, iWantDistMin, iWantDistMax);
		}

		public virtual bool WalkMobileRange(IDamageable f, bool bRun, int iWantDistMin, int iWantDistMax)
		{
			return WalkMobileRange(f, 1, bRun, iWantDistMin, iWantDistMax);
		}

		public virtual bool WalkMobileRange(IDamageable f, int iSteps, bool bRun, int iWantDistMin, int iWantDistMax)
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.Deleted)
				return false;

			if (f == null || f.Deleted || !f.Alive || f.Map != m.Map)
				return false;

			int iOldDist = (int)m.GetDistanceToSqrt(f);

			// Already where I want to be
			if (iOldDist >= iWantDistMin && iOldDist <= iWantDistMax)
			{
				CheckTurn(f);
				return true;
			}

			// I'm either too close or too far
			// I'm unable to move!
			if (BlockingMove() || m.DisallowAllMoves)
			{
				CheckTurn(f);
				return false;
			}

			// Walk to my target
			for (int i = 0; i < iSteps; i++)
			{
				// Get the current distance
				int iCurrDist = (int)m.GetDistanceToSqrt(f);

				if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
				{
					bool needCloser = (iCurrDist > iWantDistMax);

					if (needCloser && m_Path != null && m_Path.Goal == f)
					{
						if (m_Path.Follow(bRun, 1))
							m_Path = null;
					}
					else
					{
						Direction dirTo;

						if (needCloser)
							dirTo = m.GetDirectionTo(f);
						else
							dirTo = GetDirectionFrom(f.X, f.Y);

						// Add the run flag
						if (bRun)
							dirTo = dirTo | Direction.Running;

						if (!DoMove(dirTo, true))
						{
							if (needCloser)
							{
								m_Path = new PathFollower(m, f);
								m_Path.Mover = new MoveMethod(DoMoveImpl);

								if (m_Path.Follow(bRun, 1))
									m_Path = null;
							}
							else
							{
								m.Direction = m.GetDirectionTo(f);
								m_Path = null;
							}
						}
						else
						{
							m_Path = null;
						}
					}
				}
			}

			// Am I where I want to be now?
			int iNewDist = (int)m.GetDistanceToSqrt(f);

			if (iNewDist >= iWantDistMin && iNewDist <= iWantDistMax)
				return true;
			return false;
		}

		public Direction GetDirectionFrom(int x, int y)
		{
			int dx = x - m_Mobile.Location.X;
			int dy = y - m_Mobile.Location.Y;

			int rx = (dx - dy) * 44;
			int ry = (dx + dy) * 44;

			int ax = Math.Abs(rx);
			int ay = Math.Abs(ry);

			Direction ret;

			if (((ay >> 1) - ax) >= 0)
			{
				ret = (ry > 0) ? Direction.Up : Direction.Down;
			}
			else if (((ax >> 1) - ay) >= 0)
			{
				ret = (rx > 0) ? Direction.Left : Direction.Right;
			}
			else if (rx >= 0 && ry >= 0)
			{
				ret = Direction.West;
			}
			else if (rx >= 0 && ry < 0)
			{
				ret = Direction.South;
			}
			else if (rx < 0 && ry < 0)
			{
				ret = Direction.East;
			}
			else
			{
				ret = Direction.North;
			}

			return ret;
		}

		public virtual void CheckTurn(IDamageable f)
		{
			BaseCreature m = m_Mobile as BaseCreature;
			if (f == null)
				return;

			if (Core.TickCount <= m.LastMoveTime + 1000)
				return;

			if (Core.TickCount <= NextTurn)
				return;

			Direction d = m.GetDirectionTo(f);
			if (d != m.Direction && (BlockingMove() || m.DisallowAllMoves || Core.TickCount >= m.LastMoveTime + 2000))
			{
				NextTurn = Core.TickCount + 1000;
				m.Direction = d;
			}
			else if (Core.TickCount < m.LastMoveTime + 2000)
			{
				d = (Direction)((int)d + Utility.RandomList(-1, +1));
				Timer.DelayCall(TimeSpan.FromMilliseconds(1), new TimerStateCallback(Turn), f);
				NextTurn = Core.TickCount + 1000;
				m.Direction = d;
			}
		}

		public virtual void Turn(object o)
		{
			BaseCreature m = m_Mobile as BaseCreature;
			if (m == null || m.Deleted || !m.Alive || m.Map == null || m.Map == Map.Internal)
				return;
			Mobile f = o as Mobile;
			if (f == null || f.Deleted || !f.Alive || f.Map == null || f.Map == Map.Internal)
				return;
			if (m.Map != f.Map)
				return;
			m.Direction = m.GetDirectionTo(f);
		}

		public virtual bool BlockingMove()
		{
//			if (m_Mobile.Spell == null || !m_Mobile.Spell.IsCasting || m_Mobile.Target != null)
//				return false;
//			if (m_Mobile.Spell != null && m_Mobile.Spell.IsCasting)
//				return true;
			return false;
		}

		public virtual bool WalkWaypoint(WayPoint w)
		{
			if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
				return false;

			if (w != null)
			{
				// Get the current distance
				int iCurrDist = (int)m_Mobile.GetDistanceToSqrt(w);
				int iWantDist = 0;
	//				if (w == w.NextPoint)
	//					iWantDist = m_Mobile.RangeHome;
				if (iCurrDist > iWantDist)
				{
					if (m_Path != null && m_Path.Goal == w)
					{
						if (m_Path.Follow(false, 1))
							m_Path = null;
					}
					else
					{
						Direction dirTo = m_Mobile.GetDirectionTo(w);

						if (!DoMove(dirTo, true))
						{
							m_Path = new PathFollower(m_Mobile, w);
							m_Path.Mover = new MoveMethod(DoMoveImpl);

							if (m_Path.Follow(false, 1))
								m_Path = null;
						}
						else
						{
							m_Path = null;
						}
					}
				}
				else
				{
					return true;
				}

				// Get the curent distance
				int iNewDist = (int)m_Mobile.GetDistanceToSqrt(w);

				if (iNewDist == 0)
					return true;
				return false;
			}
			return false;
		}

		public virtual bool ManaFlee()
		{
//			if(!m_Mobile.CanFlee)
//				return false;

			if(m_Mobile.RangeFight == 1)
				return false;

			if (HighMana())
				return false;

			return (IsFleeing || LowMana()) ? true : false;
		}

		// At least 40 mana, Full mana, or at least 20 mana and above 50% mana
		public virtual bool HighMana()
		{
			// At least 40 mana or Full mana
			if (m_Mobile.Mana >= 40 || m_Mobile.Mana >= m_Mobile.ManaMax)
				return true;

			// At least 20 mana and above 50% mana (creature has 39-78 max mana)
			return (m_Mobile.Mana >= 20 && m_Mobile.Mana * 2 > m_Mobile.ManaMax) ? true : false;
		}

		// Below 9 mana and not Full mana
		public virtual bool LowMana()
		{
			return (m_Mobile.Mana >= 9 || m_Mobile.Mana >= m_Mobile.ManaMax) ? false : true;
		}

		// How many hits have been lost?
		public virtual int HitsLost(Mobile m)
		{
			return (m.HitsMax - m.Hits);
		}

		// Above 50% hits
		public virtual bool HighHits()
		{
			return (m_Mobile.Hits * 2 > m_Mobile.HitsMax) ? true : false;
		}

		// Below 20% hits
		public virtual bool LowHits()
		{
			return (m_Mobile.Hits * 5 < m_Mobile.HitsMax) ? true : false;
		}

		public virtual void ResetAction()
		{
			WasTooNear = false;
			WasHiding = false;
			IsSetUp = false;
		}

		public virtual long DelayTime(long v)
		{
            return Core.TickCount + v;
		}

		public virtual long NewTime(double v)
		{
			return Core.TickCount + (int)TimeSpan.FromSeconds(v).TotalMilliseconds;
		}

		public virtual long NewTime(long time, double v)
		{
			return (time + (int)TimeSpan.FromSeconds(v).TotalMilliseconds);
		}

		public virtual bool CheckShotClock()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;

			if(m.InLOS(c) && m.InRange(c, 10))
				ShotClock = Core.TickCount;

			if(Core.TickCount > ShotClock + (int)TimeSpan.FromSeconds(15).TotalMilliseconds)
			{
				m.Combatant = null;
				return false;
			}

			return true;
		}

		public virtual bool CheckMeditate()
		{
            return (m_Mobile.Mana < m_Mobile.ManaMax && Core.TickCount > NextSkillTime && CanMeditate) ? true : false;
		}

		public virtual bool CanMeditate
		{
            get
            {
                BaseCreature m = m_Mobile as BaseCreature;
                if (m.Skills.Meditation.Fixed == 0)
                    return false;

                if (HandFullCheck(m.FindItemOnLayer(Layer.OneHanded)))
                    return false;
                if (HandFullCheck(m.FindItemOnLayer(Layer.TwoHanded)))
                    return false;

                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.Neck)))
                    return false;
                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.Gloves)))
                    return false;
                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.Helm)))
                    return false;
                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.Arms)))
                    return false;
                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.Pants)))
                    return false;
                if(HeavyArmorCheck(m.FindItemOnLayer(Layer.InnerTorso)))
                    return false;
                return true;
                }
		}

		public virtual bool FullHands
        {
            get
            {
                if (HandFullCheck(m_Mobile.FindItemOnLayer(Layer.OneHanded)))
                    return true;

                if (HandFullCheck(m_Mobile.FindItemOnLayer(Layer.TwoHanded)))
                    return true;

                return false;
            }
        }

		public virtual bool HandFullCheck(Item item)
		{
			if (item == null || item is Spellbook || item is Runebook)
				return false;
			if (Core.AOS && item is BaseWeapon && ((BaseWeapon)item).Attributes.SpellChanneling != 0)
				return false;
			if (Core.AOS && item is BaseArmor && ((BaseArmor)item).Attributes.SpellChanneling != 0)
				return false;
			return true;
		}

		private bool HeavyArmorCheck(Item item)
		{
			BaseArmor ar = item as BaseArmor;
			// False means it doesn't block meditation
			if (ar == null)
				return false;
			if (ar.ArmorAttributes.MageArmor != 0)
				return false;
			if (ar.MeditationAllowance == ArmorMeditationAllowance.All)
				return false;
			return true;
		}

		public virtual bool ValidWayPoint(WayPoint w)
		{
			if (w == null)
				return false;

			if (w.Deleted)
				return false;

			if (w.Map == null)
				return false;

			return true;
		}

		public virtual int PoisonImmuneLevel
		{
            get
            {
                // This is the TARGET'S poison immune level
                BaseCreature m = m_Mobile;
                Mobile c = (Mobile)m.Combatant;

                if (c == null)
                    return 5;
                if (c.CheckPoisonImmunity( m, Poison.Lethal ))
                    return 5;
                if (c.CheckPoisonImmunity( m, Poison.Deadly ))
                    return 4;
                if (c.CheckPoisonImmunity( m, Poison.Greater ))
                    return 3;
                if (c.CheckPoisonImmunity( m, Poison.Regular ))
                    return 2;
                if (c.CheckPoisonImmunity( m, Poison.Lesser ))
                    return 1;
                return 0;
            }
		}

		public virtual int PoisonSpellLevel
		{
            get
            {
                int total = m_Mobile.Skills.Magery.Fixed + m_Mobile.Skills.Poisoning.Fixed;
                if (!m_Mobile.InRange( m_Mobile.Combatant, 2 ))
                    return 1;
                if ( total >= 2000 )
                    return 4;
                if ( total > 1790 )
                    return 3;
                if ( total > 1300 )
                    return 2;
                return 1;
            }
		}

		public virtual bool AcquireFocusMob(int iRange, FightMode acqType)
		{
			return AcquireFocusMob(iRange, acqType, false, false, true);
		}

		public override bool AcquireFocusMob(int iRange, FightMode acqType, bool playerOnly, bool findFriend, bool findFoe)
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.Deleted)
			{
				return false;
			}

			if (m.BardProvoked)
			{
				if (m.BardTarget == null || m.BardTarget.Deleted)
				{
					m.FocusMob = null;
					return false;
				}
				else
				{
					m.FocusMob = m.BardTarget;
					return (m.FocusMob != null);
				}
			}
			else if (m.Controlled)
			{
				if (m.ControlTarget == null || m.ControlTarget.Deleted || (m.ControlTarget is Mobile && ((Mobile)m.ControlTarget).Hidden) ||
					!m.ControlTarget.Alive || (m.ControlTarget is Mobile && ((Mobile)m.ControlTarget).IsDeadBondedPet) ||
					!m.InRange(m.ControlTarget, m.RangePerception * 2))
				{
					if (m.ControlTarget != null && m.ControlTarget != m.ControlMaster)
					{
						m.ControlTarget = null;
					}

					m.FocusMob = null;
					return false;
				}
				else
				{
					m.FocusMob = m.ControlTarget;
					return (m.FocusMob != null);
				}
			}

			if (m.ConstantFocus != null)
			{
				m.DebugSay("Acquired my constant focus");
				m.FocusMob = m.ConstantFocus;
				return true;
			}

			if (acqType == FightMode.None)
			{
				m.FocusMob = null;
				return false;
			}

			if (acqType == FightMode.Aggressor && m.Aggressors.Count == 0 && m.Aggressed.Count == 0 &&
				m.FactionAllegiance == null && m.EthicAllegiance == null)
			{
				m.FocusMob = null;
				return false;
			}

			if (m.NextReacquireTime > Core.TickCount)
			{
				m.FocusMob = null;
				return false;
			}

			m.NextReacquireTime = Core.TickCount + (int)m.ReacquireDelay.TotalMilliseconds;

			m.DebugSay("Acquiring...");

			Map map = m.Map;

			if (map != null)
			{
				Mobile newFocusMob = null;
				double val = double.MinValue;
				double theirVal;

				var eable = map.GetMobilesInRange(m.Location, iRange);

				foreach (Mobile f in eable)
				{
					// Deleted and Invulnerable targets are invalid.
					if (f.Deleted || f.Blessed)
					{
						continue;
					}

					// Let's not target ourselves...
					if (f == m)
					{
						continue;
					}

					// Dead targets are invalid.
					if (!f.Alive || f.IsDeadBondedPet)
					{
						continue;
					}

					// Staff members cannot be targeted.
					if (f.IsStaff())
					{
						continue;
					}

					// Does it have to be a player?
					if (playerOnly && !f.Player)
					{
						continue;
					}

					// Can't acquire a target we can't see.
					if (!m.CanSee(f) || !m.InLOS(f))
					{
						continue;
					}

					// If we only want faction friends
					if (findFriend && !findFoe)
					{
						// If it's an allied mobile, make sure we can be beneficial to it.
						if (!m.CanBeBeneficial(f, false))
						{
							continue;
						}

						// Ignore anyone who's not a friend
						if (!m.IsFriend(f))
						{
							continue;
						}
					}
					// Don't ignore friends we want to and can help
					else if (!findFriend || !m.IsFriend(f))
					{
						// Ignore anyone we can't hurt
						if (!m.CanBeHarmful(f, false))
						{
							continue;
						}

						// Let's not target familiars...
						if (f is BaseFamiliar)
						{
							continue;
						}

						// Special summoned creature aggro rules
						if (m.Summoned && m.SummonMaster != null)
						{
							// If this is a summon, it can't target its controller.
							if (f == m.SummonMaster)
								continue;

							// It also must abide by harmful spell rules if the master is a player.
							if (m.SummonMaster is PlayerMobile && !Server.Spells.SpellHelper.ValidIndirectTarget(m.SummonMaster, f))
								continue;

							// Players animated creatures cannot attack other players directly.
							if (f is PlayerMobile && m.IsAnimatedDead && m.SummonMaster is PlayerMobile)
								continue;
						}

						// Don't ignore hostile mobiles
						if (!IsHostile(f))
						{
							// Ignore anyone if we don't want enemies
							if (!findFoe)
							{
								continue;
							}
/*
							// Xmlspawner faction check
							// Ignore mob faction ranked players, more highly more often
							if (!Server.Engines.XmlSpawner2.XmlMobFactions.CheckAcquire(this.m, f))
							{
								continue;
							}
*/
							// Do these checks if I'm not an aggressive fightmode
							if (acqType == FightMode.Aggressor || acqType == FightMode.Evil || acqType == FightMode.Good)
							{

								OppositionGroup g = m.OppositionGroup;

								// Bypass these checks if the enemy is the Opposition
								if (g == null || !g.IsEnemy(this, m))
								{
									//Ignore anyone under EtherealVoyage
									if (TransformationSpellHelper.UnderTransformation(f, typeof(EtherealVoyageSpell)))
									{
										continue;
									}

									// Ignore players with activated honor
									if (f is PlayerMobile && ((PlayerMobile)f).HonorActive && !(m.Combatant == f))
									{
										continue;
									}

									// We want a faction/ethic enemy
									bool bValid = (m.GetFactionAllegiance(f) == BaseCreature.Allegiance.Enemy ||
										m.GetEthicAllegiance(f) == BaseCreature.Allegiance.Enemy);

									// We want a special FightMode enemy
									if (!bValid)
									{
										BaseCreature c = f as BaseCreature;

										// We want a karma enemy
										if (acqType == FightMode.Evil)
										{
											if (c != null && c.Controlled && c.ControlMaster != null)
											{
												bValid = (c.ControlMaster.Karma < 0);
											}
											else
											{
												bValid = (f.Karma < 0);
											}
										}
										// We want a karma enemy
										else if (acqType == FightMode.Good)
										{
											if (c != null && c.Controlled && c.ControlMaster != null)
											{
												bValid = (c.ControlMaster.Karma > 0);
											}
											else
											{
												bValid = (f.Karma > 0);
											}
										}

										// Ignore Invalid targets
										if (!bValid)
										{
											continue;
										}
									}
								}
							}
							// Ignore any non-enemy (We are an Aggressive FightMode)
							else if (!m.IsEnemy(f))
							{
								continue;
							}
						}
					}

					theirVal = m.GetFightModeRanking(f, acqType, playerOnly);

					if (theirVal > val)
					{
						newFocusMob = f;
						val = theirVal;
					}
				}

				eable.Free();

				m.FocusMob = newFocusMob;
			}

			return (m.FocusMob != null);
		}

		public virtual bool AcquireAttacker(int iRange)
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.Deleted)
			{
				return false;
			}

			if (NextAttackerTime > Core.TickCount)
			{
				m.FocusMob = null;
				return false;
			}

			NextAttackerTime = NewTime(CheckAttackerDelay);

			m.DebugSay("Acquiring attacker...");

			Map map = m.Map;

			if (map != null)
			{
				Mobile newFocusMob = null;
				double val = double.MinValue;
				double theirVal;

				var eable = map.GetMobilesInRange(m.Location, iRange);

				foreach (Mobile f in eable)
				{
					// Deleted and Invulnerable targets are invalid.
					if (f.Deleted || f.Blessed)
					{
						continue;
					}

					// Let's not target ourselves...
					if (f == m)
					{
						continue;
					}

					// Dead targets are invalid.
					if (!f.Alive || f.IsDeadBondedPet)
					{
						continue;
					}

					// Staff members cannot be targeted.
					if (f.IsStaff())
					{
						continue;
					}

					// Can't acquire a target we can't see.
					if (!m.CanSee(f) || !m.InLOS(f))
					{
						continue;
					}

					// Ignore anyone we can't hurt
					if (!m.CanBeHarmful(f, false))
					{
						continue;
					}

					// Special summoned creature aggro rules
					if (m.Summoned && m.SummonMaster != null)
					{
						// If this is a summon, it can't target its controller.
						if (f == m.SummonMaster)
							continue;

						// It also must abide by harmful spell rules if the master is a player.
						if (m.SummonMaster is PlayerMobile && !Server.Spells.SpellHelper.ValidIndirectTarget(m.SummonMaster, f))
							continue;

						// Players animated creatures cannot attack other players directly.
						if (f is PlayerMobile && m.IsAnimatedDead && m.SummonMaster is PlayerMobile)
							continue;
					}

					// Don't ignore hostile mobiles
					if (!IsHostile(f))
					{
						continue;
					}

					theirVal = m.GetFightModeRanking(f, FightMode.Aggressor, false);

					if (theirVal > val)
					{
						newFocusMob = f;
						val = theirVal;
					}
				}

				eable.Free();

				m.FocusMob = newFocusMob;
			}

			return (m.FocusMob != null);
		}

		public virtual bool DoCast(Spell spell)
		{
			if (spell != null && Core.TickCount > NextCastTime)
			{
                spell.Cast();
				m_Mobile.DebugSay("{0}", spell.Info.Name);
                CurrentSpell = spell;
				SpellDelay = (int)spell.GetCastRecovery().TotalMilliseconds;
                NextCastTime = (int)spell.GetCastDelay().TotalMilliseconds;

				if (NextCastTime > NextSkillTime)
					NextSkillTime = NextCastTime;

				return true;
			}

			return false;
		}

		//  0% to heal at 100% health
		//  5% to heal at 90% health
		// 10% to heal at 80% health
		// 30% to heal at 79.9% health
		// 40% to heal at 60% health
		// 50% to heal at 40% health
		// 60% to heal below 20% health
		public virtual double GetHealChance(Mobile f)
		{
			// 50% of health lost above 20% chance to heal
			double healChance = (1 - Math.Max(0.2, f.Hits / f.HitsMax)) / 2;

			// plus 20% below 80% health
			if(f.HitsMax * 0.8 >= f.Hits)
				healChance += 0.2;

			return healChance;
		}

        // Must have magery skill and either be non-human or have hands free (More advanced would be to swap out hands)
		public virtual bool IsMage
		{
			get { return (m_Mobile.Skills.Magery.Fixed > 0 && (!m_Mobile.Body.IsHuman || !FullHands)); }
		}

		// ~50% chance to cast a circle to pass
		public virtual int MageryCircle { get { return (!IsMage) ? 0 : (int)(m_Mobile.Skills.Magery.Fixed / (1000 / 7) + 1); } }

		public virtual int ManaMageryCircle
		{
            get
            {
                switch(MageryCircle)
                {
                    case 8: if (m_Mobile.Mana >= 50) { return 8; } goto case 7;
                    case 7: if (m_Mobile.Mana >= 40) { return 7; } goto case 6;
                    case 6: if (m_Mobile.Mana >= 20) { return 6; } goto case 5;
                    case 5: if (m_Mobile.Mana >= 15) { return 5; } goto case 4;
                    case 4: if (m_Mobile.Mana >= 11) { return 4; } goto case 3;
                    case 3: if (m_Mobile.Mana >= 9) { return 3; } goto case 2;
                    case 2: if (m_Mobile.Mana >= 6) { return 2; } goto case 1;
                    case 1: if (m_Mobile.Mana >= 4) { return 1; } goto default;
                    default: return 0;
                }
            }
		}
    }
}
