namespace NdcBingo.Models.Players
{
    public class NewViewModel
    {
        public string Message { get; set; }
        public string ReturnUrl { get; set; }
        public NewPlayerViewModel Player { get; set; } = new NewPlayerViewModel();
    }
}