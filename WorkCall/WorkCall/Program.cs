using System;

namespace WorkCall
{
    class Program
    {
        static void Main()
        {
        }

        private static void TestInterface()
        {
            IAction action = new SealedAction();
            action.Invoke();
        }

        private static void TestNonSealed()
        {
            var action = new NonSealedAction();
            action.Invoke();
        }

        private static void TestSealed()
        {
            var action = new SealedAction();
            action.Invoke();
        }

        private static void TestAction()
        {
            Action action = () => { };
            action.Invoke();
        }

        private static void TestArgumentSealed(SealedAction action)
        {
            action.Invoke();
        }

        private static void TestArgumentAction(Action action)
        {
            action.Invoke();
        }
    }

    public interface IAction
    {
        void Invoke();
    }

    public class NonSealedAction : IAction
    {
        public void Invoke()
        {
        }
    }

    public sealed class SealedAction : IAction
    {
        public void Invoke()
        {
        }
    }
}
