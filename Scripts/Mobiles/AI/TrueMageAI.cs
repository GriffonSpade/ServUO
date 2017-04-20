using System;
using System.Collections.Generic;
using Server.Items;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Spells.Eighth;
using Server.Spells.Necromancy;
using Server.Spells.Mysticism;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.Mobiles
{
    public class TrueMageAI : TrueAI
    {
		private long m_NextBlessTime;
		private long m_NextCurseTime;
		private long m_NextTeleport;

        public TrueMageAI(BaseCreature m)
            : base(m)
        {
        }

		public override bool Think()
		{
			if(m_Mobile.Deleted)
				return false;

			if(ProcessTarget())
				return true;
			else
				return base.Think();
		}

		public override bool CheckWanderAbility()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			// If I'm meditating, check if I should heal or something
			if(m.Meditating)
			{
				if(Core.TickCount > NewTime(NextSkillTime, -2))
					DoCast(ChooseSpell());
				return true;
			}

			// Check if I should heal or something
			if(Utility.RandomBool())
			{
				DoCast(ChooseSpell());
				return true;
			}

			// If I should and can meditate, meditate
			if(CheckMeditate())
			{
				m.DebugSay("I am going to meditate");
				m.UseSkill(SkillName.Meditation);
                if(m.Meditating)
					m.DebugSay("I am meditating");
				NextSkillTime = NewTime(10);
				return true;
			}

            return false;
		}

		public override void DoCombatMove()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			IDamageable c = m.Combatant;
			Point3D locus = m.Location;

			if (LowMana())
			{
				MinRange = 2;
				MaxRange = 1;
				AbsMinRange = 2;
			}
			else
			{
				MinRange = Math.Max(2, Math.Min(m.RangeFight, 10));
				MaxRange = Math.Max(1, Math.Min(m.RangeFight, 10));
				AbsMinRange = Math.Min(4, MinRange);
			}

			if (MaxRange <= 1)
			{
				WasTooNear = false;
				WasHiding = false;
				IsSetUp = false;
			}
			// Am I in LOS?
			else if (!m_Mobile.InLOS(c))
			{
				WasTooNear = false;
				WasHiding = true;
				IsSetUp = false;
			}
			// Are they out of range?
			else if ((int)m_Mobile.GetDistanceToSqrt(c) >= MaxRange)
			{
				//They're too far! Forget everything else and chase them!
				WasTooNear = false;
				WasHiding = false;
				IsSetUp = false;
			}
			// Are they close up?
			else if ((int) m_Mobile.GetDistanceToSqrt(c) <= MinRange / 2)
			{
				if (WasHiding == true)
				{
					if ((int) m_Mobile.GetDistanceToSqrt(c) >= AbsMinRange / 2)
					{
						//Far enough, I'm set up!
						WasHiding = false;
						IsSetUp = true;
					}
				}
				else if (IsSetUp == true)
				{
					if ((int) m_Mobile.GetDistanceToSqrt(c) < AbsMinRange / 2)
					{
						//I'm set up, but they're too close! Run away!
						WasTooNear = true;
						WasHiding = false;
						IsSetUp = false;
					}
				}
				else
				{
					// They're too close, Run away!
					WasTooNear = true;
				}
			}
			// Am I set up (and in range)?
			else if (IsSetUp == true)
			{
				WasTooNear = false;
				WasHiding = false;
				IsSetUp = false;
			}
			// Am I in range (and not set up)?
			else
			{
				WasHiding = false;
			}

			if (m.InRange(c, 1) && !m.InLOS(c))
				WalkMobileRange(c, 2);
			else if (MaxRange <= 1 || !m.InLOS(c))
				WalkMobileRange(c, 1);
			else if (IsSetUp == true)
				WalkMobileRange(c, AbsMinRange / 2, MaxRange);
			else if (WasHiding == true)
				WalkMobileRange(c, AbsMinRange / 2, MinRange / 2);
			else if (WasTooNear == true)
				WalkMobileRange(c, MaxRange);
			else
				WalkMobileRange(c, MinRange / 2, MaxRange);

			// Did I fail to move?
			int dist = (int)m.GetDistanceToSqrt(c);
			if (dist > MaxRange && locus == m.Location)
			{
				if (OnFailedMove())
				{
					m.DebugSay("My move is blocked, so I am going to attack {0}", m.Combatant.Name);
					ShotClock = Core.TickCount;
					return;
				}
			}

			if (Utility.Random(100) == 0)
			{
				if (dist > MaxRange)
					m.DebugSay("I should be closer to {0}", c.Name);
				else
					m.DebugSay("I am fighting {0}", c.Name);
			}
		}

		public override bool OnFailedMove()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.DisallowAllMoves)
				return false;

			if (m.Target != null)
				return false;

//			if(Core.TickCount > NextCastTime && Core.TickCount >= NextTeleport && ManaCircle() >= 3 && m.Spell == null)
//			{
//				m.DebugSay("I am stuck, I'm going to try teleporting");
//				DoCast(new TeleportSpell(m, null));
//				return false;
//			}

			if (AcquireFocusMob(m.RangeFight, m.FightMode))
			{
				m.Combatant = m.FocusMob;
				m.FocusMob = null;
				return true;
			}

			return false;
		}

		public override bool CheckStopFlee()
		{
			// Ready to fight
			if (HighHits() && !ManaFlee())
				return true;

			// Keep fleeing!
			return false;
		}

		public override void DoCombatAbility()
		{
			DoCast(ChooseSpell());
		}

		public virtual Spell ChooseSpell()
		{
			BaseCreature m = m_Mobile as BaseCreature;

			if (m.Spell != null)
				return null;

			if (Core.TickCount <= NextCastTime)
				return null;

			// We are ready to cast a spell
			IDamageable c = m.Combatant;
			Spell spell = null;
			bool combat = HasCombatant();

            // Heal/Cure Self
            spell = CheckCastHealSpell();

			if (spell != null)
				return spell;

			// Heal/Cure Ally
			spell = CheckCastAidSpell();

			if (spell != null)
				return spell;

			if (!combat)
				return null;

			// I'm in range and able to cast a spell
			if (!m.InRange(c, 10))
				return null;

			// I'm able to cast a spell
			if (!IsMage)
				return null;

			// Conserve mana when I start getting kinda low!
			if (MaxRange <= 1 && !HighMana() && Utility.Random(50) > m.Mana)
			{
				NextCastTime = NewTime(1);
				return null;
			}

            switch(Utility.RandomBool())
            {
                case true: return GetRandomDamageSpellMage();
                default: return GetRandomSupportSpellMage();
            }
        }

		public virtual Spell GetRandomDamageSpellMage()
		{
            int circle = ManaMageryCircle;
			switch(Utility.RandomMinMax(Math.Max(0,circle - 4), circle))
			{
				case 0: return new MagicArrowSpell(m_Mobile, null);
				case 1: return new HarmSpell(m_Mobile, null);
				case 2: return new FireballSpell(m_Mobile, null);
				case 3: return new LightningSpell(m_Mobile, null);
				case 4: return new MindBlastSpell(m_Mobile, null);
				case 5: return SixthCircleDamageSpellMage();
				case 6: return SeventhCircleDamageSpellMage();
				default: return EighthCircleDamageSpellMage();
			}
		}

		public virtual Spell SixthCircleDamageSpellMage()
		{
			switch(Utility.RandomBool())
            {
                case true: m_Mobile.DebugSay("Explosion"); return new ExplosionSpell(m_Mobile, null);
                default: m_Mobile.DebugSay("Energy Bolt"); return new EnergyBoltSpell(m_Mobile, null);
            }
		}

		public virtual Spell SeventhCircleDamageSpellMage()
		{
/*			BaseCreature m = m_Mobile as BaseCreature;

			if (m.CanAOE && Utility.RandomBool() && m.HostileCount() > 2)
			{
				switch(Utility.Random(3))
				{
					case 1: return new ChainLightningSpell(m_Mobile, null);
					case 2: return new MeteorSwarmSpell(m_Mobile, null);
					default: return new FlameStrikeSpell(m_Mobile, null);
				}
			}*/
			return new FlameStrikeSpell(m_Mobile, null);
		}

		public virtual Spell EighthCircleDamageSpellMage()
		{
/*			BaseCreature m = m_Mobile as BaseCreature;
			Mobile c = m.Combatant;
			int dist = (int)m.GetDistanceToSqrt(c);

			if (m_Mobile.CanAOE && Utility.RandomBool() && m.HostileCount() > 2 && dist <= 2)
				return new EarthquakeSpell(m_Mobile, null);*/
			return SeventhCircleDamageSpellMage();
		}

		public virtual Spell GetRandomSupportSpellMage()
		{
			switch(ManaMageryCircle)
            {
                case 0: return FirstCircleSupportSpellMage();   //Curse
                case 1: return SecondCircleSupportSpellMage();  //Bless
                case 2: return ThirdCircleSupportSpellMage();   //Poison
                case 3: return FourthCircleSupportSpellMage();  //Mana Drain
                case 4: return FifthCircleSupportSpellMage();   //Paralyze
                case 5: return SixthCircleSupportSpellMage();   //Invisibility
                case 6: return ThirdCircleSupportSpellMage();   //More Poison!
                default: return GetRandomDamageSpellMage();
            }
		}

		public virtual Spell FirstCircleSupportSpellMage()
		{
            if (m_Mobile.Combatant is IDamageable)
                return GetRandomDamageSpellMage();

            if ( CurseSpell.UnderEffect((Mobile)m_Mobile.Combatant) )
                return GetRandomDamageSpellMage();

            if ( ManaMageryCircle >= 4 )
                return new CurseSpell( m_Mobile, null );

            if ( Core.TickCount < m_NextCurseTime )
                return GetRandomDamageSpellMage();

            switch (Utility.Random(3))
            {
                case 0: return new WeakenSpell( m_Mobile, null );
                case 1: return new ClumsySpell( m_Mobile, null );
                default: return new FeeblemindSpell( m_Mobile, null );
            }
		}

		public virtual Spell SecondCircleSupportSpellMage()
		{
            if ( Core.TickCount < m_NextBlessTime )
                return GetRandomDamageSpellMage();

            if ( ManaMageryCircle >= 3 )
                return new BlessSpell( m_Mobile, null );

            switch (Utility.Random(3))
            {
                case 0: return new AgilitySpell( m_Mobile, null );
                case 1: return new CunningSpell( m_Mobile, null );
                default: return new StrengthSpell( m_Mobile, null );
            }
		}

		public virtual Spell ThirdCircleSupportSpellMage()
		{
            Mobile c = (Mobile)m_Mobile.Combatant;

            if (c == null)
                return GetRandomDamageSpellMage();

            int poisLev = c.Poisoned ? c.Poison.Level + 1 : 0;
            int poisSpl = PoisonSpellLevel;

            // Target's poison level/immunity is equal to or greater than my poison spell level
            if ( poisLev >= poisSpl || PoisonImmuneLevel >= poisSpl )
                return GetRandomDamageSpellMage();

            return new PoisonSpell(m_Mobile, null);
		}

		public virtual Spell FourthCircleSupportSpellMage()
		{
            Mobile c = (Mobile)m_Mobile.Combatant;

            if (c == null)
                return GetRandomDamageSpellMage();

            if( c.Mana < 15 )
                return GetRandomDamageSpellMage();

            if( ManaMageryCircle >= 7 && Utility.RandomBool())
                return new ManaVampireSpell( m_Mobile, null );

            return new ManaDrainSpell( m_Mobile, null );
		}

		public virtual Spell FifthCircleSupportSpellMage()
		{
            Mobile c = (Mobile)m_Mobile.Combatant;

            if (c == null)
                return GetRandomDamageSpellMage();

            if ( c.Paralyzed || c.Poisoned || Utility.RandomBool() )
                return GetRandomDamageSpellMage();

            return new ParalyzeSpell(m_Mobile, null);
		}

		public virtual Spell SixthCircleSupportSpellMage()
		{
            if (Utility.Random(3) > 0 || m_Mobile.Hidden || m_Mobile.Poisoned )
                return GetRandomDamageSpellMage();

            return new InvisibilitySpell(m_Mobile, null);
		}

		private Spell CheckCastHealSpell()
		{
			BaseCreature m = m_Mobile as BaseCreature;

            // Am I ready to cure yet?
            if(Core.TickCount >= NextCureTime)
            {
                // Cure thyself
                if (m.Poisoned)
                {
                    return GetCureSpell();
                }
            }

			// Am I ready to heal yet?
			if(Core.TickCount >= NextHealTime)
			{
				// Summoned creatures never heal themselves
				if(m.Summoned)
					return null;

				// Poisoned creatures never heal themselves
				if(m.Poisoned)
					return null;

				// Hiding creatures never heal themselves
				if(m.Hidden)
					return null;

				// Summoned creatures never heal themselves
				if(m.Hits >= m.HitsMax)
					return null;

				// Mortally Wounded creatures never heal themselves
				if(MortalStrike.IsWounded(m))
					return null;

				// Don't check to heal again for (5) seconds if it fails after this point
				NextHealTime = DelayTime(CheckHealDelay);

                // Heal thyself, if want
				if(GetHealChance(m) >= Utility.RandomDouble())
                {
                    return GetHealSpell(m);
                }
			}

            return null;
		}

		private Spell CheckCastAidSpell()
		{
			BaseCreature m = m_Mobile as BaseCreature;
/*
            if (!IsHealer)
            {
                return null;
            }
*/
            // Hiding creatures never heal
            if(!m.Poisoned && m.Hidden)
                return null;

            // Am I ready to aid yet?
            if(Core.TickCount >= NextAidTime)
            {
                NextAidTime = DelayTime(CheckAidDelay);

                bool hasCureTarget = HasCureTarget();
                bool hasHealTarget = HasHealTarget();

                if (!hasCureTarget && !hasHealTarget)
                {
                    return null;
                }

                // Cure
                if (hasCureTarget && (!hasHealTarget || Utility.RandomBool()))
                {
                    return GetCureSpell();
                }

                // Heal
                return GetHealSpell();
			}

            return null;
		}

		private Spell GetCureSpell()
		{
            if (ManaMageryCircle >= 2)
            {
                NextCureTime = DelayTime(CheckCureDelay);

                return new CureSpell(m_Mobile, null);
			}

            return null;
		}

		private Spell GetHealSpell()
		{
            if (ManaMageryCircle >= 1)
            {
                NextCureTime = DelayTime(CheckCureDelay);

                if (ManaMageryCircle >= 4)
                {
                    return new GreaterHealSpell(m_Mobile, null);
                }

                return new HealSpell(m_Mobile, null);
			}

            return null;
		}

		private Spell GetHealSpell(Mobile m)
		{
            if (ManaMageryCircle >= 1)
            {
                NextCureTime = DelayTime(CheckCureDelay);

                if (ManaMageryCircle >= 4 && HitsLost(m) > 10)
                {
                    return new GreaterHealSpell(m_Mobile, null);
                }

                return new HealSpell(m_Mobile, null);
			}

            return null;
		}

        private bool HasCureTarget()
        {
			BaseCreature m = m_Mobile as BaseCreature;

			foreach (Mobile f in m.GetMobilesInRange(10))
			{
				if (!f.Poisoned)
					continue;
				if (f.Hidden)
					continue;
				if (f.Hits == 0)
					continue;
				if (f == m)
					continue;
				if (!m.InLOS(f))
					continue;
				if (!m.CanSee(f))
					continue;
				if (!m.IsFriend(f))
					continue;
				if (!m.CanBeBeneficial(f))
					continue;
                return true;
			}

            return false;
        }

        private bool HasHealTarget()
        {
			BaseCreature m = m_Mobile as BaseCreature;

            // Summoned creatures never heal
            if(m.Summoned)
                return false;

			foreach (Mobile f in m.GetMobilesInRange(10))
			{
                if (f.Hits == f.HitsMax)
                    continue;
				if (f.Poisoned)
					continue;
				if (f.Hits == 0)
					continue;
				if (f.Hidden)
					continue;
				if (f == m)
					continue;
				if (MortalStrike.IsWounded(f))
					continue;
				if (!m.InLOS(f))
					continue;
				if (!m.CanSee(f))
					continue;
				if (!m.IsFriend(f))
					continue;
				if (!m.CanBeBeneficial(f))
					continue;
                return true;
			}

            return false;
        }

		private Mobile CureTarget()
		{
            BaseCreature m = m_Mobile as BaseCreature;
/*
			if(!m.IsHealer)
			{
				if(m.Poisoned)
					return m;
				return null;
			}
*/
			double lifeScore = (!m.Poisoned) ? 0 :
                GetHealChance(m) * HitsLost(m) * 1.2 * (m.Poison.Level + 1) + m.Poison.Level + 1;
			Mobile toCure = (lifeScore == 0) ? null : m as Mobile;

			foreach (Mobile f in m.GetMobilesInRange(10))
			{
				if (!f.Poisoned)
					continue;
				if (f.Hits == 0)
					continue;
				if (f == m)
					continue;
				if (!m.InLOS(f))
					continue;
				if (!m.CanSee(f))
					continue;
				if (!m.IsFriend(f))
					continue;
				if (!m.CanBeBeneficial(f))
					continue;

				double newLifeScore =
                    GetHealChance(f) * HitsLost(f) * (f.Poison.Level + 1) + f.Poison.Level + 1;

				if (newLifeScore > lifeScore)
				{
                    toCure = f;
					lifeScore = newLifeScore;
				}
			}

			return toCure;
		}

		private Mobile HealTarget()
		{
			BaseCreature m = m_Mobile as BaseCreature;
/*
			if(!m.IsHealer)
			{
				if(!m.Poisoned && !MortalStrike.IsWounded(m))
					return m;
				return null;
			}
*/
//			int range = CurrentSpell is CloseWoundsSpell ? 2 : 10;

			double lifeScore = (m.Poisoned || MortalStrike.IsWounded(m)) ? 0 :
                GetHealChance(m) * HitsLost(m) * 1.2;
			Mobile toHeal = (lifeScore == 0) ? null : m as Mobile;

			foreach (Mobile f in m.GetMobilesInRange(10))
			{
				if (f.Hits == 0)
					continue;
				if (f.Poisoned)
					continue;
				if (f == m)
					continue;
				if (MortalStrike.IsWounded(f))
					continue;
				if (!m.InLOS(f))
					continue;
				if (!m.CanSee(f))
					continue;
				if (!m.IsFriend(f))
					continue;
				if (!m.CanBeBeneficial(f))
					continue;

				double newLifeScore = GetHealChance(f) * HitsLost(f);

				if (newLifeScore > lifeScore)
				{
					toHeal = f;
					lifeScore = newLifeScore;
				}
			}

			return toHeal;
		}

		private bool ProcessTarget()
		{
			BaseCreature m = m_Mobile as BaseCreature;
			Target targ = m.Target;

			if(targ == null)
				return false;

            if ((targ.Flags & TargetFlags.Harmful) != 0)
            {
				if (m.Combatant == null || BadTarget() ||
                    !(targ.Range == -1 || m.InRange(m.Combatant, targ.Range)))
				{
					targ.Cancel(m_Mobile, TargetCancelType.Canceled);
					targ = null;
					NextCastTime = NewTime(0);
				}
                else
				{
					LastSpellTarget = m.Combatant;
					targ.Invoke(m, m.Combatant);
					targ = null;
                    NextCastTime = DelayTime(SpellDelay);
				}
            }
			else if((targ.Flags & TargetFlags.Beneficial) != 0)
			{
				if(targ is HealSpell.InternalTarget || targ is GreaterHealSpell.InternalTarget)
				{
                    Mobile f = HealTarget();

					if(f == null)
					{
						targ.Cancel(m, TargetCancelType.Canceled);
						targ = null;
						NextCastTime = NewTime(0);
					}
					else
					{
						targ.Invoke(m, f);
						targ = null;
                        NextCastTime = DelayTime(SpellDelay);
					}
				}
				else if(targ is CureSpell.InternalTarget)
				{
					Mobile f = CureTarget();

					if(f == null)
					{
						targ.Cancel(m, TargetCancelType.Canceled);
						targ = null;
						NextCastTime = NewTime(0);
					}
					else
					{
						targ.Invoke(m, f);
						targ = null;
                        NextCastTime = DelayTime(SpellDelay);
					}
				}
				else
				{
					targ.Invoke(m, m);
					targ = null;
                    NextCastTime = DelayTime(SpellDelay);
				}
			}
            else
            {
				if (targ != null)
					targ.Cancel(m_Mobile, TargetCancelType.Canceled);
					NextCastTime = NewTime(0);
            }

			return true;
        }
    }
}
