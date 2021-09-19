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

        private static void TestNonSealed2()
        {
            new NonSealedAction().Invoke();
        }

        private static void TestSealed()
        {
            var action = new SealedAction();
            action.Invoke();
        }

        private static void TestSealed2()
        {
            new SealedAction().Invoke();
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

        private static void TestArgumentActionNullable1(Action action)
        {
            if (action is not null)
            {
                action.Invoke();
            }
        }

        private static void TestArgumentActionNullable2(Action action)
        {
            action?.Invoke();
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
