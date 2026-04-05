using Unity.AppUI.MVVM;

namespace Systems.UI
{
    public class MyApp : App
    {
        public new static MyApp current => (MyApp)App.current;

        public override void InitializeComponent()
        {
            base.InitializeComponent();
            // rootVisualElement.Add(services.GetRequiredService<MainPage>());
            
        }

        public override void Shutdown()
        {
            // Called when the app is shutting down
        }
    }
}