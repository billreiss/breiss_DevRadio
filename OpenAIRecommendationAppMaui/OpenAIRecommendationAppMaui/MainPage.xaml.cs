using OpenAIRecommendationAppMaui.Services;

namespace OpenAIRecommendationAppMaui
{
    public partial class MainPage : ContentPage
    {
        OpenAIService openAIService;
        public MainPage(OpenAIService svc)
        {
            openAIService = svc;
            InitializeComponent();
        }

        private async void OnRestaurantClicked(object sender, EventArgs e)
        {
            await GetRecommendation("restaurant");
        }

        private async void OnHotelClicked(object sender, EventArgs e)
        {
            await GetRecommendation("hotel");
        }

        private async void OnAttractionClicked(object sender, EventArgs e)
        {
            await GetRecommendation("attraction");
        }

        private async Task GetRecommendation(string recommendationType)
        {
            if (string.IsNullOrWhiteSpace(LocationEntry.Text))
            {
                await DisplayAlert("Empty location", "Please enter a location (city or postal code)", "OK");
                return;
            }
            SmallLabel.Text = "Working on it...This can take a little while based on the model selected.";
            var message = await openAIService.CallOpenAI(recommendationType, LocationEntry.Text);
            SmallLabel.Text = message;
        }
    }
}
