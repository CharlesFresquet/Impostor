using Impostor.Api.Events;
using Impostor.Api.Events.Player;
using Microsoft.Extensions.Logging;

namespace Impostor.Plugins.Example.Handlers
{
    /// <summary>
    ///     A class that listens for two events.
    ///     It may be more but this is just an example.
    ///
    ///     Make sure your class implements <see cref="IEventListener"/>.
    /// </summary>
    public class GameEventListener : IEventListener
    {
        private readonly ILogger<ExamplePlugin> _logger;
        // private readonly bool debug = false;

        public GameEventListener(ILogger<ExamplePlugin> logger)
        {
            _logger = logger;
        }

        /// <summary>
        ///     An example event listener.
        /// </summary>
        /// <param name="e">
        ///     The event you want to listen for.
        /// </param>

        [EventListener]
        public void OnPlayerChat(IPlayerChatEvent e)
        {
            switch (e.Message)
            {
                case ("/ColorImp"):
                    _logger.LogInformation($"Color help : /color [Black, Blue, Brown, Cyan, Green, Lime, Orange, Pink, Purlple, Red, White, Yellow]");
                    SendMessage(e, "Color help : /color (Black, Blue, Brown, Cyan, Green, Lime, Orange, Pink, Purlple, Red, White, Yellow)");
                    break;
                case ("/color Black"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Black);
                    break;
                case ("/color Blue"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Blue);
                    break;
                case ("/color Brown"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Brown);
                    break;
                case ("/color Cyan"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Cyan);
                    break;
                case ("/color Green"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Green);
                    break;
                case ("/color Lime"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Lime);
                    break;
                case ("/color Orange"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Orange);
                    break;
                case ("/color Pink"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Pink);
                    break;
                case ("/color Purple"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Purple);
                    break;
                case ("/color Red"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Red);
                    break;
                case ("/color White"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.White);
                    break;
                case ("/color Yellow"):
                    e.PlayerControl.SetColorAsync(Api.Innersloth.Customization.ColorType.Yellow);
                    break;
                default:
                    break;
            }
        }

        private void SendMessage(IPlayerChatEvent e, string message)
        {
            e.Game.Host.Client.Player.Character.SendChatAsync(message);
            return;
        }
    }
}