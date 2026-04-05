using Unity.AppUI.MVVM;

namespace Systems.UI
{
    public class MyAppBuilder : UIToolkitAppBuilder<MyApp>
    {
        protected override void OnAppInitialized(MyApp app)
        {
            base.OnAppInitialized(app);
            // Called after the app is initialized
        }

        protected override void OnConfiguringApp(AppBuilder builder)
        {
            base.OnConfiguringApp(builder);
            // Called during app configuration

            builder.services.AddSingleton<IListProjectStoreService, ListProjectStoreService>();

            builder.services.AddTransient<ListProjectViewModel>();

            builder.services.AddTransient<ListProjectPage>();
        }

        protected override void OnAppShuttingDown(MyApp app)
        {
            base.OnAppShuttingDown(app);
            // Called before the app is shut down
        }
    }
}
