namespace PlaygroundTest.ComponentTesting
{
    using Playground.Components;

    [TestClass]
    public class AttributeComponentTest
    {
        private AttributeComponent _attributeComponent;
        private ModifierManager _modifierManager;

        [TestInitialize]
        public void StartUp()
        {
            //_modifierManager = new ModifierManager();
            //_attributeComponent = new AttributeComponent();
            //_attributeComponent.AddAttribute(new Dexterity(_modifierManager));
        }

        [TestMethod]
        public void FirstTest()
        {
            //var attr = _attributeComponent.GetAttribute<Dexterity>();
            //if (attr != null)
            //    attr.InvestedPoints += 5;

            //var damageComponent = new DamageComponent(new UnarmedDamageStrategy(), _modifierManager);
            //var criticalChanceBefor = damageComponent.GetCriticalChance();
            //var attrr = _attributeComponent.GetAttribute<Dexterity>();
            //if (attrr != null)
            //    attrr.InvestedPoints -= 3;
            //var criticalChanceAfter = damageComponent.GetCriticalChance();

            //Assert.IsTrue(criticalChanceBefor > criticalChanceAfter);
        }
    }
}
