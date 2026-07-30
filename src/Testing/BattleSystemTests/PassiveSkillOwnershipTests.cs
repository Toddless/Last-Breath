namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Items.Grants;
    using Godot;
    using Moq;

    /// <summary>
    /// One skill Id can arrive from several independent owners at once — an item grant and a passive-tree
    /// node hand out the same <c>Passive_Skill_*</c>. Ownership is therefore per INSTANCE: a source may only
    /// ever take back the instance it registered, the strongest registered instance is the one attached to
    /// the entity, and every attach is matched by exactly one detach.
    /// </summary>
    [TestClass]
    public class PassiveSkillOwnershipTests
    {
        private const string SharedId = "Passive_Skill_Shared";

        [TestMethod]
        public void RemovingOneSource_KeepsTheOtherSourcesSkillActive()
        {
            var component = Component();
            var fromItem = new CountingSkill(SharedId, power: 1f);
            var fromNode = new CountingSkill(SharedId, power: 1f);
            component.AddSkill(fromItem);
            component.AddSkill(fromNode);

            component.RemoveSkill(fromItem);

            Assert.AreSame(fromNode, component.GetSkill(SharedId), "the node still grants this skill");
            Assert.AreEqual(1, component.Skills.Count, "an Id is in effect exactly once");
            Assert.IsTrue(fromNode.IsAttached, "the surviving contribution must be attached to the owner");
        }

        [TestMethod]
        public void BothSourcesRemoved_DetachTheLiveSkillExactlyOnce()
        {
            var component = Component();
            var strongFromNode = new CountingSkill(SharedId, power: 2f);
            var weakFromItem = new CountingSkill(SharedId, power: 1f);
            component.AddSkill(strongFromNode);
            component.AddSkill(weakFromItem);

            component.RemoveSkill(weakFromItem);
            component.RemoveSkill(strongFromNode);

            Assert.IsNull(component.GetSkill(SharedId), "no source left — no skill");
            Assert.AreEqual(0, component.Skills.Count);
            Assert.AreEqual(1, strongFromNode.Detaches, "the live skill leaves the owner exactly once");
            Assert.AreEqual(0, weakFromItem.Attaches, "a contribution that lost the collision never touches the owner");
            Assert.AreEqual(0, weakFromItem.Detaches, "...and must not be detached either");
        }

        [TestMethod]
        public void EveryContributionIsDetachedAsManyTimesAsItWasAttached()
        {
            var component = Component();
            var first = new CountingSkill(SharedId, power: 1f);
            var second = new CountingSkill(SharedId, power: 1f);
            component.AddSkill(first);
            component.AddSkill(second);

            component.RemoveSkill(second);
            component.RemoveSkill(first);

            Assert.AreEqual(first.Attaches, first.Detaches, "attach/detach of the first source must balance out");
            Assert.AreEqual(second.Attaches, second.Detaches, "attach/detach of the second source must balance out");
            Assert.IsFalse(first.IsAttached);
            Assert.IsFalse(second.IsAttached);
            Assert.IsNull(component.GetSkill(SharedId));
        }

        [TestMethod]
        public void RemovingTheStrongerSource_PromotesTheWeakerContributionBack()
        {
            var component = Component();
            var weakFromItem = new CountingSkill(SharedId, power: 1f);
            var strongFromNode = new CountingSkill(SharedId, power: 3f);
            component.AddSkill(weakFromItem);
            component.AddSkill(strongFromNode);

            Assert.AreSame(strongFromNode, component.GetSkill(SharedId), "the stronger version is the active one");
            Assert.IsFalse(weakFromItem.IsAttached, "the weaker version waits, detached");

            component.RemoveSkill(strongFromNode);

            Assert.AreSame(weakFromItem, component.GetSkill(SharedId), "the item's contribution survived the respec");
            Assert.IsTrue(weakFromItem.IsAttached);
            Assert.IsFalse(strongFromNode.IsAttached);
        }

        [TestMethod]
        public void StrongerContributionWinsRegardlessOfTheOrderTheSourcesArrivedIn()
        {
            var strongFirst = Component();
            var strongA = new CountingSkill(SharedId, power: 3f);
            strongFirst.AddSkill(strongA);
            strongFirst.AddSkill(new CountingSkill(SharedId, power: 1f));

            var weakFirst = Component();
            weakFirst.AddSkill(new CountingSkill(SharedId, power: 1f));
            var strongB = new CountingSkill(SharedId, power: 3f);
            weakFirst.AddSkill(strongB);

            Assert.AreSame(strongA, strongFirst.GetSkill(SharedId), "a weaker newcomer must not evict the strong one");
            Assert.AreSame(strongB, weakFirst.GetSkill(SharedId), "a stronger newcomer must take the slot");
        }

        [TestMethod]
        public void SuppressAndResume_ReattachEveryActiveSkillExactlyOnce()
        {
            var component = Component();
            var active = new CountingSkill(SharedId, power: 3f);
            var dormant = new CountingSkill(SharedId, power: 1f);
            var other = new CountingSkill("Passive_Skill_Other", power: 1f);
            component.AddSkill(active);
            component.AddSkill(dormant);
            component.AddSkill(other);

            component.Suppress();

            Assert.IsFalse(active.IsAttached);
            Assert.IsFalse(other.IsAttached);
            Assert.AreEqual(1, active.Detaches);
            Assert.AreEqual(1, other.Detaches);

            component.Resume();

            Assert.AreEqual(2, active.Attaches, "one attach per resume — never one per registered contribution");
            Assert.AreEqual(2, other.Attaches);
            Assert.AreEqual(0, dormant.Attaches, "a losing contribution stays dormant across suppression");
            Assert.AreEqual(2, component.Skills.Count);
        }

        [TestMethod]
        public void AddedWhileSuppressed_AttachesOnlyOnResume()
        {
            var component = Component();
            component.Suppress();
            var skill = new CountingSkill(SharedId, power: 1f);

            component.AddSkill(skill);
            Assert.AreEqual(0, skill.Attaches, "suppression means nothing is attached");

            component.Resume();
            Assert.AreEqual(1, skill.Attaches);
        }

        [TestMethod]
        public void SuppressSurvivesASkillThatRegistersAnotherSkillWhileDetaching()
        {
            var component = Component();
            CountingSkill? grantedOnDetach = null;
            var trigger = new ReactingSkill("Passive_Skill_Trigger", onDetach: () =>
            {
                grantedOnDetach = new CountingSkill("Passive_Skill_Granted", power: 1f);
                component.AddSkill(grantedOnDetach);
            });
            component.AddSkill(trigger);
            component.AddSkill(new CountingSkill("Passive_Skill_Companion", power: 1f));

            component.Suppress();

            Assert.IsNotNull(grantedOnDetach, "the reacting skill ran");
            Assert.AreEqual(0, grantedOnDetach!.Attaches, "a skill registered under suppression stays detached");
        }

        [TestMethod]
        public void UnequippingAnItem_LeavesTheNodeGrantedSkillOfTheSameIdAlone()
        {
            var component = Component();
            var owner = OwnerOf(component);
            var fromNode = new CountingSkill(SharedId, power: 1f);
            component.AddSkill(fromNode);

            var provider = new StubSkillProvider(SharedId, power: 1f);
            var grant = new PassiveSkillGrant("Grant", SharedId, new Dictionary<string, float>(), () => provider);
            grant.Attach(owner);
            grant.Detach(owner);

            Assert.AreSame(fromNode, component.GetSkill(SharedId), "unequipping must not evict the node's passive");
            Assert.IsTrue(fromNode.IsAttached, "the node's passive is back in effect");
            Assert.AreEqual(1, component.Skills.Count);
        }

        private static PassiveSkillsComponent Component() => new(Mock.Of<IFightable>());

        private static IFightable OwnerOf(IPassiveSkillsComponent component)
        {
            var owner = new Mock<IFightable>();
            owner.SetupGet(fightable => fightable.PassiveSkills).Returns(component);
            return owner.Object;
        }

        /// <summary>Counts its own attachments: the component owes every instance exactly as many
        /// detaches as attaches, and a contribution that lost the collision owes it zero of both.</summary>
        private class CountingSkill(string id, float power) : ISkill
        {
            public string Id => id;
            public string InstanceId { get; } = Guid.NewGuid().ToString();
            public Texture2D? Icon => null;
            public string DisplayName => id;
            public string Description => id;

            public float Power { get; } = power;
            public int Attaches { get; private set; }
            public int Detaches { get; private set; }
            public bool IsAttached => Attaches > Detaches;

            public bool IsSame(string otherId) => InstanceId.Equals(otherId);

            public virtual void Attach(IFightable owner) => Attaches++;

            public virtual void Detach(IFightable owner) => Detaches++;

            public ISkill Copy() => new CountingSkill(id, Power);

            public bool IsStronger(ISkill skill) => skill is CountingSkill other && Power > other.Power;
        }

        /// <summary>Edits the component from inside its own detach — the shape that turns an unsnapshotted
        /// iteration over the skill list into an InvalidOperationException.</summary>
        private sealed class ReactingSkill(string id, Action onDetach) : CountingSkill(id, 1f)
        {
            public override void Detach(IFightable owner)
            {
                base.Detach(owner);
                onDetach();
            }
        }

        private sealed class StubSkillProvider(string id, float power) : ISkillProvider
        {
            public ISkill? CreateSkill(string skillId) => skillId == id ? new CountingSkill(id, power) : null;
        }
    }
}
