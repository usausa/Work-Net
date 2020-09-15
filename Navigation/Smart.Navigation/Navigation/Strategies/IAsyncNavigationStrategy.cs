namespace Smart.Navigation.Strategies
{
    using System.Threading.Tasks;

    public interface IAsyncNavigationStrategy
    {
        StrategyResult Initialize(INavigationController controller);

        object ResolveToView(INavigationController controller);

        Task UpdateStackAsync(INavigationController controller, object toView);
    }
}
