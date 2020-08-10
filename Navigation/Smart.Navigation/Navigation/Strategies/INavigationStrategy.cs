namespace Smart.Navigation.Strategies
{
    using System.Threading.Tasks;

    public interface INavigationStrategy
    {
        StrategyResult Initialize(INavigationController controller);

        object ResolveToView(INavigationController controller);

        void UpdateStack(INavigationController controller, object toView);

        Task UpdateStackAsync(INavigationController controller, object toView);
    }
}
