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

        [EventListener]
        public void OnPlayerChat(IPlayerChatEvent e)
        {
            switch(e.Message){
                case ("/pet Alien"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Alien);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Alien");
                    break;
                case ("/pet Bedcrab"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Bedcrab);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Bedcrab");
                    break;
                case ("/pet Crewmate"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Crewmate);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Crewmate");
                    break;
                case ("/pet Doggy"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Doggy);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Doggy");
                    break;
                case ("/pet Ellie"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Ellie);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Ellie");
                    break;
                case ("/pet Hamster"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Hamster);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Hamster");
                    break;
                case ("/pet Nopet"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.NoPet);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Nopet");
                    break;
                case ("/pet Robot"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Robot);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Robot");
                    break;
                case ("/pet Squig"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Squig);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Squig");
                    break;
                case ("/pet Stickmin"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Stickmin);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Stickmin");
                    break;
                case ("/pet Ufo"):
                    e.PlayerControl.SetPetAsync(Api.Innersloth.Customization.PetType.Ufo);
                    _logger.LogInformation($"{e.PlayerControl.PlayerInfo.PlayerName} set is pet to Ufo");
                    break;
                case ("/AllPets"):
                    SendMessage(e, "Pets: Alien, Bedcrab, Ellie, Hamster, Nopet (No pet), Robot, Squig, Stickmin and Ufo");
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