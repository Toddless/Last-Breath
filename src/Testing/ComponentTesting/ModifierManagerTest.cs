namespace PlaygroundTest.ComponentTesting
{
    using Playground.Components;

    [TestClass]
    public class ModifierManagerTest
    {
        private ModifierManager _modifierManager;
        [TestInitialize]
        public void StartUp()
        {
            _modifierManager = new ModifierManager();
            // need to test how i delete some modifiers
        }

        [TestMethod]
        public void Deleting_Modifier_Test()
        {
        }
    }
}
